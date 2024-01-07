using System;
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
#if UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX || GODOT_X11
    private delegate bool GetAudioInfoFunc(string url, AVSampleFormat format, out int channels, out int frequency, out ulong length);
    private delegate void CopyAudioSamplesFunc(void* addr);
    private delegate bool GetPixelsInfoFunc(string url, out int width, out int height, out bool isBitmap);
    private delegate void CopyPixelsFunc(void* addr, int width, int height, bool isBitmap, bool strech = false);
    public delegate void CleanUpFunc();
    private static GetAudioInfoFunc GetAudioInfo = null;
    private static CopyAudioSamplesFunc CopyAudioSamples = null;
    private static GetPixelsInfoFunc GetPixelsInfo = null;
    private static CopyPixelsFunc CopyPixels = null;
    public static CleanUpFunc CleanUp = null;
    private static class V4{
        private const string PluginName = projLibPath + "FFmpegPlugin.so";
        [DllImport(PluginName)] public extern static bool GetAudioInfo(
            string url, AVSampleFormat format, out int channels, out int frequency, out ulong length);
        [DllImport(PluginName)] public extern static void CopyAudioSamples(void* addr);
        [DllImport(PluginName)] public extern static bool GetPixelsInfo(
            string url, out int width, out int height, out bool isBitmap);
        [DllImport(PluginName)] public extern static void CopyPixels(
            void* addr, int width, int height, bool isBitmap, bool strech = false);
        [DllImport(PluginName)] public extern static void CleanUp();
    }
    private static class V5{
        private const string PluginName = projLibPath + "FFmpeg5Plugin.so";
        [DllImport(PluginName)] public extern static bool GetAudioInfo(
            string url, AVSampleFormat format, out int channels, out int frequency, out ulong length);
        [DllImport(PluginName)] public extern static void CopyAudioSamples(void* addr);
        [DllImport(PluginName)] public extern static bool GetPixelsInfo(
            string url, out int width, out int height, out bool isBitmap);
        [DllImport(PluginName)] public extern static void CopyPixels(
            void* addr, int width, int height, bool isBitmap, bool strech = false);
        [DllImport(PluginName)] public extern static void CleanUp();
    }
    private static class V6{
        private const string PluginName = projLibPath + "FFmpeg6Plugin.so";
        [DllImport(PluginName)] public extern static bool GetAudioInfo(
            string url, AVSampleFormat format, out int channels, out int frequency, out ulong length);
        [DllImport(PluginName)] public extern static void CopyAudioSamples(void* addr);
        [DllImport(PluginName)] public extern static bool GetPixelsInfo(
            string url, out int width, out int height, out bool isBitmap);
        [DllImport(PluginName)] public extern static void CopyPixels(
            void* addr, int width, int height, bool isBitmap, bool strech = false);
        [DllImport(PluginName)] public extern static void CleanUp();
    }
    static FFmpegPlugins(){
        const string PluginDir = "/lib/x86_64-linux-gnu/";
        if(File.Exists(PluginDir + "libavcodec.so.58")){//FFmpeg 4.x
            GetAudioInfo     = V4.GetAudioInfo;
            CopyAudioSamples = V4.CopyAudioSamples;
            GetPixelsInfo    = V4.GetPixelsInfo;
            CopyPixels       = V4.CopyPixels;
            CleanUp          = V4.CleanUp;
        }else if(File.Exists(PluginDir + "libavcodec.so.59")){//FFmpeg 5.x
            GetAudioInfo     = V5.GetAudioInfo;
            CopyAudioSamples = V5.CopyAudioSamples;
            GetPixelsInfo    = V5.GetPixelsInfo;
            CopyPixels       = V5.CopyPixels;
            CleanUp          = V5.CleanUp;
        }else if(File.Exists(PluginDir + "libavcodec.so.60")){//FFmpeg 6.x
            GetAudioInfo     = V6.GetAudioInfo;
            CopyAudioSamples = V6.CopyAudioSamples;
            GetPixelsInfo    = V6.GetPixelsInfo;
            CopyPixels       = V6.CopyPixels;
            CleanUp          = V6.CleanUp;
        }else throw new DllNotFoundException("unknown FFmpeg version");
    }
#else
    private const string PluginName = projLibPath + "FFmpegPlugin";
    [DllImport(PluginName)] private extern static bool GetAudioInfo(
        string url, AVSampleFormat format, out int channels, out int frequency, out ulong length);
    [DllImport(PluginName)] private extern static void CopyAudioSamples(void* addr);
    [DllImport(PluginName)] private extern static bool GetPixelsInfo(
        string url, out int width, out int height, out bool isBitmap);
    [DllImport(PluginName)] private extern static void CopyPixels(
        void* addr, int width, int height, bool isBitmap, bool strech = false);
    [DllImport(PluginName)] public extern static void CleanUp();
#endif
    public static Color32[] GetTextureInfo(string path, out int width, out int height){
        width = height = 0;
        // if(!File.Exists(path)) return null;
        Color32[] color32s = null;
        if(GetPixelsInfo(path, out width, out height, out bool isBitmap)){
            int max = Math.Max(width, height);
            ulong length = (ulong)max;
            length *= length;
            if(length <= int.MaxValue) color32s = new Color32[length];
            fixed(void* p = color32s)
                CopyPixels(p, width, height, isBitmap
                || path.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase));
            width = height = max;
        }
        return color32s;
    }
    public static Color32[] GetStageImage(string path, out int width, out int height){
        width = height = 0;
        // if(!File.Exists(path)) return null;
        Color32[] color32s = null;
        if(GetPixelsInfo(path, out width, out height, out bool isBitmap)){
            color32s = new Color32[width * height];
            fixed(void* p = color32s)
                CopyPixels(p, width, height, false, true);
        }
        return color32s;
    }
    public static AudioSample[] AudioToSamples(string path, out int channels, out int frequency){
        channels = frequency = 0;
        // if(!File.Exists(path)) return null;
        AudioSample[] result = null;
        if(GetAudioInfo(path, format, out channels, out frequency, out ulong length) && length <= int.MaxValue)
            result = new AudioSample[length / sizeof(AudioSample)];
#if UNITY_5_3_OR_NEWER
        // else Debug.LogWarning(path + ":Invalid data or too long data");
#elif GODOT
        // else GD.PushWarning(path + ":Invalid data or too long data");
#endif
        fixed(void* p = result) CopyAudioSamples(p);
        /*if(result == null){
            try{
                channels = FluidManager.channels;
                result = FluidManager.MidiToSamples(path, out int lengthSamples, out frequency);
            }catch(Exception e){
                channels = frequency = 0;
                result = null;
                Debug.LogWarning(e.GetBaseException());
            }
        }*/
        return result;
    }
}
