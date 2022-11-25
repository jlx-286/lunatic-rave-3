using NAudio.Wave;
using NLayer;
using NVorbis;
using System;
using System.Collections;
using System.Collections.Generic;
using SkiaSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Ude;
using UnityEngine;
using Image = SixLabors.ImageSharp.Image;

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
    // public static readonly double OverFlowTime = 4.398046511E12;
    // public static readonly double OverFlowTime = 4398046511104d;
    public static readonly double OverFlowTime = Math.Pow(2, 42) - Math.Pow(2, -52) - 1;
#else
    public static readonly double OverFlowTime = Mathf.Pow(2, 13) - Mathf.Pow(2, -23) - 1;
    // public static readonly double OverFlowTime = 2 * 3600 + 20 * 60;
#endif

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
            // fileStream.Close();
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

    /// <summary>
    /// Depends on SkiaSharp & SixLabors.ImageSharp
    /// </summary>
    /// <param name="path"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns>Color32[] (use Texture2D.SetPixels32(Color32[]))</returns>
    public static Color32[] GetTextureInfo(string path, out int width, out int height){
        width = height = 0;
        if(!File.Exists(path)) return null;
        byte[] source = File.ReadAllBytes(path);
        SKBitmap bitmap = SKBitmap.Decode(source);//.Copy(SKColorType.Rgba8888);
        width = bitmap.Width;
        height = bitmap.Height;
        byte[] dist = bitmap.Bytes;
        bitmap.Dispose();
        int index1, index2;
        int maxSize = Math.Max(width, height);
        Color32[] color32s = null;
        if (Regex.IsMatch(path, @"\.bmp$", StaticClass.regexOption)
            || Regex.IsMatch(Image.DetectFormat(source).Name, @"bmp", StaticClass.regexOption)
        ){
            color32s = new Color32[maxSize * maxSize];
            for (int h = 0; h < height; h++){
                for(int w = 0; w < width; w++){
                    index1 = width * (h + maxSize - height) + (w + maxSize - width);
                    index2 = (w + (height - h - 1) * width) * 4;
                    color32s[index1].b = dist[index2];
                    color32s[index1].g = dist[index2 + 1];
                    color32s[index1].r = dist[index2 + 2];
                    if(color32s[index1].b < 5 && color32s[index1].g < 5 && color32s[index1].r < 5){
                        color32s[index1].a = 0;
                    }else color32s[index1].a = dist[index2 + 3];
                }
            }
            width = height = maxSize;
        }else{
            color32s = new Color32[width * height];
            //color32s = new Color32[maxSize * maxSize];
            for (int h = 0; h < height; h++){
                for(int w = 0; w < width; w++){
                    //index1 = width * (h + maxSize - height) + (w + maxSize - width);
                    index1 = width * h + w; index2 = (w + (height - h - 1) * width) * 4;
                    color32s[index1].b = dist[index2];
                    color32s[index1].g = dist[index2 + 1];
                    color32s[index1].r = dist[index2 + 2];
                    color32s[index1].a = dist[index2 + 3];
                }
            }
        }
        return color32s;
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
                    VorbisReader vorbisReader = new VorbisReader(path);
                    channels = vorbisReader.Channels;
                    frequency = vorbisReader.SampleRate;
                    result = new float[vorbisReader.TotalSamples * channels];
                    vorbisReader.ReadSamples(result, 0, result.Length);
                    vorbisReader.Dispose();
                    // vorbisReader = null;
                }catch(Exception e){
                    Debug.LogWarning(e.GetBaseException());
                }
                break;
            case AudioFormat.Mpeg:
                try{
                    MpegFile mpegFile = new MpegFile(path);
                    channels = mpegFile.Channels;
                    frequency = mpegFile.SampleRate;
                    result = new float[mpegFile.Length / sizeof(float)];
                    mpegFile.ReadSamples(result, 0, result.Length);
                    mpegFile.Dispose();
                    // mpegFile = null;
                }catch(Exception e){
                    Debug.LogWarning(e.GetBaseException());
                }
                break;
            case AudioFormat.Others:
                try{
                    AudioFileReader audioFileReader = new AudioFileReader(path);
                    channels = audioFileReader.WaveFormat.Channels;
                    frequency = audioFileReader.WaveFormat.SampleRate;
                    result = new float[audioFileReader.Length / sizeof(float)];
                    audioFileReader.Read(result, 0, result.Length);
                    // audioFileReader.Flush();
                    audioFileReader.Dispose();
                    // audioFileReader = null;
                }
                catch(Exception e){
                    channels = frequency = 0;
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
                        channels = frequency = 0;
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
                        channels = frequency = 0;
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
    public static bool TryParseDecimal(string s, out decimal m){
        m = decimal.Zero;
        if (string.IsNullOrEmpty(s) || string.IsNullOrWhiteSpace(s)) return false;
        s = s.Trim();
        bool res = false;
        BigInteger bigInteger;
        if(Regex.IsMatch(s, @"^(\+|\-)?0x[0-9a-f]{1,}", StaticClass.regexOption)){
            s = Regex.Match(s, @"^(\+|\-)?0x[0-9a-f]{1,}", StaticClass.regexOption).Value;
            //s = Regex.Match(s, @"^(+|-)?0x[0-9a-f]{1,}", StaticClass.regexOption).Groups[0].Value;
            //s = Regex.Match(s, @"^(+|-)?0x[0-9a-f]{1,}", StaticClass.regexOption).Captures[0].Value;
            //s = Regex.Match(s, @"^(+|-)?0x[0-9a-f]{1,}", StaticClass.regexOption).Groups[0].Captures[0].Value;
            bool minus = false;
            switch (s[0]) {
                case '0': case '+': minus = false; break;
                case '-': minus = true; break;
                default: break;
            }
            s = s.Substring(s.IndexOf('0'));
            s = new StringBuilder(s).Remove(s.IndexOf('0') + 1, 1).ToString();
            //s = s.Remove(s.IndexOf('0') + 1, 1);
            res = BigInteger.TryParse(s, NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo, out bigInteger);
            if (minus && res) bigInteger *= -1;
            if (bigInteger > (BigInteger)decimal.MaxValue)
                m = decimal.MaxValue;
            else if (bigInteger < (BigInteger)decimal.MinValue)
                m = decimal.MinValue;
            else m = (decimal)bigInteger;
        }
        else if(Regex.IsMatch(s, @"^(\+|\-)?\d{1,}(\.\d{1,})?(e(\+|\-)?\d{1,})?", StaticClass.regexOption)){
            s = Regex.Match(s, @"^(\+|\-)?\d{1,}(\.\d{1,})?(e(\+|\-)?\d{1,})?", StaticClass.regexOption).Value;
            // Debug.Log(s);
            res = BigInteger.TryParse(s, NumberStyles.Any & (~NumberStyles.AllowCurrencySymbol), NumberFormatInfo.InvariantInfo, out bigInteger);
            // Debug.Log(bigInteger);
            if (bigInteger >= (BigInteger)decimal.MaxValue)
                m = decimal.MaxValue;
            else if (bigInteger <= (BigInteger)decimal.MinValue)
                m = decimal.MinValue;
            else res = decimal.TryParse(s, NumberStyles.Any & (~NumberStyles.AllowCurrencySymbol), NumberFormatInfo.InvariantInfo, out m);
        }
        return res;
    }
    private static int c;
    private static ulong gcd(ulong a, ulong b){
        if (a == b || a == 0) return b;
        if (b == 0) return a;
        c = 0;
        while (((a & 0x1) == 0) && ((b & 0x1) == 0)){
            a = a >> 1; b = b >> 1; c++;
        }
        while ((a & 0x1) == 0) a = a >> 1;
        while ((b & 0x1) == 0) b = b >> 1;
        while(true){
            if (a == 0) return b << c;
            if (b == 0) return a << c;
            if (a < b){
                b = (b - a) >> 1;
                //b -= a;
                while ((b & 0x1) == 0) b = b >> 1;
            }
            else if(a > b){
                a = (a - b) >> 1;
                //a -= b;
                while ((a & 0x1) == 0) a = a >> 1;
            }
            else if(a == b) return b << c;
            /*else{
                a = (a - b) >> 1;
            }*/
        }
    }
    public static ulong Lcm(SortedSet<ulong> _set_){
        if (_set_ == null || _set_.Count < 1)
            return 0;//error
        if (_set_.Min < 1){
            //_set_.Clear();
            return 0;//error
        }
        if (_set_.Max == 1){
            return 1;
        }
        _set_.Remove(1);
        if (_set_.Count == 1){
            return _set_.Max;
        }
        ulong[] integers = new ulong[_set_.Count];
        _set_.CopyTo(integers);
        ulong result = integers[0];
        for(int i = 1; i < integers.Length; i++){
            result = result / (ulong)BigInteger.GreatestCommonDivisor(result, integers[i]) * integers[i];
            // result = result / gcd(result, integers[i]) * integers[i];
        }
        return result;
    }
}
