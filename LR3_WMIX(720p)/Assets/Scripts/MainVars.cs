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
using System.Text.RegularExpressions;
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
    public static byte master_vol, bgm_vol, key_vol;
    public static sbyte freq, pitch;
    public static byte delay_d, decay_r;
    public static byte lowpass_c, lowpass_Q;
    public static byte hipass_c, hipass_Q;
    public static byte dist;
    public static byte chorus_del, chorus_r, chorus_dep;
    public static byte reverb_dt, reverb_level;
    public static sbyte eq_62, eq_160, eq_400, eq_1000, eq_2500, eq_6300, eq_16000;
    //public static byte flanger, flanger_level;
    //public Button play_btn;
    public AudioMixer mixer;
    public static BMSReader BMSReader;
    //public static BMSPlayer BMSPlayer;
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
        //BMSPlayer = null;
        bms_file_path = string.Empty;
        StaticClass.ffmpegEngine = null;
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
    }
	
	// Update is called once per frame
	private void Update () {}
    
}