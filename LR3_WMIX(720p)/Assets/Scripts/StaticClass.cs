using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
// using System.Threading.Tasks;
using Ude;
using UnityEngine;
public unsafe static class StaticClass{
    public const RegexOptions regexOption = RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
    private const string PluginName = "FFmpegPlugin";
    [DllImport(PluginName, EntryPoint = "GetVideoSize")] private extern static void __GetVideoSize(
        string url, out int width, out int height);
    [DllImport(PluginName)] private extern static bool GetAudioInfo(
        string url, out int channels, out int frequency, out ulong length);
    [DllImport(PluginName)] private extern static void CopyAudioSamples(float* addr);
    [DllImport(PluginName)] private extern static bool GetPixelsInfo(
        string url, out int width, out int height, out bool isBitmap);
    [DllImport(PluginName)] private extern static void CopyPixels(
        void* addr, int width, int height, bool isBitmap, bool strech = false);
    [DllImport(PluginName, EntryPoint = "CleanUp")] public extern static void FFmpegCleanUp();
/*
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
*/
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    [DllImport("ucrtbase")] public extern static void* memset(void* src, int val, UIntPtr count);
    [DllImport("ucrtbase")] public extern static void* memset(void* src, int val, IntPtr count);
#else
    [DllImport("libavcodec")] public extern static void* memset(void* src, int val, UIntPtr count);
    [DllImport("libavcodec")] public extern static void* memset(void* src, int val, IntPtr count);
#endif
    private static readonly Encoding Shift_JIS = Encoding.GetEncoding("shift_jis");
    private static readonly Encoding GB18030 = Encoding.GetEncoding("GB18030");
    /// <summary>
    /// using Ude;
    /// </summary>
    /// <param name="path">text file path</param>
    /// <returns></returns>
    public static Encoding GetEncodingByFilePath(string path){
        CharsetDetector detector = new CharsetDetector();
        using(FileStream fileStream = File.OpenRead(path)){
            detector.Feed(fileStream);
            detector.DataEnd();
            fileStream.Flush();
            // fileStream.Close();
        }
        if(!string.IsNullOrWhiteSpace(detector.Charset)){
            try{
                Encoding encoding = Encoding.GetEncoding(detector.Charset);
                if(encoding != Shift_JIS && detector.Confidence <= 0.7f
                    && detector.Confidence > 0.6f) return GB18030;
                else if(detector.Confidence <= 0.6f) return Shift_JIS;
                else return encoding;
            }catch{ return Shift_JIS; }
        }
        else return Shift_JIS;
    }

    /// <summary>
    /// returns 0 if the string is null or the string doesn't match ^[\d\w]+$
    /// </summary>
    /// <param name="s"></param>
    /// <returns>ushort number</returns>
    public static ushort Convert36To10(string s){
        if(s == null || !Regex.IsMatch(s, @"^[\d\w]+$", StaticClass.regexOption)){
            return 0;
        }
        s = s.ToLower();
        const string digits = "0123456789abcdefghijklmnopqrstuvwxyz";
        ushort result = 0;
        for(int i = 0; i < s.Length; i++){
            result *= 36;
            result += (ushort)digits.IndexOf(s[i]);
        }
        return result;
    }

    /// <summary>
    /// Depends on FFmpeg libraries
    /// </summary>
    /// <param name="path"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns>Color32[] (use Texture2D.SetPixels32(Color32[]))</returns>
    public static Color32[] GetTextureInfo(string path, out int width, out int height){
        width = height = 0;
        if(!File.Exists(path)) return null;
        Color32[] color32s = null;
        bool isBitmap;
        if(GetPixelsInfo(path, out width, out height, out isBitmap)){
            int max = Math.Max(width, height);
            ulong length = (ulong)max;
            length *= length;
            if(length <= int.MaxValue) color32s = new Color32[length];
            fixed(void* p = color32s)
                CopyPixels(p, width, height, isBitmap
                || Regex.IsMatch(path, @"\.bmp$", regexOption));
            width = height = max;
        }
        return color32s;
    }
    public static Color32[] GetStageImage(string path, out int width, out int height){
        width = height = 0;
        if(!File.Exists(path)) return null;
        Color32[] color32s = null;
        bool isBitmap;
        if(GetPixelsInfo(path, out width, out height, out isBitmap)){
            color32s = new Color32[width * height];
            fixed(void* p = color32s)
                CopyPixels(p, width, height, false, true);
        }
        return color32s;
    }
    public static float[] AudioToSamples(string path, out int channels, out int frequency){
        if(!File.Exists(path)){
            channels = frequency = 0;
            // Debug.Log(path);
            return null;
        }
        float[] result = null;
        ulong length;
        if(GetAudioInfo(path, out channels, out frequency, out length) && length <= int.MaxValue)
            result = new float[length];
        else Debug.LogWarning(path + ":Invalid data or too long data");
        fixed(float* p = result) CopyAudioSamples(p);
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
        return result;
    }
    public static bool GetVideoSize(string path, out int width, out int height){
        if(!File.Exists(path)){
            width = height = 0;
            return false;
        }
        __GetVideoSize(path, out width, out height);
        return (width > 0 && height > 0);
    }
    public static string GaugeToString(this in decimal m){
        if(m >= 100) return "100.0";
        else if(m < 0.1m) return "0.0";
        decimal r = decimal.Round(m, 1);
        if(r > m) r -= 0.1m;
        return r.ToString("F1");
    }
    private static readonly string[] tmp_digits = new string[10]{
        "<sprite index=0>",
        "<sprite index=6>",
        "<sprite index=12>",
        "<sprite index=18>",
        "<sprite index=24>",
        "<sprite index=30>",
        "<sprite index=36>",
        "<sprite index=42>",
        "<sprite index=48>",
        "<sprite index=54>",
    };
    private static readonly string[] blink_digits = new string[10]{
        "<sprite anim=\"0,5,61\">",
        "<sprite anim=\"6,11,61\">",
        "<sprite anim=\"12,17,61\">",
        "<sprite anim=\"18,23,61\">",
        "<sprite anim=\"24,29,61\">",
        "<sprite anim=\"30,35,61\">",
        "<sprite anim=\"36,41,61\">",
        "<sprite anim=\"42,47,61\">",
        "<sprite anim=\"48,53,61\">",
        "<sprite anim=\"54,59,61\">",
    };
    public static readonly string[] judge_tmp = new string[(byte)NoteJudge.Landmine + 1]{
        "<sprite index=60>",
        "<sprite index=60>",
        "<sprite index=61>",
        "<sprite index=62>",
        "<sprite index=63>",
        "<sprite anim=\"64,69,61\">",
        "<sprite index=60>",
        "<sprite index=60>",
    };
    public static string ComboNumToTMP(this StringBuilder builder, in ulong num, in NoteJudge noteJudge){
        if(builder == null) builder = new StringBuilder(80);
        else builder.Clear();
        char[] digits;
        switch(noteJudge){
            case NoteJudge.Perfect:
                digits = num.ToString().ToCharArray();
                for(int i = 0; i < digits.Length; i++)
                    builder.Append(blink_digits[digits[i] - '0']);
                break;
            case NoteJudge.Great: case NoteJudge.Good:
                digits = num.ToString().ToCharArray();
                for(int i = 0; i < digits.Length; i++)
                    builder.Append(tmp_digits[digits[i] - '0']);
                break;
            default: return "";// break;
        }
        return builder.ToString();
    }
    public static bool TryParseDecimal(string s, out decimal m){
        m = decimal.Zero;
        if(string.IsNullOrWhiteSpace(s)) return false;
        s = s.Trim();
        bool res = false;
        BigInteger bigInteger;
        if(Regex.IsMatch(s, @"^[\+-]?0x[\da-f]+", StaticClass.regexOption)){
            s = Regex.Match(s, @"^[\+-]?0x[\da-f]+", StaticClass.regexOption).Value;
            //s = Regex.Match(s, @"^[\+-]?0x[\da-f]+", StaticClass.regexOption).Groups[0].Value;
            //s = Regex.Match(s, @"^[\+-]?0x[\da-f]+", StaticClass.regexOption).Captures[0].Value;
            //s = Regex.Match(s, @"^[\+-]?0x[\da-f]+", StaticClass.regexOption).Groups[0].Captures[0].Value;
            bool minus = false;
            switch(s[0]){
                case '0': case '+': minus = false; break;
                case '-': minus = true; break;
                default: break;
            }
            // s = s.Substring(s.IndexOf('0') + 1);
            s = s.TrimStart('+').TrimStart('-').Substring(2);
            res = BigInteger.TryParse(s, NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo, out bigInteger);
            if(minus && res) bigInteger *= -1;
            if(bigInteger > (BigInteger)decimal.MaxValue)
                m = decimal.MaxValue;
            else if(bigInteger < (BigInteger)decimal.MinValue)
                m = decimal.MinValue;
            else m = (decimal)bigInteger;
        }
        else if(Regex.IsMatch(s, @"^[\+-]?\d+(\.\d+)?(e[\+-]?\d+)?", StaticClass.regexOption)){
            s = Regex.Match(s, @"^[\+-]?\d+(\.\d+)?(e[\+-]?\d+)?", StaticClass.regexOption).Value;
            // Debug.Log(s);
            res = BigInteger.TryParse(s, NumberStyles.Any & (~NumberStyles.AllowCurrencySymbol), NumberFormatInfo.InvariantInfo, out bigInteger);
            // Debug.Log(bigInteger);
            if(bigInteger >= (BigInteger)decimal.MaxValue)
                m = decimal.MaxValue;
            else if(bigInteger <= (BigInteger)decimal.MinValue)
                m = decimal.MinValue;
            else res = decimal.TryParse(s, NumberStyles.Any & (~NumberStyles.AllowCurrencySymbol), NumberFormatInfo.InvariantInfo, out m);
        }
        return res;
    }
    public static BigInteger NextBigInteger(this System.Random random, BigInteger max){
        if(max < 1) return 0;
        if(max == 1) return 1;
        byte[] src = max.ToByteArray();
        // byte[] src = new byte[max.ToByteArray().Length];
        BigInteger result;
        do{
            random.NextBytes(src);
            // randomNumberGenerator.GetBytes(src);
            result = new BigInteger(src);
        }while(result < 1 || result > max);
        return result;
    }
    public static BigInteger NextBigInteger(this RandomNumberGenerator rng, BigInteger max){
        if(max < 1) return 0;
        if(max == 1) return 1;
        byte[] src = max.ToByteArray();
        BigInteger result;
        do{
            rng.GetBytes(src);
            result = new BigInteger(src);
        }while(result < 1 || result > max);
        return result;
    }
    public static ulong gcd(ulong a, ulong b){
        if(a == 0 || a == b) return b;
		if(b == 0) return a;
		byte c = 0;
		while(((a & 1) == 0) && ((b & 1) == 0)){
			a >>= 1; b >>= 1; c++;
		}
		while(((a & 1) == 0)) a >>= 1;
		while(((b & 1) == 0)) b >>= 1;
		while(true){
			if(a == 0 || a == b) return b << c;
			if(b == 0) return a << c;
			if(a < b){
				b = (b - a) >> 1;
				// b -= a;
				while(((b & 1) == 0)) b >>= 1;
			}
			else if(a > b){
				a = (a - b) >> 1;
				// a -= b;
				while(((a & 1) == 0)) a >>= 1;
			}
		}
    }
    private struct EqualityComparer<T> : IEqualityComparer<T> where T : struct{
        private readonly Func<T, T, bool> _func;
        public EqualityComparer(Func<T, T, bool> func){ _func = func; }
        public bool Equals(T x, T y) => _func(x, y);
        public int GetHashCode(T obj) => 0;
    }
    public static IEnumerable<T> Distinct<T>(
        this IEnumerable<T> source, Func<T, T, bool> comparer)
        where T : struct => source.Distinct(new EqualityComparer<T>(comparer));
}
