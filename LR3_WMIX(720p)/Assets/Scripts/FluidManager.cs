using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
#if UNITY_5_3_OR_NEWER
using UnityEngine;
using AudioSample = System.Single;
#elif GODOT
using Godot;
using AudioSample = System.Byte;
using File = System.IO.File;
using Path = System.IO.Path;
#else
using AudioSample = System.Single;
#endif
public unsafe static class FluidManager{
    private static UIntPtr settings = UIntPtr.Zero;
    private static UIntPtr synth = UIntPtr.Zero;
    private static bool ready = false;
    private static int frames = 0;
    public const int channels = 2;
    public static int frequency { get; private set; } = 0;
    public static bool inThread = true;
    private enum PlayerStatus : byte{
        READY,    // Player is ready
        PLAYING,  // Player is currently playing
        STOPPING, // Player is stopping, but hasn't finished yet (currently unused)
        DONE      // Player is finished playing
    };
    // private delegate int HandleMidiEventFunc(UIntPtr data, UIntPtr e);
#if GODOT
    private const string format = "fluid_synth_write_s16";
#else
    private const string format = "fluid_synth_write_float";
#endif
#if UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX || GODOT_X11 || GODOT_LINUXBSD
    private delegate int FluidSynthWriteFunc(UIntPtr synth, int len, void* lout, int loff, int lincr, void* rout, int roff, int rincr);
    private delegate int FluidSettingsGetintFunc(UIntPtr settings, string name, out int val);
    private delegate int FluidSettingsGetnumFunc(UIntPtr settings, string name, out double val);
    private static class V2{
        private const string PluginName = "libfluidsynth.so.2";
        [DllImport(PluginName)] public extern static UIntPtr new_fluid_settings();
        [DllImport(PluginName)] public extern static UIntPtr new_fluid_synth(UIntPtr settings);
        [DllImport(PluginName)] public extern static int fluid_synth_sfload(
            UIntPtr synth, string sfpath, int reset_presets);
        [DllImport(PluginName)] public extern static int fluid_settings_getint(
            UIntPtr settings, string name, out int val);
        [DllImport(PluginName)] public extern static int fluid_settings_getnum(
            UIntPtr settings, string name, out double val);
        [DllImport(PluginName)] public extern static int fluid_settings_setint(
            UIntPtr settings, string name, int val);
        [DllImport(PluginName)] public extern static int fluid_settings_setnum(
            UIntPtr settings, string name, double val);
        [DllImport(PluginName)] public extern static void delete_fluid_synth(UIntPtr synth);
        [DllImport(PluginName)] public extern static void delete_fluid_settings(UIntPtr settings);
        [DllImport(PluginName)] public extern static UIntPtr new_fluid_player(UIntPtr synth);
        [DllImport(PluginName)] public extern static int fluid_player_add(UIntPtr player, string path);
        [DllImport(PluginName)] public extern static int fluid_player_play(UIntPtr player);
        [DllImport(PluginName)] public extern static PlayerStatus fluid_player_get_status(UIntPtr player);
        [DllImport(PluginName, EntryPoint = format)] public extern static int fluid_synth_write(
            UIntPtr synth, int len, void* lout, int loff, int lincr, void* rout, int roff, int rincr);
        [DllImport(PluginName)] public extern static void delete_fluid_player(UIntPtr player);
        // [DllImport(PluginName, CallingConvention = CallingConvention.StdCall)]
        // public extern static int fluid_player_set_playback_callback(UIntPtr player, HandleMidiEventFunc handler, UIntPtr data);
        [DllImport(PluginName)] public extern static int fluid_player_join(UIntPtr player);
        [DllImport(PluginName)] public extern static int fluid_player_stop(UIntPtr player);
        // [DllImport(PluginName)] public extern static int fluid_player_seek(UIntPtr player, int ticks);
    }
    private static class V3{
        private const string PluginName = "libfluidsynth.so.3";
        [DllImport(PluginName)] public extern static UIntPtr new_fluid_settings();
        [DllImport(PluginName)] public extern static UIntPtr new_fluid_synth(UIntPtr settings);
        [DllImport(PluginName)] public extern static int fluid_synth_sfload(
            UIntPtr synth, string sfpath, int reset_presets);
        [DllImport(PluginName)] public extern static int fluid_settings_getint(
            UIntPtr settings, string name, out int val);
        [DllImport(PluginName)] public extern static int fluid_settings_getnum(
            UIntPtr settings, string name, out double val);
        [DllImport(PluginName)] public extern static int fluid_settings_setint(
            UIntPtr settings, string name, int val);
        [DllImport(PluginName)] public extern static int fluid_settings_setnum(
            UIntPtr settings, string name, double val);
        [DllImport(PluginName)] public extern static void delete_fluid_synth(UIntPtr synth);
        [DllImport(PluginName)] public extern static void delete_fluid_settings(UIntPtr settings);
        [DllImport(PluginName)] public extern static UIntPtr new_fluid_player(UIntPtr synth);
        [DllImport(PluginName)] public extern static int fluid_player_add(UIntPtr player, string path);
        [DllImport(PluginName)] public extern static int fluid_player_play(UIntPtr player);
        [DllImport(PluginName)] public extern static PlayerStatus fluid_player_get_status(UIntPtr player);
        [DllImport(PluginName, EntryPoint = format)] public extern static int fluid_synth_write(
            UIntPtr synth, int len, void* lout, int loff, int lincr, void* rout, int roff, int rincr);
        [DllImport(PluginName)] public extern static void delete_fluid_player(UIntPtr player);
        // [DllImport(PluginName, CallingConvention = CallingConvention.StdCall)]
        // public extern static int fluid_player_set_playback_callback(UIntPtr player, HandleMidiEventFunc handler, UIntPtr data);
        [DllImport(PluginName)] public extern static int fluid_player_join(UIntPtr player);
        [DllImport(PluginName)] public extern static int fluid_player_stop(UIntPtr player);
        // [DllImport(PluginName)] public extern static int fluid_player_seek(UIntPtr player, int ticks);
    }
    private static Func<UIntPtr> new_fluid_settings = null;
    private static Func<UIntPtr,UIntPtr> new_fluid_synth = null;
    private static Func<UIntPtr,string,int,int> fluid_synth_sfload = null;
    private static FluidSettingsGetintFunc fluid_settings_getint = null;
    private static FluidSettingsGetnumFunc fluid_settings_getnum = null;
    private static Func<UIntPtr,string,int,int> fluid_settings_setint = null;
    private static Func<UIntPtr,string,double,int> fluid_settings_setnum = null;
    private static Action<UIntPtr> delete_fluid_synth = null;
    private static Action<UIntPtr> delete_fluid_settings = null;
    private static Func<UIntPtr,UIntPtr> new_fluid_player = null;
    private static Func<UIntPtr,string,int> fluid_player_add = null;
    private static Func<UIntPtr,int> fluid_player_play = null;
    private static Func<UIntPtr,PlayerStatus> fluid_player_get_status = null;
    private static FluidSynthWriteFunc fluid_synth_write = null;
    private static Action<UIntPtr> delete_fluid_player = null;
    // private static Func<UIntPtr,HandleMidiEventFunc,UIntPtr,int> fluid_player_set_playback_callback = null;
    private static Func<UIntPtr,int> fluid_player_join = null;
    private static Func<UIntPtr,int> fluid_player_stop = null;
    // private static Func<UIntPtr,int,int> fluid_player_seek = null;
    private static void MatchVersion(){
        const string path = "/lib/x86_64-linux-gnu/libfluidsynth.so.";
        if(File.Exists(path + "2")){
            new_fluid_settings = V2.new_fluid_settings;
            new_fluid_synth = V2.new_fluid_synth;
            fluid_synth_sfload = V2.fluid_synth_sfload;
            fluid_settings_getint = V2.fluid_settings_getint;
            fluid_settings_getnum = V2.fluid_settings_getnum;
            fluid_settings_setint = V2.fluid_settings_setint;
            fluid_settings_setnum = V2.fluid_settings_setnum;
            delete_fluid_synth = V2.delete_fluid_synth;
            delete_fluid_settings = V2.delete_fluid_settings;
            new_fluid_player = V2.new_fluid_player;
            fluid_player_add = V2.fluid_player_add;
            fluid_player_play = V2.fluid_player_play;
            fluid_player_get_status = V2.fluid_player_get_status;
            fluid_synth_write = V2.fluid_synth_write;
            delete_fluid_player = V2.delete_fluid_player;
            // fluid_player_set_playback_callback = V2.fluid_player_set_playback_callback;
            fluid_player_join = V2.fluid_player_join;
            fluid_player_stop = V2.fluid_player_stop;
            // fluid_player_seek = V2.fluid_player_seek;
        }
        else if(File.Exists(path + "3")){
            new_fluid_settings = V3.new_fluid_settings;
            new_fluid_synth = V3.new_fluid_synth;
            fluid_synth_sfload = V3.fluid_synth_sfload;
            fluid_settings_getint = V3.fluid_settings_getint;
            fluid_settings_getnum = V3.fluid_settings_getnum;
            fluid_settings_setint = V3.fluid_settings_setint;
            fluid_settings_setnum = V3.fluid_settings_setnum;
            delete_fluid_synth = V3.delete_fluid_synth;
            delete_fluid_settings = V3.delete_fluid_settings;
            new_fluid_player = V3.new_fluid_player;
            fluid_player_add = V3.fluid_player_add;
            fluid_player_play = V3.fluid_player_play;
            fluid_player_get_status = V3.fluid_player_get_status;
            fluid_synth_write = V3.fluid_synth_write;
            delete_fluid_player = V3.delete_fluid_player;
            // fluid_player_set_playback_callback = V3.fluid_player_set_playback_callback;
            fluid_player_join = V3.fluid_player_join;
            fluid_player_stop = V3.fluid_player_stop;
            // fluid_player_seek = V3.fluid_player_seek;
        }
        // else throw new DllNotFoundException("libfluidsynth 2 or 3 required");
    }
#else //if UNITY_5_3_OR_NEWER || GODOT
#if GODOT
    // private const string PluginName = "Plugins/FluidSynth/libfluidsynth-3";
    private const string PluginName = "Plugins/FluidSynth/audioplugin-fluidsynth-3";
#else
    // private const string PluginName = "libfluidsynth-3";
    private const string PluginName = "audioplugin-fluidsynth-3";
#endif
    [DllImport(PluginName)] private extern static UIntPtr new_fluid_settings();
    [DllImport(PluginName)] private extern static UIntPtr new_fluid_synth(UIntPtr settings);
    [DllImport(PluginName)] private extern static int fluid_synth_sfload(
        UIntPtr synth, string sfpath, int reset_presets);
    [DllImport(PluginName)] private extern static int fluid_settings_getint(
        UIntPtr settings, string name, out int val);
    [DllImport(PluginName)] private extern static int fluid_settings_getnum(
        UIntPtr settings, string name, out double val);
    [DllImport(PluginName)] private extern static int fluid_settings_setint(
        UIntPtr settings, string name, int val);
    [DllImport(PluginName)] private extern static int fluid_settings_setnum(
        UIntPtr settings, string name, double val);
    [DllImport(PluginName)] private extern static void delete_fluid_synth(UIntPtr synth);
    [DllImport(PluginName)] private extern static void delete_fluid_settings(UIntPtr settings);
    [DllImport(PluginName)] private extern static UIntPtr new_fluid_player(UIntPtr synth);
    [DllImport(PluginName)] private extern static int fluid_player_add(UIntPtr player, string path);
    [DllImport(PluginName)] private extern static int fluid_player_play(UIntPtr player);
    [DllImport(PluginName)] private extern static PlayerStatus fluid_player_get_status(UIntPtr player);
    [DllImport(PluginName, EntryPoint = format)] private extern static int fluid_synth_write(
        UIntPtr synth, int len, void* lout, int loff, int lincr, void* rout, int roff, int rincr);
    [DllImport(PluginName)] private extern static void delete_fluid_player(UIntPtr player);
    // [DllImport(PluginName, CallingConvention = CallingConvention.StdCall)]
    // private extern static int fluid_player_set_playback_callback(UIntPtr player, HandleMidiEventFunc handler, UIntPtr data);
    [DllImport(PluginName)] private extern static int fluid_player_join(UIntPtr player);
    [DllImport(PluginName)] private extern static int fluid_player_stop(UIntPtr player);
    // [DllImport(PluginName)] private extern static int fluid_player_seek(UIntPtr player, int ticks);
#endif
    public static void Init(string sfpath, double gain = double.NaN, double overflow_vol = double.NaN){
        if((Func<UIntPtr>)new_fluid_settings == null || !File.Exists(sfpath)){ ready = false; return; }
        CleanUp();
        settings = new_fluid_settings();
        if(settings == UIntPtr.Zero){ ready = false; return; }
        synth = new_fluid_synth(settings);
        if(synth == UIntPtr.Zero) goto cleanup;
        if(fluid_synth_sfload(synth, sfpath, 0) == -1) goto cleanup;
        fluid_settings_getnum(settings, "synth.sample-rate", out double sampleRate);
        frequency = (int)sampleRate;
        if(frequency < 8000) goto cleanup;
        // fluid_settings_getint(settings, "audio.periods", out int audio_periods);
        // if(audio_periods < 2) goto cleanup;
        fluid_settings_getint(settings, "audio.period-size", out int audio_period_size);
        if(audio_period_size < 64) goto cleanup;
        frames = audio_period_size / channels;
        fluid_settings_setint(settings, "synth.audio-channels", channels / 2);
        if(!double.IsNaN(overflow_vol) && !double.IsInfinity(overflow_vol) && overflow_vol >= double.Epsilon)
            fluid_settings_setnum(settings, "synth.overflow.volume", overflow_vol);// default:500
        if(!double.IsNaN(gain) && !double.IsInfinity(gain) && gain >= double.Epsilon)
            fluid_settings_setnum(settings, "synth.gain", gain);// default:0.2
        ready = true; return;
        cleanup: CleanUp(); return;
    }
    public static AudioSample[] MidiToSamples(string midipath){
        if(!ready || !File.Exists(midipath)) return null;
        UIntPtr player = new_fluid_player(synth);
        if(player == UIntPtr.Zero) return null;
        List<AudioSample> total_samples = new List<AudioSample>();
#if GODOT
        byte[] samples = new byte[frames * channels * sizeof(short)];
#else
        float[] samples = new float[frames * channels];
#endif
        fluid_player_add(player, midipath);
        fixed(void* temp = samples){
            /*fluid_player_set_playback_callback(player, (data, e) => {
                if(fluid_synth_write(synth, frames, temp, 0, channels, temp, 1, channels) == 0){
                    total_samples.AddRange(samples);
                }
                return 0;
            }, synth);*/
            fluid_player_play(player);
            while(inThread && 
                fluid_player_get_status(player) == PlayerStatus.PLAYING &&
                fluid_synth_write(synth, frames, temp, 0, channels, temp, 1, channels) == 0
            ){ total_samples.AddRange(samples); }
        }
        fluid_player_stop(player);
        fluid_player_join(player);
        delete_fluid_player(player);
        return total_samples.ToArray();
    }
    public static void CleanUp(){
        frequency = frames = 0;
        ready = false;
        // inThread = false;
        if((Action<UIntPtr>)delete_fluid_synth == null) return;
        if(synth != UIntPtr.Zero) delete_fluid_synth(synth);
        if(settings != UIntPtr.Zero) delete_fluid_settings(settings);
        settings = synth = UIntPtr.Zero;
    }
    static FluidManager(){
#if UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX || GODOT_X11 || GODOT_LINUXBSD
        MatchVersion();
#endif
// #if GODOT
//         Init("TimGM6mb.sf2", 1.5);
//         Atexit
// #endif
    }
#if UNITY_5_3_OR_NEWER
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void _(){
        Application.quitting += CleanUp;
        Init(Application.streamingAssetsPath + "/TimGM6mb.sf2", 1.5);
    }
#endif
}
