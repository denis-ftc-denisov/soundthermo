using NAudio.Dsp;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SoundThermo
{
	/// <summary>
	/// Generates sine sample in the form
	/// amp * sin(x / freq)
	/// </summary>
	class ThermoSampleProvider : ISampleProvider
	{
		public const int SAMPLE_RATE = 44100;
		WaveFormat format;
		float freq;
		float amp;
		int alreadyGeneratedSamples;

		public ThermoSampleProvider(float freq, float amp)
		{
			format = WaveFormat.CreateIeeeFloatWaveFormat(SAMPLE_RATE, 1);
			this.freq = freq;
			this.amp = amp;
			alreadyGeneratedSamples = 0;
		}

		public WaveFormat WaveFormat
		{
			get
			{
				return format;
			}
		}

		public int Read(float[] buffer, int offset, int count)
		{
			for (int i = 0; i < count; i++)
			{
				int sample = alreadyGeneratedSamples++;
				buffer[offset + i] = amp * MathF.Sin(2 * MathF.PI * sample * freq / SAMPLE_RATE);
			}
			return count;
		}
	}

	/// <summary>
	/// Returns ratio of output signal to input signal
	/// </summary>
	class Measurer
	{
		public enum MeasureChannel
		{
			Left,
			Right
		}

		const int THERMO_FREQ = 6000;
		const float THERMO_AMP = 0.1f;

		WaveInEvent inputDevice;
		WaveOutEvent outputDevice;
		MeasureChannel channel;

		public Measurer(WaveInEvent inputDevice, WaveOutEvent outputDevice, MeasureChannel ch)
		{
			this.inputDevice = inputDevice;
			this.outputDevice = outputDevice;
			this.channel = ch;
		}

		public async Task<float> Measure(float duration)
		{
			var recorder = new Recorder(inputDevice);
			var provider = new ThermoSampleProvider(THERMO_FREQ, THERMO_AMP);
			outputDevice.Init(provider.ToStereo(channel == MeasureChannel.Left ? 1 : 0, channel == MeasureChannel.Right ? 1 : 0));
			outputDevice.Play();
			await recorder.Record(duration);
			outputDevice.Stop();
			var newSamples = new List<float>();
			int leftPos = 0;
			while (leftPos < recorder.samples.Count && MathF.Abs(recorder.samples[leftPos]) < 1e-4) leftPos++;
			int rightPos = recorder.samples.Count - 1;
			while (rightPos > 0 && MathF.Abs(recorder.samples[rightPos]) < 1e-4) rightPos--;
			for (int i = leftPos; i <= rightPos; i++) newSamples.Add(recorder.samples[i]);
			int pow = 1;
			int m = 0;
			while (2 * pow <= newSamples.Count)
			{
				pow *= 2;
				m++;
			}
			var data = new Complex[pow];
			for (int i = 0; i < data.Length; i++)
			{
				data[i].X = newSamples[i];
				data[i].Y = 0;
			}
			FastFourierTransform.FFT(true, m, data);
			float max = -1;
			int argmax = 0;
			for (int i = 0; i < data.Length; i++)
			{
				var amp = MathF.Sqrt(data[i].X * data[i].X + data[i].Y * data[i].Y);
				if (amp > max)
				{
					max = amp;
					argmax = i;
				}	
			}
			float desired = THERMO_FREQ * pow / ThermoSampleProvider.SAMPLE_RATE;
			if (MathF.Abs(desired - argmax) < 1e-3)
			{
				// max - amplitude on desired frequency
				return max / THERMO_AMP;
			}
			return float.NaN;
			
		}
	}
}
