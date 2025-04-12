using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
#if UNITY_5_3_OR_NEWER
using UnityEngine;
using AudioSample = System.Single;
#elif GODOT
using Godot;
using AudioSample = System.Byte;
using Color32 = System.UInt32;
using File = System.IO.File;
using Thread = System.Threading.Thread;
#else
using AudioSample = System.Single;
using Color32 = System.UInt32;
#endif
public unsafe static class FFmpegPlugins{
    private enum AVSampleFormat : sbyte {
        AV_SAMPLE_FMT_NONE = -1, 
        AV_SAMPLE_FMT_U8, AV_SAMPLE_FMT_S16, AV_SAMPLE_FMT_S32,
        AV_SAMPLE_FMT_FLT, AV_SAMPLE_FMT_DBL, 
        AV_SAMPLE_FMT_U8P, AV_SAMPLE_FMT_S16P, AV_SAMPLE_FMT_S32P,
        AV_SAMPLE_FMT_FLTP, AV_SAMPLE_FMT_DBLP,
        AV_SAMPLE_FMT_S64, AV_SAMPLE_FMT_S64P,
        AV_SAMPLE_FMT_NB // Number of sample formats. DO NOT USE if linking dynamically
    };
#if GODOT
    private const string projLibPath = "Plugins/FFmpeg/";
    private const AVSampleFormat format = AVSampleFormat.AV_SAMPLE_FMT_S16;
#else //if UNITY_5_3_OR_NEWER
    private const string projLibPath = "";
    private const AVSampleFormat format = AVSampleFormat.AV_SAMPLE_FMT_FLT;
#endif
#if UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX || GODOT_X11 || GODOT_LINUXBSD
    private delegate bool GetAudioInfoFunc(string url, AVSampleFormat format,
        out int channels, out int frequency, Func<int,IntPtr> add, Action append);
    private delegate bool GetPixelsInfoFunc(string url, out int width,
        out int height, Func<int,int,IntPtr> load, bool strech = false);
#if GODOT
    private const string ext = ".so";
#else
    private const string ext = "";
#endif
    private static GetAudioInfoFunc GetAudioInfo = null;
    private static GetPixelsInfoFunc GetPixelsInfo = null;
    private static class V4{
        private const string PluginName = projLibPath + "FFmpegPlugin.4" + ext;
        [DllImport(PluginName)] public extern static bool GetAudioInfo(string url, AVSampleFormat
            format, out int channels, out int frequency, Func<int,IntPtr> add, Action append);
        [DllImport(PluginName)] public extern static bool GetPixelsInfo(string url,
            out int width, out int height, Func<int,int,IntPtr> load, bool strech = false);
    }
    private static class V5{
        private const string PluginName = projLibPath + "FFmpegPlugin.5" + ext;
        [DllImport(PluginName)] public extern static bool GetAudioInfo(string url, AVSampleFormat
            format, out int channels, out int frequency, Func<int,IntPtr> add, Action append);
        [DllImport(PluginName)] public extern static bool GetPixelsInfo(string url,
            out int width, out int height, Func<int,int,IntPtr> load, bool strech = false);
    }
    private static class V6{
        private const string PluginName = projLibPath + "FFmpegPlugin.6" + ext;
        [DllImport(PluginName)] public extern static bool GetAudioInfo(string url, AVSampleFormat
            format, out int channels, out int frequency, Func<int,IntPtr> add, Action append);
        [DllImport(PluginName)] public extern static bool GetPixelsInfo(string url,
            out int width, out int height, Func<int,int,IntPtr> load, bool strech = false);
    }
    private static class V7{
        private const string PluginName = projLibPath + "FFmpegPlugin.7" + ext;
        [DllImport(PluginName)] public extern static bool GetAudioInfo(string url, AVSampleFormat
            format, out int channels, out int frequency, Func<int,IntPtr> add, Action append);
        [DllImport(PluginName)] public extern static bool GetPixelsInfo(string url,
            out int width, out int height, Func<int,int,IntPtr> load, bool strech = false);
    }
    private static void MatchVersion(){
        const string PluginDir = "/lib/x86_64-linux-gnu/libavcodec.so.";
        if(File.Exists(PluginDir + "58")){//FFmpeg 4.x
            GetAudioInfo     = V4.GetAudioInfo;
            GetPixelsInfo    = V4.GetPixelsInfo;
        }else if(File.Exists(PluginDir + "59")){//FFmpeg 5.x
            GetAudioInfo     = V5.GetAudioInfo;
            GetPixelsInfo    = V5.GetPixelsInfo;
        }else if(File.Exists(PluginDir + "60")){//FFmpeg 6.x
            GetAudioInfo     = V6.GetAudioInfo;
            GetPixelsInfo    = V6.GetPixelsInfo;
        }else if(File.Exists(PluginDir + "61")){//FFmpeg 7.x
            GetAudioInfo     = V7.GetAudioInfo;
            GetPixelsInfo    = V7.GetPixelsInfo;
        }else throw new DllNotFoundException("unknown FFmpeg version");
    }
#else
    private const string PluginName = projLibPath + "FFmpegPlugin.4";
    [DllImport(PluginName)] private extern static bool GetAudioInfo(string url, AVSampleFormat
        format, out int channels, out int frequency, Func<int,IntPtr> add, Action append);
    [DllImport(PluginName)] private extern static bool GetPixelsInfo(string url,
        out int width, out int height, Func<int,int,IntPtr> load, bool strech = false);
#endif
    static FFmpegPlugins(){
#if UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX || GODOT_X11 || GODOT_LINUXBSD
        MatchVersion();
#endif
// #if !UNITY_5_3_OR_NEWER
//         Atexit
// #endif
    }
/*#if UNITY_5_3_OR_NEWER
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void _(){
        Application.quitting += ;
    }
#endif*/
    public static Color32[] GetTextureInfo(string path, out int width, out int height){
        width = height = 0;
        // if(!File.Exists(path)) return null;
        Color32[] color32s = null;
        GetPixelsInfo(path, out width, out height, (w,h)=>{
            int max = Math.Max(w,h);
            color32s = new Color32[max*max];
            fixed(void* p = color32s) return (IntPtr)p;
            // return Marshal.UnsafeAddrOfPinnedArrayElement(color32s, 0);
        });
        if(width < 1 || height < 1) return null;
        width = height = Math.Max(width, height);
        return color32s;
    }
    public static Color32[] GetStageImage(string path, out int width, out int height){
        width = height = 0;
        // if(!File.Exists(path)) return null;
        Color32[] color32s = null;
        GetPixelsInfo(path, out width, out height, (w,h)=>{
            color32s = new Color32[w*h];
            fixed(void* p = color32s) return (IntPtr)p;
            // return Marshal.UnsafeAddrOfPinnedArrayElement(color32s, 0);
        }, true);
        if(width < 1 || height < 1) return null;
        return color32s;
    }
    public static AudioSample[] AudioToSamples(string path, out int channels, out int frequency){
        channels = frequency = 0;
        // if(!File.Exists(path)) return null;
        List<AudioSample> result = new List<AudioSample>();
        AudioSample[] samples = null;
        GetAudioInfo(path, format, out channels, out frequency, i=>{
            samples = new AudioSample[i / sizeof(AudioSample)];
            fixed(void* p = samples) return (IntPtr)p;
            // return Marshal.UnsafeAddrOfPinnedArrayElement(samples, 0);
        },()=>{result.AddRange(samples);});
#if UNITY_5_3_OR_NEWER
        // else Debug.LogWarning(path + ":Invalid data or too long data");
#elif GODOT
        // else GD.PushWarning(path + ":Invalid data or too long data");
#endif
        if(result.Count < 1){
            try{
                channels = FluidManager.channels;
                frequency = FluidManager.frequency;
                return FluidManager.MidiToSamples(path);
            }catch(Exception e){
                channels = frequency = 0;
                Debug.LogWarning(e.GetBaseException());
                return null;
            }
        }
        return result.ToArray();
    }
}
