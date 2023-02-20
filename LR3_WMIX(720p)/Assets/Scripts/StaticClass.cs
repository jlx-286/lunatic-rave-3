using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
// using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
// using System.Threading.Tasks;
using Ude;
using UnityEngine;
public static class StaticClass{
    public const RegexOptions regexOption = RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
    public const ushort Base36ArrLen = (ushort)36*36-1;
    private const string PluginName = "FFmpegPlugin";
    [DllImport(PluginName, EntryPoint = "GetVideoSize")] private extern static bool __GetVideoSize(
        string url, out int width, out int height);
    [DllImport(PluginName)] private extern static bool GetAudioInfo(
        string url, out int channels, out int frequency, out ulong length);
    [DllImport(PluginName)] private extern static unsafe void CopyAudioSamples(
        float* addr);
    [DllImport(PluginName)] private extern static bool GetPixelsInfo(
        string url, out int width, out int height, out bool isBitmap);
    [DllImport(PluginName)] private extern static unsafe void CopyPixels(
        void* addr, int width, int height, bool isBitmap);
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
        Encoding Shift_JIS = Encoding.GetEncoding("shift_jis");
        // Debug.Log(detector.Charset);
        // Debug.Log(detector.Confidence);
        if(!string.IsNullOrEmpty(detector.Charset)){
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
        if(s == null || !Regex.IsMatch(s, @"^[0-9a-z]{1,}$", StaticClass.regexOption)){
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
            if(length <= int.MaxValue){
                if(!isBitmap && Regex.IsMatch(path, @"\.bmp$", regexOption))
                    isBitmap = true;
                color32s = new Color32[length];
                unsafe{fixed(void* p = color32s){
                    CopyPixels(p, width, height, isBitmap);
                }}
                width = height = max;
            }
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
        if(GetAudioInfo(path, out channels, out frequency, out length) && length <= int.MaxValue){
            result = new float[length];
            unsafe{fixed(float* p = result) CopyAudioSamples(p); }
        }
        else Debug.LogWarning(path + ":Invalid data or too long data");
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
        if(!__GetVideoSize(path, out width, out height) || width < 1 || height < 1)
            return false;
        return true;
    }
    public static bool TryParseDecimal(string s, out decimal m){
        m = decimal.Zero;
        if(string.IsNullOrEmpty(s) || string.IsNullOrWhiteSpace(s)) return false;
        s = s.Trim();
        bool res = false;
        BigInteger bigInteger;
        if(Regex.IsMatch(s, @"^(\+|\-)?0x[0-9a-f]{1,}", StaticClass.regexOption)){
            s = Regex.Match(s, @"^(\+|\-)?0x[0-9a-f]{1,}", StaticClass.regexOption).Value;
            //s = Regex.Match(s, @"^(\+|\-)?0x[0-9a-f]{1,}", StaticClass.regexOption).Groups[0].Value;
            //s = Regex.Match(s, @"^(\+|\-)?0x[0-9a-f]{1,}", StaticClass.regexOption).Captures[0].Value;
            //s = Regex.Match(s, @"^(\+|\-)?0x[0-9a-f]{1,}", StaticClass.regexOption).Groups[0].Captures[0].Value;
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
        else if(Regex.IsMatch(s, @"^(\+|\-)?\d{1,}(\.\d{1,})?(e(\+|\-)?\d{1,})?", StaticClass.regexOption)){
            s = Regex.Match(s, @"^(\+|\-)?\d{1,}(\.\d{1,})?(e(\+|\-)?\d{1,})?", StaticClass.regexOption).Value;
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
    // private static RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create();
    // private static RNGCryptoServiceProvider rNGCryptoServiceProvider = new RNGCryptoServiceProvider();
    public static BigInteger NextBigInteger(this System.Random random, BigInteger max){
        // string match = Regex.Match(s, @"^\s{0,}\+?0{0,}\d{1,}").Value;
        // if(string.IsNullOrEmpty(match)) return 0;
        // BigInteger max = BigInteger.Parse(match, NumberStyles.AllowLeadingSign
        //     | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite
        //     | NumberStyles.Integer | NumberStyles.Number
        //     , NumberFormatInfo.InvariantInfo);
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
    public static uint gcd(uint a, uint b){
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
    private sealed class EqualityComparer<T> : IEqualityComparer<T> where T : class{
        private readonly Func<T, T, bool> _func;
        public EqualityComparer(Func<T, T, bool> func){ _func = func; }
        public bool Equals(T x, T y) => _func(x, y);
        public int GetHashCode(T obj) => 0;
    }
    public static IEnumerable<T> Distinct<T>(
        this IEnumerable<T> source, Func<T, T, bool> comparer)
        where T : class => source.Distinct(new EqualityComparer<T>(comparer));
    /*public unsafe static uint TryGetGCD(int a, int b){
        uint aa,bb;
        if(a == int.MinValue) aa = *(uint*)&a;
        else if(a < 0) aa = (uint)Math.Abs(a);
        else aa = (uint)a;
        if(b == int.MinValue) bb = *(uint*)&b;
        else if(b < 0) bb = (uint)Math.Abs(b);
        else bb = (uint)b;
		return gcd(aa, bb);
	}
    
    public static BigInteger LCM(SortedSet<int> s){
        if(s == null || s.Count < 1 || s.Min < 1) return 0;
        s.Remove(1);
        if(s.Count < 1) return 1;
        if(s.Count == 1) return s.Min;
        int[] integers = new int[s.Count];
        s.CopyTo(integers);
        BigInteger result = integers[0];
        for(int i = 1; i < integers.Length; i++){
            //result *= integers[i] / (int)BigInteger.GreatestCommonDivisor(result, integers[i]);
            result *= integers[i] / BigInteger.GreatestCommonDivisor(result, integers[i]);
        }
        return result;
    }*/
}
