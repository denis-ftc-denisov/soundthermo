using NAudio.Wave;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SoundThermo
{
	class Recorder
	{
		const int RECORDING_RATE = 44100;
		private WaveInEvent device;
		public List<float> samples;

		public Recorder(WaveInEvent device)
		{
			this.device = device;
		}

		public async Task Record(float duration)
		{
			device.WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(RECORDING_RATE, 1);
			samples = new List<float>();
			device.DataAvailable += OnDataAvailable;
			device.StartRecording();
			await Task.Delay((int)(duration * 1000));
			device.StopRecording();
		}

		private void OnDataAvailable(object sender, WaveInEventArgs e)
		{
			var buffer = new WaveBuffer(e.Buffer);
			for (int i = 0; i < e.BytesRecorded / 4; i++)
			{
				samples.Add(buffer.FloatBuffer[i]);
			}
		}
	}
}
