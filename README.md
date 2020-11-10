# SoundThermo

Simple thermometer using sound card input and output, a thermistor and couple of resistors.

# Hardware

One should assemble components as shown on scheme below:

![Thermometer scheme](scheme.png)

* Lout should be connected to left channel of sound card output
* Rout should be connected to right channel
* Mic In is for microphone input
* GND should be connected to ground (both on input and output)

# Using the software

First, one should compile the project (target is .NET Core 3.1). After that (assuming your executable 
is named `SoundThermo.exe` one should enumerate all sound devices in the system with:
`SoundThermo.exe list`

You will get list of all sound devices with their GUIDs. Identify your device and note its GUID.

After that one should run `SoundThermo.exe measure --input <INPUT_GUID> --output <OUTPUT_GUID> --duration <DURATION>`.
Duration means amount of time spent for measuring (longer the measurement, more precise results). As for me, one second
is enough.

# How it works

The idea is to generate harmonic signal with some amplitude `Vin` on sound output and then measure
`Vout` - amplitude of same signal on microphone input. 

If we consider just left or just right channel, we could not that `Vout = R2 * Vin / (R1 + R2)`,
where `R1` and `R2` are resistor values. Unfortunately, due to different volumes in input and output,
really equation is like `Vout = R2 * Vin * k / (R1 + R2)`. So firstly we do measurement via right 
channel (where we know both `R1` and `R2`), to determine `k`. After that we repeat same measurement
using left channel. Knowing `Vin`, `Vout`, `R2`, and `k` we can determine value of thermistor. 
After that we compute real temperature via approximation formula.
