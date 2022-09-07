using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
// using UnityEngine.Internal;

public static class FluidManager{
	private static IntPtr settings = IntPtr.Zero;
	private static IntPtr synth = IntPtr.Zero;
	private static bool ready = false;
	public static int channels = 0;
	private static int audio_period_size = 0;
	// public static string sfpath = string.Empty;
	private const string PluginName = "FluidPlugin";
	[DllImport(PluginName, EntryPoint = "FluidInit")] private static extern bool InternalFluidInit(
		string sfpath, out IntPtr settings, out IntPtr synth,
		out int channels, out int audio_period_size,
		double overflow_vol, double gain);
	[DllImport(PluginName, EntryPoint = "GetMidiSamples")] private static extern IntPtr InternalGetMidiSamples(
		string midipath, IntPtr settings, IntPtr synth,
		out int lengthSamples, int channels, out int frequency, out int length,
		int audio_period_size);
	[DllImport(PluginName, EntryPoint = "FluidCleanUp")] private static extern void InternalFluidCleanUp(
		IntPtr settings, IntPtr synth);
	public static void Init(string sfpath, double gain = 0.2d, double overflow_vol = 500d){
		if(!File.Exists(sfpath)){
			ready = false;
			return;
		}
		ready = InternalFluidInit(sfpath, out settings, out synth, out channels, out audio_period_size,
			overflow_vol, gain);
	}
	public static float[] MidiToSamples(string midipath, out int lengthSamples,
		out int frequency){
		lengthSamples = frequency = 0;
		if(!ready || !File.Exists(midipath)) return null;
		float[] samples = null;
		int length = 0;
		IntPtr ptr = InternalGetMidiSamples(midipath, settings, synth,
			out lengthSamples, channels, out frequency, out length,
			audio_period_size);
		if(ptr != IntPtr.Zero && lengthSamples > 0 && frequency > 0){
			samples = new float[length];
			Marshal.Copy(ptr, samples, 0, length);
		}
		return samples;
	}
	public static void CleanUp(){
		InternalFluidCleanUp(settings, synth);
		settings = synth = IntPtr.Zero;
	}
}
