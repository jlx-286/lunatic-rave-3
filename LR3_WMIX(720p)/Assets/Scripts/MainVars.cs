using FFmpeg.NET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ude;
//using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using Debug = UnityEngine.Debug;
using FFmpegEngine = FFmpeg.NET.Engine;

public class MainVars : MonoBehaviour {
    public static string bms_file_path;
    public static string cur_scene_name;
    public static sbyte freq, pitch;
    //public static string bms_root_dir;
    public static byte master_vol, bgm_vol, key_vol;
    public static byte reverb, reverb_level;
    public static byte hipass, hipass_level;
    public static byte lowpass, lowpass_level;
    public static byte delay, delay_level;
    public static byte flanger, flanger_level;
    public static byte chorus, chorus_level;
    private Dictionary<string,object> dict;
    public static Dictionary<string,Dictionary<string,object>> FXs;
    public AudioMixer mixer;
    //public AudioSource bgmAudioSource;
    //public AudioSource keyAudioSource;
    //public AudioSource audioSource;
    //public static AudioMixerGroup mixerGroup;
    [HideInInspector] public enum FFmpegConverting{
        Converting = 0,
        Success = 1,
        Error = 2,
        Completed = 3
    }
    public static FFmpegEngine ffmpegEngine;
    private MetaData metaData;
    public static FFmpegConverting converting;
    public static FFmpegConverting prevConverting;
    // Use this for initialization
    private void Start () {
        cur_scene_name = "Main";
        bms_file_path = string.Empty;
        freq = pitch = 0;
        master_vol = bgm_vol = key_vol = 100;
        mixer.SetFloat("freq", Mathf.Pow(2f, freq / 12f));
        mixer.SetFloat("pitch", Mathf.Pow(2f, pitch / 12f));
        dict = new Dictionary<string, object>{
            { "enabled", false },
            { "value", (byte)0 },
            { "level", (byte)0 }
        };
        FXs = new Dictionary<string, Dictionary<string, object>>{
            { "reverb", dict },
            { "hipass", dict },
            { "lowpass", dict },
            { "chorus", dict },
            { "flanger", dict },
            { "distortion", dict },
            { "delay", dict }
        };
        JObject jObject = JObject.Parse(File.ReadAllText(Application.dataPath + "/config.json"));
        Debug.Log(jObject["FFmpegPath"]["Windows"]);
        switch (Application.platform){
            case RuntimePlatform.WindowsEditor:
            case RuntimePlatform.WindowsPlayer:
            case RuntimePlatform.WSAPlayerARM:
            case RuntimePlatform.WSAPlayerX64:
            case RuntimePlatform.WSAPlayerX86:
                ffmpegEngine = new FFmpegEngine(jObject["FFmpegPath"]["Windows"].ToString());
                break;
            case RuntimePlatform.LinuxEditor:
            case RuntimePlatform.LinuxPlayer:
                ffmpegEngine = new FFmpegEngine(jObject["FFmpegPath"]["Linux"].ToString());
                break;
            case RuntimePlatform.OSXEditor:
            case RuntimePlatform.OSXPlayer:
                ffmpegEngine = new FFmpegEngine(jObject["FFmpegPath"]["Mac"].ToString());
                break;
        }
        converting = FFmpegConverting.Completed;
        //mixerGroup = mixer.outputAudioMixerGroup;
    }
	
	// Update is called once per frame
	private void Update () {}

    /// <summary>
    /// 
    /// </summary>
    /// <param name="s">length should be 2</param>
    /// <returns>ushort number</returns>
    public static ushort Convert36To10(string s){
        if (s == null || s.Length != 2){
            return 0;
        }else{
            s = s.ToLower();
            string digits = "0123456789abcdefghijklmnopqrstuvwxyz";
            ushort result = 0;
            ushort.TryParse((digits.IndexOf(s[0]) * 36 + digits.IndexOf(s[1])).ToString(), out result);
            return result;
        }
    }

    public static string Convert10To36(ushort u){
        if (u >= 36 * 36){
            return "ZZ";
        }
        StringBuilder result = new StringBuilder();
        string digits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        result.Append(digits[u / 36]).Append(digits[u % 36]);
        return result.ToString();
    }

    public AudioClip GetAudioClipByFilePath(string path){
        if (!File.Exists(path)) { return null; }
        Task.Run(async () => {
            MediaFile mediaFile = new MediaFile(path);
            metaData = await ffmpegEngine.GetMetaDataAsync(mediaFile);
        }).Wait();
        Debug.Log(metaData);
        if(metaData == null) { return null; }
        string format = metaData.AudioData.Format.Trim().ToLower();
        AudioClip audioClip = null;
        byte[] data = File.ReadAllBytes(path);
        if (format.StartsWith("mp3")){
            audioClip = WAV.Mp3ToClip(data, metaData);
        }else if (format.StartsWith("pcm")){
            audioClip = WAV.WavToClip(data, metaData);
        }else if (format.StartsWith("vorbis")){
            audioClip = WAV.OggToClip(data);
        }
        return audioClip;
    }
    
    public static async void ConvertVideoByFilePath(string input, string output){
        input = input.Replace("file://", "");
        if (!File.Exists(input)){
            //throw new Exception("no such file");
            return;
        }
        string fileName;
        fileName = new FileInfo(input).Name;
        //fileName = Path.GetFileNameWithoutExtension(url);
        //fileName = Path.GetFileName(url);
        Engine ffmpeg = new Engine(Application.dataPath + "/FFmpeg/Windows/ffmpeg.exe");
        MediaFile inputFile = new MediaFile(input);
        MediaFile outputFile = new MediaFile(output);
        prevConverting = converting;
        converting = FFmpegConverting.Converting;
        await ffmpeg.ConvertAsync(inputFile, outputFile);
        //completed = true;
        converting = FFmpegConverting.Completed;
        return;
        //Engine ffprobe = new Engine(Application.dataPath + "/FFmpeg/Windows/ffprobe.exe");
    }

    public static Encoding GetEncodingByFilePath(string bms_path){
        CharsetDetector detector = new CharsetDetector();
        Debug.Log(Application.systemLanguage);
        Debug.Log(Encoding.Default);
        using (FileStream fileStream = File.OpenRead(bms_path)){
            detector.Feed(fileStream);
            detector.DataEnd();
            fileStream.Flush();
            fileStream.Close();
        }
        if(!string.IsNullOrEmpty(detector.Charset)){
            Debug.Log($"charset:{detector.Charset},confidence:{detector.Confidence}");
            if (detector.Confidence <= 0.6f){
                //return Encoding.Default;
                return Encoding.GetEncoding("shift_jis");
            }
            //else if (detector.Confidence >= 0.98f){
            //    return Encoding.GetEncoding("gb18030");
            //}
            else {
                return Encoding.GetEncoding(detector.Charset);
            }
        }
        else { return Encoding.GetEncoding("shift_jis"); }
    }
}
