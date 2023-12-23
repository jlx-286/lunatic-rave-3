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
    public const RegexOptions regexOption = RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant;
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
    private const string libc = "ucrtbase";
#elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX || PLATFORM_STANDALONE_LINUX
    private const string libc = "libc";
#else
    private const string libc = "libavcodec";
#endif
    [DllImport(libc)] public extern static void* memset(void* src, int val, UIntPtr count);
    [DllImport(libc)] public extern static void* memset(void* src, int val, IntPtr count);
    private static readonly Encoding Shift_JIS = Encoding.GetEncoding("shift_jis");
    private static readonly Encoding GB18030 = Encoding.GetEncoding("GB18030");
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
    private static readonly Regex alnumIdx = new Regex(@"^[\da-zA-Z]+$", regexOption);
    public static ushort Convert36To10(string s){
        if(s == null || !alnumIdx.IsMatch(s)) return 0;
        s = s.ToLower();
        const string digits = "0123456789abcdefghijklmnopqrstuvwxyz";
        ushort result = 0;
        for(int i = 0; i < s.Length; i++){
            result *= 36;
            result += (ushort)digits.IndexOf(s[i]);
        }
        return result;
    }

    public static string GaugeToString(this in decimal m){
        if(m >= 100) return "100.0";
        else if(m < 0.1m) return "0.0";
        decimal r = decimal.Round(m, 1);
        if(r > m) r -= 0.1m;
        return r.ToString("F1");
    }
    public static string RateToString(this in double rate){
        if(rate >= 100) return "100.00";
        if(rate > 0){
            double r = Math.Round(rate, 2);
            if(r > rate) r -= 0.01d;
            return r.ToString("F2");
        }
        return "0.00";
    }
    public static string RateToString(this in decimal rate){
        if(rate >= 100) return "100.00";
        if(rate > 0){
            decimal r = decimal.Round(rate, 2);
            if(r > rate) r -= 0.01m;
            return r.ToString("F2");
        }
        return "0.00";
    }
    public static readonly WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();
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
    public static readonly string[] judge_tmp = new string[(byte)NoteJudge.HCN + 1]{
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
            default: return " ";// break;
        }
        return builder.ToString();
    }
    /*public static bool TryParseDecimal(string s, out decimal m){
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
    }*/
    public static BigInteger NextBigInteger(this System.Random random, BigInteger max){
        if(max < 1) return 0;
        if(max == 1) return 1;
        byte[] src = max.ToByteArray();
        BigInteger result;
        do{
            random.NextBytes(src);
            result = new BigInteger(src);
        }while(result < 1 || result > max);
        return result;
    }
    private static readonly RandomNumberGenerator rng = RandomNumberGenerator.Create();
// #if UNITY_EDITOR || UNITY_STANDALONE
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init(){
        Application.quitting += ()=>{
            rng.Dispose();
            Debug.Log("rng.Dispose");
        };
    }
// #else
//     static StaticClass(){
//         rng.Dispose();
//     }
// #endif
    public static BigInteger NextBigInteger(BigInteger max){
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
    public static Texture2D ToTexture2D(this Sprite sprite){
        Texture2D src = sprite.texture;
        Rect rect = sprite.rect;
        // rect = sprite.textureRect;
        Texture2D dst = new Texture2D((int)rect.width, (int)rect.height,
            src.format, false){filterMode = src.filterMode};
        dst.Apply(false, true);
        Graphics.CopyTexture(src, 0, 0, (int)rect.x, (int)rect.y,
            (int)rect.width, (int)rect.height , dst, 0, 0, 0, 0);
        return dst;
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
