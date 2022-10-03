using NAudio.Wave;
using NLayer;
using NVorbis;
using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using System.Drawing;
using System.Drawing.Imaging;
#else
using SkiaSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
#endif
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Ude;
using UnityEngine;
using UnityEngine.Networking;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using Image = System.Drawing.Image;
#else
using Image = SixLabors.ImageSharp.Image;
#endif

public static class StaticClass{
    public static RegexOptions regexOption = RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
    [DllImport("FFmpegPlugin", EntryPoint = "GetVideoSize")] private extern static bool __GetVideoSize(string path, out int width, out int height);
    [DllImport("FFmpegPlugin")] private extern static IntPtr GetAudioSamples(string path, out int channels, out int frequency, out int length);
    private enum AudioFormat{
        Unknown,
        Vorbis,
        Mpeg,
        Others
    };
    [DllImport("FFmpegPlugin")] private extern static AudioFormat GetAudioFormat(string path);
    /// <summary>
    /// seconds
    /// </summary>
#if UNITY_2020_2_OR_NEWER
    // public static readonly double OverFlowTime = ;4.398046511E12;
    // public static readonly double OverFlowTime = 4398046511104d;
    public static readonly double OverFlowTime = Math.Pow(2, 42) - Math.Pow(2, -52) - 1;
#else
    public static readonly double OverFlowTime = Mathf.Pow(2, 13) - Mathf.Pow(2, -23) - 1;
    // public static readonly double OverFlowTime = 2 * 3600 + 20 * 60;
#endif
    /*public static TaskAwaiter GetAwaiter(this AsyncOperation operation){
        TaskCompletionSource<object> source = new TaskCompletionSource<object>();
        operation.completed += obj => { source.SetResult(null); };
        return (source.Task as Task).GetAwaiter();
    }

    /// <summary>
    /// use UnityEngine.Networking.UnityWebRequest
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static async Task<AudioClip> LoadAudioClipAsync(string path){
        if (!File.Exists(path) || File.ReadAllBytes(path).Length < 100){ return null; }
        AudioClip clip = null;
        try {
            UnityWebRequest request = null;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            request = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.UNKNOWN);
#else
            request = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.UNKNOWN);
#endif
            await request.SendWebRequest();
            if (request.isNetworkError || request.isHttpError){
                // more errors in Linux?
                Debug.LogWarning("request.error:" + request.error);
            }
            else {
                clip = DownloadHandlerAudioClip.GetContent(request);
            }
            request.Dispose();
        } catch (Exception e){
            Debug.LogWarning(e.Message);
        }
        return clip;
    }*/

    /// <summary>
    /// using Ude;
    /// </summary>
    /// <param name="path">text file path</param>
    /// <returns></returns>
    public static Encoding GetEncodingByFilePath(string path){
        CharsetDetector detector = new CharsetDetector();
        using (FileStream fileStream = File.OpenRead(path)){
            detector.Feed(fileStream);
            detector.DataEnd();
            fileStream.Flush();
            fileStream.Close();
        }
        Encoding Shift_JIS = Encoding.GetEncoding("shift_jis");
        // Debug.Log(detector.Charset);
        // Debug.Log(detector.Confidence);
        if (!string.IsNullOrEmpty(detector.Charset)){
            Encoding encoding = Encoding.GetEncoding(detector.Charset);
            if(encoding != Shift_JIS && detector.Confidence <= 0.7f && detector.Confidence > 0.6f){
                return Encoding.GetEncoding("GB18030");
            }else if(detector.Confidence <= 0.6f){
                return Shift_JIS;
            }else{
                return encoding;
            }
        }else{ return Shift_JIS; }
    }

    /// <summary>
    /// returns 0 if the string is null or the string doesn't match ^[0-9a-zA-Z]{1,}$
    /// </summary>
    /// <param name="s"></param>
    /// <returns>ushort number</returns>
    public static ushort Convert36To10(string s){
        if (s == null || !Regex.IsMatch(s, @"^[0-9a-z]{1,}$", StaticClass.regexOption)){
            return 0;
        }
        s = s.ToLower();
        string digits = "0123456789abcdefghijklmnopqrstuvwxyz";
        ushort result = 0;
        for(int i = 0; i < s.Length; i++){
            result *= 36;
            result += (ushort)digits.IndexOf(s[i]);
        }
        return result;
    }

    /*/// <summary>
    /// maximum value is "ZZ"
    /// </summary>
    /// <param name="u"></param>
    /// <returns>2 digits of 36-base nunmber string (uppercase)</returns>
    public static string Convert10To36(ushort u){
        if (u >= 36 * 36){ return "ZZ"; }
        StringBuilder result = new StringBuilder();
        string digits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        result.Append(digits[u / 36]).Append(digits[u % 36]);
        return result.ToString();
    }*/

    /*public static Texture2D GetTexture2D(string path){
        if (!File.Exists(path)) { return null; }
        MemoryStream memoryStream = new MemoryStream();
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        Bitmap bitmap = new Bitmap(path);
        if (bitmap.RawFormat.Guid == ImageFormat.Bmp.Guid || bitmap.RawFormat.Guid == ImageFormat.MemoryBmp.Guid
            || Regex.IsMatch(path, @"\.bmp$", StaticClass.regexOption)
        ){
            bitmap.MakeTransparent(System.Drawing.Color.Black);
            bitmap.Save(memoryStream, ImageFormat.Png);
        }
        //else if (Regex.IsMatch(item.Value.ToString(), @"\.(jpg|jpeg)$", StaticClass.regexOptions)){
        //    bitmap.Save(memoryStream, ImageFormat.Jpeg);
        //}
        else bitmap.Save(memoryStream, bitmap.RawFormat);
#else
        byte[] source = File.ReadAllBytes(path);
        SKBitmap bitmap = SKBitmap.Decode(source);
        bitmap.Encode(memoryStream, SKEncodedImageFormat.Png, 100);
#endif
        Texture2D texture2D = new Texture2D(bitmap.Width, bitmap.Height, TextureFormat.RGBA32, false);
        bitmap.Dispose();
        texture2D.LoadImage(memoryStream.ToArray());
        //memoryStream.Flush();
        //memoryStream.Close();
        memoryStream.Dispose();
#if !(UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
        IImageFormat format = Image.DetectFormat(source);
        if(Regex.IsMatch(path, @"\.bmp$", StaticClass.regexOption)
            || Regex.IsMatch(format.Name, @"bmp", StaticClass.regexOption)
        ){
            Color32[] color32s = texture2D.GetPixels32();
            for(int i = 0; i < color32s.Length; i++){
                if(color32s[i].r == 0 && color32s[i].g == 0 && color32s[i].b == 0){
                    color32s[i].a = 0;
                }
            }
            texture2D.SetPixels32(color32s);
        }
#endif
        texture2D.Apply(false);
        return texture2D;
    }*/

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    /// <summary>
    /// use Texture2D.LoadImage(byte[])
    /// </summary>
    /// <param name="path"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
#else
    /// <summary>
    /// use Texture2D.SetPixels32(Color32[])
    /// </summary>
    /// <param name="path"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
#endif
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    public static byte[] GetTextureInfo(string path, out int width, out int height){
#else
    public static Color32[] GetTextureInfo(string path, out int width, out int height){
#endif
        width = height = 0;
        if(!File.Exists(path)) return null;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        using(MemoryStream memoryStream = new MemoryStream())
        using(Bitmap bitmap = new Bitmap(path)){
            width = bitmap.Width; height = bitmap.Height;
            if (bitmap.RawFormat.Guid == ImageFormat.Bmp.Guid || bitmap.RawFormat.Guid == ImageFormat.MemoryBmp.Guid
                || Regex.IsMatch(path, @"\.bmp$", StaticClass.regexOption)
            ){
                bitmap.MakeTransparent(System.Drawing.Color.Black);
                bitmap.Save(memoryStream, ImageFormat.Png);
            }
            //else if (Regex.IsMatch(item.Value.ToString(), @"\.(jpg|jpeg)$", StaticClass.regexOptions)){
            //    image.Save(memoryStream, ImageFormat.Jpeg);
            //}
            else bitmap.Save(memoryStream, bitmap.RawFormat);
            return memoryStream.ToArray();
        }
#else
        byte[] source = File.ReadAllBytes(path);
        SKBitmap bitmap = SKBitmap.Decode(source);
        width = bitmap.Width;
        height = bitmap.Height;
        Color32[] color32s = new Color32[width * height];
        byte[] dist = bitmap.Bytes;
        bitmap.Dispose();
        int index1, index2;
        if(Regex.IsMatch(path, @"\.bmp$", StaticClass.regexOption)
            || Regex.IsMatch(Image.DetectFormat(source).Name, @"bmp", StaticClass.regexOption)
        ){
            for(int h = 0; h < height; h++){
                for(int w = 0; w < width; w++){
                    index1 = width * h + w; index2 = (w + (height - h - 1) * width) * 4;
                    color32s[index1].b = dist[index2];
                    color32s[index1].g = dist[index2 + 1];
                    color32s[index1].r = dist[index2 + 2];
                    if(color32s[index1].b == 0 && color32s[index1].g == 0 && color32s[index1].r == 0){
                        color32s[index1].a = 0;
                    }else color32s[index1].a = dist[index2 + 3];
                }
            }
        }else{
            for(int h = 0; h < height; h++){
                for(int w = 0; w < width; w++){
                    index1 = width * h + w; index2 = (w + (height - h - 1) * width) * 4;
                    color32s[index1].b = dist[index2];
                    color32s[index1].g = dist[index2 + 1];
                    color32s[index1].r = dist[index2 + 2];
                    color32s[index1].a = dist[index2 + 3];
                }
            }
        }
        return color32s;
#endif
    }

    public static float[] AudioToSamples(string path, out int channels, out int frequency){
        float[] result = null;
        channels = frequency = 0;
        AudioFormat format = GetAudioFormat(path);
        //Debug.Log(format);
        // Debug.Break();
        switch (format){
            case AudioFormat.Vorbis:
                try{    
                    using(var reader = new VorbisReader(path)){
                        channels = reader.Channels;
                        frequency = reader.SampleRate;
                        result = new float[reader.TotalSamples * channels];
                        reader.ReadSamples(result, 0, result.Length);
                    }
                }catch(Exception e){
                    Debug.LogWarning(e.GetBaseException());
                }
                break;
            case AudioFormat.Mpeg:
                try{
                    using(var reader = new MpegFile(path)){
                        channels = reader.Channels;
                        frequency = reader.SampleRate;
                        result = new float[reader.Length / sizeof(float)];
                        reader.ReadSamples(result, 0, result.Length);
                    }
                }catch(Exception e){
                    Debug.LogWarning(e.GetBaseException());
                }
                break;
            case AudioFormat.Others:
                try{
                    using(var reader = new AudioFileReader(path)){
                        channels = reader.WaveFormat.Channels;
                        frequency = reader.WaveFormat.SampleRate;
                        result = new float[reader.Length / sizeof(float)];
                        reader.Read(result, 0, result.Length);
                    }
                }catch(Exception e){
                    result = null;
                    Debug.LogWarning(e.GetBaseException());
                }
                if(result == null){
                    try{
                        IntPtr ptr = IntPtr.Zero;
                        int length = 0;
                        ptr = GetAudioSamples(path, out channels, out frequency, out length);
                        if(ptr != IntPtr.Zero && frequency > 0 && channels > 0){
                            result = new float[length];
                            Marshal.Copy(ptr, result, 0, length);
                        }
                    }catch(Exception e){
                        result = null;
                        Debug.LogWarning(e.GetBaseException());
                    }
                }
                /*if(result == null){
                    try{
                        channels = FluidManager.channels;
                        int lengthSamples = 0;
                        result = FluidManager.MidiToSamples(path, out lengthSamples, out frequency);
                    }catch(Exception e){
                        result = null;
                        Debug.LogWarning(e.GetBaseException());
                    }
                }*/
                break;
            default: break;
        }
        return result;
    }
    public static bool GetVideoSize(string path, out int width, out int height){
        if(!File.Exists(path)){
            width = height = 0;
            return false;
        }
        if(!__GetVideoSize(path, out width, out height) || width < 1 || height < 1){
            return false;
        }
        return true;
    }
}
