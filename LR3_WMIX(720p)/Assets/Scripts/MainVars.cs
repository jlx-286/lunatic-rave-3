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
//using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using Debug = UnityEngine.Debug;
using FFmpegEngine = FFmpeg.NET.Engine;

public class MainVars : MonoBehaviour {
    public static string Bms_root_dir { internal set; get; }
    public static string bms_file_path;
    //public static string currPath;
    public static string cur_scene_name;
    public static sbyte freq, pitch;
    public static byte master_vol, bgm_vol, key_vol;
    //public static byte reverb, reverb_level;
    //public static byte hipass, hipass_level;
    //public static byte lowpass, lowpass_level;
    //public static byte delay, delay_level;
    //public static byte flanger, flanger_level;
    //public static byte chorus, chorus_level;
    private Dictionary<string,byte> dict;
    public static Dictionary<string,Dictionary<string,byte>> FXs;
    //public Button play_btn;
    public AudioMixer mixer;
    public static FFmpegEngine ffmpegEngine;
    public static string ffmpegPath = string.Empty;
    public static BMSReader BMSReader;
    public static AudioChorusFilter chorusFilter;
    public static AudioDistortionFilter distortionFilter;
    public static AudioEchoFilter echoFilter;
    public static AudioHighPassFilter highPassFilter;
    public static AudioLowPassFilter lowPassFilter;
    public static AudioReverbFilter reverbFilter;
    // Use this for initialization
    private void Start () {
        cur_scene_name = "Start";
        BMSReader = null;
        bms_file_path = string.Empty;
        MainVars.ffmpegEngine = null;
        freq = pitch = 0;
        master_vol = bgm_vol = key_vol = 100;
        mixer.SetFloat("freq", Mathf.Pow(2f, freq / 12f));
        mixer.SetFloat("pitch", Mathf.Pow(2f, pitch / 12f));
        chorusFilter = this.gameObject.GetComponent<AudioChorusFilter>();
        distortionFilter = this.gameObject.GetComponent<AudioDistortionFilter>();
        echoFilter = this.gameObject.GetComponent<AudioEchoFilter>();
        highPassFilter = this.gameObject.GetComponent<AudioHighPassFilter>();
        lowPassFilter = this.gameObject.GetComponent<AudioLowPassFilter>();
        reverbFilter = this.gameObject.GetComponent<AudioReverbFilter>();
        dict = new Dictionary<string, byte>{
            { "enabled", 0 },
            { "value", 0 },
            { "level", 0 }
        };
        FXs = new Dictionary<string, Dictionary<string, byte>>{
            { "reverb", dict },
            { "hipass", dict },
            { "lowpass", dict },
            { "chorus", dict },
            { "flanger", dict },
            { "distortion", dict },
            { "delay", dict }
        };
    }
	
	// Update is called once per frame
	private void Update () {}
    
}