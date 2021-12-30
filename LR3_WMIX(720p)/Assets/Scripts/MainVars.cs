using FFmpeg.NET;
using Microsoft.Scripting.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shell32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
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
using Debug = UnityEngine.Debug;

public class MainVars : MonoBehaviour {
    public static string bms_file_path;
    public static string cur_scene_name;
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
    public AudioSource bgmAudioSource;
    public AudioSource keyAudioSource;
    //public AudioSource audioSource;
    //public static AudioMixerGroup mixerGroup;
    public static sbyte freq, pitch;
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
        StringBuilder result = new StringBuilder();
        string digits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        result.Append(digits[u / 36]).Append(digits[u % 36]);
        return result.ToString();
    }

    public static AudioClip GetAudioClipByFilePath(string path){
        string jString = string.Empty;
        try{
            using (Process process = new Process()){
                string arguments = string.Empty;
                string programFile = string.Empty;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                if(Application.platform == RuntimePlatform.WindowsEditor
                    || Application.platform == RuntimePlatform.WindowsPlayer
                    //|| Application.platform == RuntimePlatform.WindowsServer
                ){
                    process.StartInfo.FileName = "cmd";
                    programFile = "Windows/ffmpeg/ffprobe.exe";
                }
                else if (Application.platform == RuntimePlatform.LinuxEditor
                    || Application.platform == RuntimePlatform.LinuxPlayer
                ){
                    process.StartInfo.FileName = "/bin/bash";
                    programFile = "Linux/ffmpeg/ffprobe";
                }
                else if (Application.platform == RuntimePlatform.OSXEditor
                    || Application.platform == RuntimePlatform.OSXPlayer
                ){
                    //process.StartInfo.FileName = "/bin/bash";
                    process.StartInfo.FileName = "/Applications/Utilities/Terminal.app/Contents/MacOS/Terminal";
                    programFile = "Mac/ffmpeg/ffprobe";
                }
                else { return null; }
                arguments =
                    //"ffprobe"
                    $"\"{Application.dataPath}/Programs/{programFile}\""
                    + $" -v quiet -print_format json -show_format -show_streams \"{path}\"";
                //#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                //#endif
                //process.StartInfo.Arguments = process.StartInfo.Arguments.Replace('\\', '/');
                process.Start();
                process.StandardInput.WriteLine(arguments);
                process.StandardInput.Close();
                jString = process.StandardOutput.ReadToEnd();
                jString = jString.Substring(jString.IndexOf('{'), jString.LastIndexOf('}') - jString.IndexOf('{') + 1);
                //process.WaitForExit();
                process.Close();
            }
        }
        catch (Exception e){
            Debug.Log(e.Message);
            return null;
        }
        JObject jObject = JObject.Parse(jString);
        if (jObject == null
            || jObject["streams"] == null
            || jObject["streams"][0] == null
            || jObject["streams"][0]["codec_name"] == null
        ){ return null; }
        string extension = string.Empty;
        extension = jObject["streams"][0]["codec_name"].ToString().ToLower();
        AudioClip audioClip = null;
        byte[] data = File.ReadAllBytes(path);
        if (extension.StartsWith("mp3")){
            audioClip = WAV.Mp3ToClip(data, jObject);
        }else if (extension.StartsWith("pcm")){
            audioClip = WAV.WavToClip(data, jObject);
        }else if (extension.StartsWith("vorbis")){
            audioClip = WAV.OggToClip(data);
        }
        return audioClip;
    }

    public static bool ConvertVideoByFilePath(string input, string output){
        try{
            using (Process process = new Process()){
                string arguments = string.Empty;
                string programFile = string.Empty;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                if (Application.platform == RuntimePlatform.WindowsEditor
                    || Application.platform == RuntimePlatform.WindowsPlayer
                    //|| Application.platform == RuntimePlatform.WindowsServer
                ){
                    process.StartInfo.FileName = "cmd";
                    programFile = "Windows/ffmpeg.exe";
                }
                else if (Application.platform == RuntimePlatform.LinuxEditor
                    || Application.platform == RuntimePlatform.LinuxPlayer
                ){
                    process.StartInfo.FileName = "/bin/bash";
                    programFile = "Linux/ffmpeg";
                }
                else if (Application.platform == RuntimePlatform.OSXEditor
                    || Application.platform == RuntimePlatform.OSXPlayer
                ){
                    //process.StartInfo.FileName = "/bin/bash";
                    process.StartInfo.FileName = "/Applications/Utilities/Terminal.app/Contents/MacOS/Terminal";
                    programFile = "Mac/ffmpeg";
                }
                else { return false; }
                arguments =
                    "ffmpeg" +
                    //$"\"{Application.dataPath}/Programs/{programFile}\"" +
                    $" -i \"{input}\" \"{output}\"";
                //#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                //#endif
                arguments = arguments.Replace('\\', '/');
                process.Start();
                process.StandardInput.WriteLine(arguments);
                process.StandardInput.Close();
                //jString = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                process.Close();
                return true;
            }
        } catch (Exception e){
            Debug.Log("no ffmpeg?");
            return false;
        }
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
            if (detector.Confidence < 0.6f){
                //return Encoding.Default;
                return Encoding.GetEncoding("shift_jis");
            }else if (detector.Confidence >= 0.98f){
                return Encoding.GetEncoding(detector.Charset);
            }else {
                return Encoding.GetEncoding("gb18030");
            }
        }
        else { return Encoding.GetEncoding("shift_jis"); }
    }
}
