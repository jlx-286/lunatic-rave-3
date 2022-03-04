using FFmpeg.NET;
using FFmpeg.NET.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Ude;
using UnityEngine;
using UnityEngine.Networking;
using FFmpegEngine = FFmpeg.NET.Engine;
using Image = System.Drawing.Image;

public static class StaticClass{
    public static RegexOptions regexOption = RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
    public static FFmpegEngine ffmpegEngine;

    public static TaskAwaiter GetAwaiter(this AsyncOperation operation){
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
        if (!File.Exists(path)){
            return null;
        }
        AudioClip clip = null;
        UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.UNKNOWN);
        await request.SendWebRequest();
        if (request.isNetworkError || request.isHttpError){
            Debug.LogError("request.error:" + request.error);
        }
        else{
            try{
                clip = DownloadHandlerAudioClip.GetContent(request);
            }catch (Exception e){
                Debug.Log(e.Message);
            }
        }
        request.Dispose();
        return clip;
    }

    /// <summary>
    /// FFmpeg (http://ffmpeg.org) required
    /// </summary>
    /// <param name="path"></param>
    /// <param name="ffmpegEngine"></param>
    /// <returns></returns>
    public static async Task<AudioClip> GetAudioClipByFilePath(string path, FFmpegEngine ffmpegEngine){
        if (ffmpegEngine == null || !File.Exists(path)) { return null; }
        MediaFile mediaFile = new MediaFile(path);
        MetaData metaData = await ffmpegEngine.GetMetaDataAsync(mediaFile);
        if (metaData == null) { return null; }
        string format = metaData.AudioData.Format.Trim().ToLower();
        AudioClip audioClip = null;
        byte[] data = File.ReadAllBytes(path);
        if (format.StartsWith("mp3")){
            audioClip = WAV.Mp3ToClip(data, metaData);
        }
        else if (format.StartsWith("pcm")){
            audioClip = WAV.WavToClip(data, metaData);
        }
        else if (format.StartsWith("vorbis")){
            audioClip = WAV.OggToClip(data);
        }
        return audioClip;
    }

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
        if (!string.IsNullOrEmpty(detector.Charset)){
            if (detector.Confidence <= 0.6f){
                return Encoding.GetEncoding("shift_jis");
            }
            else{
                return Encoding.GetEncoding(detector.Charset);
            }
        }
        else { return Encoding.GetEncoding("shift_jis"); }
    }

    /// <summary>
    /// returns 0 if the string is null or the string dose't match ^[0-9a-zA-Z]{1,}$
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

    public static Texture2D GetTexture2D(string path){
        if (!File.Exists(path)) { return null; }
        Texture2D texture2D = new Texture2D(255, 255);
        //byte[] sourcce_bytes = File.ReadAllBytes(path);
        using (Image image = Image.FromFile(path)){
            using (MemoryStream tempStream = new MemoryStream()){
                if (Regex.IsMatch(path, @"\.bmp$", StaticClass.regexOption)
                    || image.RawFormat == ImageFormat.Bmp || image.RawFormat == ImageFormat.MemoryBmp
                ){
                    using (Bitmap bitmap = new Bitmap(image)){
                        bitmap.MakeTransparent(System.Drawing.Color.Black);
                        bitmap.Save(tempStream, ImageFormat.Png);
                    }
                }
                //else if (Regex.IsMatch(item.Value.ToString(), @"\.png$", StaticClass.regexOptions)){
                //    image.Save(tempStream, ImageFormat.Png);
                //}
                //else if (Regex.IsMatch(item.Value.ToString(), @"\.(jpg|jpeg)$", StaticClass.regexOptions)){
                //    image.Save(tempStream, ImageFormat.Jpeg);
                //}
                else{
                    //image.Save(tempStream, ImageFormat.Png);
                    image.Save(tempStream, image.RawFormat);
                }
                byte[] dist_bytes = new byte[tempStream.Length];
                tempStream.Seek(0, SeekOrigin.Begin);
                tempStream.Read(dist_bytes, 0, dist_bytes.Length);
                texture2D.LoadImage(dist_bytes);
                texture2D.Apply();
                //tempStream.Flush();
                //tempStream.Close();
            }
        }
        return texture2D;
    }
    
}
