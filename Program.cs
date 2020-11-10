using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace SoundThermo
{
	class Program
	{
		static void Main(string[] args)
		{
			CultureInfo.CurrentCulture = new CultureInfo("en-US");
			var rootCommand = new RootCommand();
			var listCommand = new Command("list", "List all available devices");
			listCommand.Handler = CommandHandler.Create(ListDevices);
			var recordCommand = new Command("record")
			{
				new Option<string>("--input", "Input device to record from (GUID)"),
				new Option<float>("--duration", "How many seconds of audio you want to record"),
			};
			recordCommand.Handler = CommandHandler.Create<string, float>(RecordSamples);
			var measureCommand = new Command("measure")
			{
				new Option<string>("--input", "Input device to record from (GUID)"),
				new Option<string>("--output", "Output device to play sound to (GUID)"),
				new Option<float>("--duration", "How many seconds of audio you want to record (uses twice this time because of recording on two channels)"),
			};
			measureCommand.Handler = CommandHandler.Create<string, string, float>(Measure);
			rootCommand.AddCommand(listCommand);
			rootCommand.AddCommand(measureCommand);
			rootCommand.AddCommand(recordCommand);
			rootCommand.InvokeAsync(args).Wait();
		}

		private async static Task Measure(string input, string output, float duration)
		{
			var leftMea = new Measurer(GetInputDevice(input), GetOutputDevice(output), Measurer.MeasureChannel.Left);
			var leftResult = await leftMea.Measure(duration);
			var rightMea = new Measurer(GetInputDevice(input), GetOutputDevice(output), Measurer.MeasureChannel.Right);
			var rightResult = await rightMea.Measure(duration);
			float k = 2 * rightResult;
			float v0v = leftResult / k;
			// assume r2 = 10K
			float r1 = 10000 / v0v - 10000;
			float t = 1.0f / (MathF.Log(r1 / 10000) / 4300.0f + 1.0f / 298.0f) - 273.0f;
			Console.WriteLine(t.ToString());
		}

		private static WaveOutEvent GetOutputDevice(string guid)
		{
			WaveOutEvent device = null;
			for (int n = -1; n < WaveOut.DeviceCount; n++)
			{
				var caps = WaveOut.GetCapabilities(n);
				if (caps.ProductGuid.ToString() == guid)
				{
					device = new WaveOutEvent();
					device.DeviceNumber = n;
					break;
				}
			}
			if (device == null)
			{
				throw new Exception("Unable to find device " + guid);
			}
			return device;
		}

		private static WaveInEvent GetInputDevice(string guid)
		{
			WaveInEvent device = null;
			for (int n = -1; n < WaveIn.DeviceCount; n++)
			{
				var caps = WaveIn.GetCapabilities(n);
				if (caps.ProductGuid.ToString() == guid)
				{
					device = new WaveInEvent();
					device.DeviceNumber = n;
					break;
				}
			}
			if (device == null)
			{
				throw new Exception("Unable to find device " + guid);
			}
			return device;
		}

		private async static Task RecordSamples(string input, float duration)
		{
			WaveInEvent device = GetInputDevice(input);
			var rec = new Recorder(device);
			await rec.Record(duration);
			using (FileStream fs = File.OpenWrite("recorded.txt"))
			{
				for (int i =  0; i < rec.samples.Count; i++)
				{
					fs.Write(Encoding.UTF8.GetBytes(rec.samples[i].ToString() + "\n"));
				}
			}
		}

		private static void ListDevices()
		{
			Console.WriteLine("List of audio devices in your system");
			Console.WriteLine("Output devices:");
			for (int n = -1; n < WaveOut.DeviceCount; n++)
			{
				var caps = WaveOut.GetCapabilities(n);
				Console.WriteLine($"{n}: {caps.ProductName} {caps.ProductGuid}");
			}
			Console.WriteLine("Input devices:");
			for (int n = -1; n < WaveIn.DeviceCount; n++)
			{
				var caps = WaveIn.GetCapabilities(n);
				Console.WriteLine($"{n}: {caps.ProductName} {caps.ProductGuid}");
			}
		}
	}
}
