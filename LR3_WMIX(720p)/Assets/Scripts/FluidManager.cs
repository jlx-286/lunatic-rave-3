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
#if UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
	private const string PluginName = "libfluidsynth";
	// private const string PluginName = "libfluidsynth2";
#else
    //private const string PluginName = "libfluidsynth-3";
    private const string PluginName = "audioplugin-fluidsynth-3";
#endif
    [DllImport(PluginName)] private extern static IntPtr new_fluid_settings();
	[DllImport(PluginName)] private extern static IntPtr new_fluid_synth(IntPtr settings);
	[DllImport(PluginName)] private extern static int fluid_synth_sfload(
		IntPtr synth, string sfpath, int reset_presets);
	[DllImport(PluginName)] private extern static int fluid_settings_getint(
		IntPtr settings, string name, out int val);
	[DllImport(PluginName)] private extern static int fluid_settings_setnum(
		IntPtr settings, string name, double val);
	[DllImport(PluginName)] private extern static void delete_fluid_synth(IntPtr synth);
	[DllImport(PluginName)] private extern static void delete_fluid_settings(IntPtr settings);
	[DllImport(PluginName)] private extern static IntPtr new_fluid_player(IntPtr synth);
	[DllImport(PluginName)] private extern static int fluid_player_add(IntPtr player, string path);
	[DllImport(PluginName)] private extern static int fluid_player_play(IntPtr player);
    private enum fluid_player_status{
		FLUID_PLAYER_READY,     //< Player is ready
		FLUID_PLAYER_PLAYING,   //< Player is currently playing
		FLUID_PLAYER_STOPPING,  //< Player is stopping, but hasn't finished yet (currently unused)
		FLUID_PLAYER_DONE       //< Player is finished playing
	};
	[DllImport(PluginName)] private extern static fluid_player_status fluid_player_get_status(IntPtr player);
	[DllImport(PluginName)] private extern static int fluid_synth_write_float(IntPtr synth,
		int len, IntPtr lout, int loff, int lincr, IntPtr rout, int roff, int rincr);
    [DllImport(PluginName)] private extern static void delete_fluid_player(IntPtr player);
    /*private delegate int handle_midi_event_func_t(IntPtr data, IntPtr e);
    [DllImport(PluginName, CallingConvention = CallingConvention.StdCall)]
    private extern static int fluid_player_set_playback_callback(IntPtr player, handle_midi_event_func_t handler, IntPtr data);*/
    [DllImport(PluginName)] private extern static int fluid_player_join(IntPtr player);
    public static void Init(string sfpath, double gain = double.NaN, double overflow_vol = double.NaN){
		if(!File.Exists(sfpath)){ ready = false; return; }
		CleanUp();
		settings = new_fluid_settings();
		if(settings == IntPtr.Zero){ ready = false; return; }
		synth = new_fluid_synth(settings);
		if(synth == IntPtr.Zero) goto cleanup;
		if(fluid_synth_sfload(synth, sfpath, 0) == -1) goto cleanup;
		fluid_settings_getint(settings, "synth.audio-channels", out channels);
		if(channels < 1) goto cleanup;
		fluid_settings_getint(settings, "audio.period-size", out audio_period_size);
		if(audio_period_size < channels) goto cleanup;
		if(!double.IsNaN(overflow_vol) && !double.IsInfinity(overflow_vol) && overflow_vol >= double.Epsilon)
            fluid_settings_setnum(settings, "synth.overflow.volume", overflow_vol);// default:500
		if(!double.IsNaN(gain) && !double.IsInfinity(gain) && gain >= double.Epsilon)
		    fluid_settings_setnum(settings, "synth.gain", gain);// default:0.2
		ready = true; return;
		cleanup: CleanUp(); return;
	}
	public static float[] MidiToSamples(string midipath, out int lengthSamples, out int frequency){
		lengthSamples = frequency = 0;
		if(!ready || !File.Exists(midipath)) return null;
        IntPtr player = new_fluid_player(synth);
        if (player == IntPtr.Zero) return null;
        List<float> total_samples = new List<float>();
        float[] samples = new float[audio_period_size];
        IntPtr temp = Marshal.UnsafeAddrOfPinnedArrayElement(samples, 0);
        fluid_player_add(player, midipath);
        /*fluid_player_set_playback_callback(player, (data, e) => {
            if (fluid_synth_write_float(synth, audio_period_size, temp, 0, channels, temp, 1, channels) == 0){
                total_samples.AddRange(samples);
            }
            return 0;
        }, synth);*/
        fluid_player_play(player);
        while(fluid_player_get_status(player) == fluid_player_status.FLUID_PLAYER_PLAYING){
            if (fluid_synth_write_float(synth, audio_period_size, temp, 0, channels, temp, 1, channels) == 0){
                total_samples.AddRange(samples);
            } else break;
        }
        fluid_player_join(player);
        delete_fluid_player(player);
        lengthSamples = total_samples.Count;
#if UNITY_EDITOR_WIN || UNITY_STANDAONE_WIN
        frequency = lengthSamples / audio_period_size * 2;
#else
        frequency = lengthSamples / audio_period_size / sizeof(float);
#endif
        return total_samples.ToArray();
	}
	public static void CleanUp(){
		if(synth != IntPtr.Zero) delete_fluid_synth(synth);
		if(settings != IntPtr.Zero) delete_fluid_settings(settings);
		settings = synth = IntPtr.Zero;
		ready = false;
	}
}
