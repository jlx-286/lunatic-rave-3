using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UnityEngine;
public unsafe static class FFmpegPlugins{
#if UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX || PLATFORM_STANDALONE_LINUX
    public delegate bool GetVideoSizeFunc(string url, out int width, out int height);
    public delegate bool GetAudioInfoFunc(string url, out int channels, out int frequency, out ulong length);
    public delegate void CopyAudioSamplesFunc(float* addr);
    public delegate bool GetPixelsInfoFunc(string url, out int width, out int height, out bool isBitmap);
    public delegate void CopyPixelsFunc(void* addr, int width, int height, bool isBitmap, bool strech = false);
    public delegate void CleanUpFunc();
    public static GetVideoSizeFunc GetVideoSize = null;
    public static GetAudioInfoFunc GetAudioInfo = null;
    public static CopyAudioSamplesFunc CopyAudioSamples = null;
    public static GetPixelsInfoFunc GetPixelsInfo = null;
    public static CopyPixelsFunc CopyPixels = null;
    public static CleanUpFunc CleanUp = null;
    private static class V4{
        private const string PluginName = "FFmpegPlugin";
        [DllImport(PluginName)] public extern static bool GetVideoSize(
            string url, out int width, out int height);
        [DllImport(PluginName)] public extern static bool GetAudioInfo(
            string url, out int channels, out int frequency, out ulong length);
        [DllImport(PluginName)] public extern static void CopyAudioSamples(float* addr);
        [DllImport(PluginName)] public extern static bool GetPixelsInfo(
            string url, out int width, out int height, out bool isBitmap);
        [DllImport(PluginName)] public extern static void CopyPixels(
            void* addr, int width, int height, bool isBitmap, bool strech = false);
        [DllImport(PluginName)] public extern static void CleanUp();
    }
    private static class V5{
        private const string PluginName = "FFmpeg5Plugin";
        [DllImport(PluginName)] public extern static bool GetVideoSize(
            string url, out int width, out int height);
        [DllImport(PluginName)] public extern static bool GetAudioInfo(
            string url, out int channels, out int frequency, out ulong length);
        [DllImport(PluginName)] public extern static void CopyAudioSamples(float* addr);
        [DllImport(PluginName)] public extern static bool GetPixelsInfo(
            string url, out int width, out int height, out bool isBitmap);
        [DllImport(PluginName)] public extern static void CopyPixels(
            void* addr, int width, int height, bool isBitmap, bool strech = false);
        [DllImport(PluginName)] public extern static void CleanUp();
    }
    private static class V6{
        private const string PluginName = "FFmpeg6Plugin";
        [DllImport(PluginName)] public extern static bool GetVideoSize(
            string url, out int width, out int height);
        [DllImport(PluginName)] public extern static bool GetAudioInfo(
            string url, out int channels, out int frequency, out ulong length);
        [DllImport(PluginName)] public extern static void CopyAudioSamples(float* addr);
        [DllImport(PluginName)] public extern static bool GetPixelsInfo(
            string url, out int width, out int height, out bool isBitmap);
        [DllImport(PluginName)] public extern static void CopyPixels(
            void* addr, int width, int height, bool isBitmap, bool strech = false);
        [DllImport(PluginName)] public extern static void CleanUp();
    }
    public static void MatchFFmpegVersion(){
        const string PluginDir = "/lib/x86_64-linux-gnu/";
        if(File.Exists(PluginDir + "libavcodec.so.58")){//FFmpeg 4.x
            GetVideoSize     = V4.GetVideoSize;
            GetAudioInfo     = V4.GetAudioInfo;
            CopyAudioSamples = V4.CopyAudioSamples;
            GetPixelsInfo    = V4.GetPixelsInfo;
            CopyPixels       = V4.CopyPixels;
            CleanUp          = V4.CleanUp;
        }else if(File.Exists(PluginDir + "libavcodec.so.59")){//FFmpeg 5.x
            GetVideoSize     = V5.GetVideoSize;
            GetAudioInfo     = V5.GetAudioInfo;
            CopyAudioSamples = V5.CopyAudioSamples;
            GetPixelsInfo    = V5.GetPixelsInfo;
            CopyPixels       = V5.CopyPixels;
            CleanUp          = V5.CleanUp;
        }else if(File.Exists(PluginDir + "libavcodec.so.60")){//FFmpeg 6.x
            GetVideoSize     = V6.GetVideoSize;
            GetAudioInfo     = V6.GetAudioInfo;
            CopyAudioSamples = V6.CopyAudioSamples;
            GetPixelsInfo    = V6.GetPixelsInfo;
            CopyPixels       = V6.CopyPixels;
            CleanUp          = V6.CleanUp;
        }else throw new DllNotFoundException("unknown FFmpeg version");
    }
#else
    private const string PluginName = "FFmpegPlugin";
    [DllImport(PluginName)] public extern static bool GetVideoSize(
        string url, out int width, out int height);
    [DllImport(PluginName)] private extern static bool GetAudioInfo(
        string url, out int channels, out int frequency, out ulong length);
    [DllImport(PluginName)] private extern static void CopyAudioSamples(float* addr);
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
                || Regex.IsMatch(path, @"\.bmp$", StaticClass.regexOption));
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
    public static float[] AudioToSamples(string path, out int channels, out int frequency){
        channels = frequency = 0;
        // if(!File.Exists(path)) return null;
        float[] result = null;
        if(GetAudioInfo(path, out channels, out frequency, out ulong length) && length <= int.MaxValue)
            result = new float[length];
        // else Debug.LogWarning(path + ":Invalid data or too long data");
        fixed(float* p = result) CopyAudioSamples(p);
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
