using FFmpeg.NET;
using Microsoft.Scripting.Hosting;
using Newtonsoft.Json.Linq;
//using IronPython;
//using IronPython.Hosting;
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
//using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using Debug = UnityEngine.Debug;
//using Python = IronPython.Hosting.Python;

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
    public static JObject FXs;
    private JObject jObject;
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
        jObject = new JObject{
            { "enabled", false },
            { "value", (byte)0 },
            { "level", (byte)0 }
        };
        FXs = new JObject{
            { "reverb", jObject },
            { "hipass", jObject },
            { "lowpass", jObject },
            { "chorus", jObject },
            { "flanger", jObject },
            { "distortion", jObject },
            { "delay", jObject }
        };
        //mixerGroup = mixer.outputAudioMixerGroup;
        //SceneManager.LoadScene("Select", LoadSceneMode.Additive);
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
        string digits = "0123456789abcdefghijklmnopqrstuvwxyz";
        result.Append(digits[u / 36]).Append(digits[u % 36]);
        return result.ToString();
    }
    public static AudioClip GetAudioClipByFilePath(string path){
        string jString = string.Empty;
        Process process = new Process();
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
            process.StartInfo.Arguments = "cmd /c start /b \"\" "
                + "\"" + Path.GetFullPath("./Assets/Plugins/Windows/ffprobe.exe") + "\""
                + " -v quiet -print_format json -show_format -show_streams "
                + "\"" + path + "\"";
        }
        //#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        //#endif
        process.StartInfo.Arguments = process.StartInfo.Arguments.Replace('\\', '/');
        process.Start();
        try{
            jString = process.StandardOutput.ReadToEnd();
            //process.WaitForExit();
            process.Close();
            process.Dispose();
        }
        catch (Exception e){
            Debug.Log(e.Message);
            return null;
        }
        JObject jObject = JObject.Parse(jString);
        if (jObject == null){
            Debug.LogError("JObject null");
            return null;
        }
        if (jObject["streams"] == null){
            return null;
        }
        if (jObject["streams"][0] == null){
            return null;
        }
        if (jObject["streams"][0]["codec_name"] == null){
            return null;
        }
        string extension = string.Empty;
        extension = jObject["streams"][0]["codec_name"].ToString().ToLower();
        AudioClip audioClip = null;
        byte[] data;
        data = File.ReadAllBytes(path);
        if (extension.StartsWith("mp3")){
            audioClip = WAV.Mp3ToClip(data, jObject);
        }else if (extension.StartsWith("pcm")){
            audioClip = WAV.WavToClip(data, jObject);
        }else if (extension.StartsWith("vorbis")){
            audioClip = WAV.OggToClip(data);
        }
        return audioClip;
    }

    public static VideoClip GetVideoCLipByFilePath(string path){
        string jString = string.Empty;
        Process process = new Process();
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
            process.StartInfo.Arguments = "cmd /c start /b \"\" "
                + "\"" + Path.GetFullPath("./Assets/Plugins/Windows/ffprobe.exe") + "\""
                + " -v quiet -print_format json -show_format -show_streams "
                + "\"" + path + "\""; 
        }
        //#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        //#endif
        process.StartInfo.Arguments = process.StartInfo.Arguments.Replace('\\', '/');
        process.Start();
        jString = process.StandardOutput.ReadToEnd();
        //process.WaitForExit();
        process.Close();
        process.Dispose();
        JObject jObject = JObject.Parse(jString);
        if (jObject == null){
            return null;
        }
        if (jObject["streams"] == null){
            return null;
        }
        //List<JObject> streams = new List<JObject>();
        //streams.AddRange(jObject["streams"]);
        JArray streams = new JArray(jObject["streams"]);
        for(int i = 0; i < streams.Count; i++){
            if (streams[i]["codec_type"].ToString().ToLower() == "video"){
                jObject.RemoveAll();
                jObject = new JObject(streams[i]);
                break;
            }
        }
        return null;
    }
    public static string GetEncodingByFilePath(string bms_path){
        string encoding = string.Empty;
        Process process = new Process();
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
            process.StartInfo.Arguments = "cmd /c start /b \"\" "
                + "python "
                + "\"" + Path.GetFullPath("./Assets/Scripts/GetEncoding.py") + "\""
                + "\"" + bms_path + "\"";
        }
        else if (Application.platform == RuntimePlatform.LinuxEditor
           || Application.platform == RuntimePlatform.LinuxPlayer
           || Application.platform == RuntimePlatform.OSXEditor
           || Application.platform == RuntimePlatform.OSXPlayer
        ){
            process.StartInfo.Arguments = "python3 "
                + "\"" + Path.GetFullPath("./Assets/Scripts/GetEncoding.py") + "\""
                + "\"" + bms_path + "\""; ;
        }
        //#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        //#endif
        process.StartInfo.Arguments = process.StartInfo.Arguments.Replace('\\', '/');
        process.Start();
        encoding = process.StandardOutput.ReadToEnd().Split()[0];
        process.WaitForExit();
        process.Close();
        process.Dispose();
        //ScriptEngine scriptEngine = Python.CreateEngine();
        //dynamic py = scriptEngine.ExecuteFile("GetEncoding.py");
        //encoding = py;
        //Debug.Log(encoding);
        if (string.IsNullOrEmpty(encoding)){
            encoding = "shift_jis";
        }
        return encoding;
    }
}
