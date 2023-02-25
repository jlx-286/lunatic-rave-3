using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
public class MainVars : MonoBehaviour {
    public static string Bms_root_dir;
    public static string bms_file_path = string.Empty;
    //public static string currPath;
    public static string cur_scene_name = "Start";
    public static byte master_vol = 100, bgm_vol = 100, key_vol = 100;
    public static sbyte freq = 0, pitch = 0;
    public static decimal speed = 1;
    public static byte delay_d, decay_r;
    public static byte lowpass_c, lowpass_Q;
    public static byte hipass_c, hipass_Q;
    public static byte dist;
    public static byte chorus_del, chorus_r, chorus_dep;
    public static byte reverb_dt, reverb_level;
    public static sbyte eq_62, eq_160, eq_400, eq_1000, eq_2500, eq_6300, eq_16000;
    public AudioMixer mixer;
    public static AudioChorusFilter chorusFilter;
    public static AudioDistortionFilter distortionFilter;
    public static AudioEchoFilter echoFilter;
    public static AudioHighPassFilter highPassFilter;
    public static AudioLowPassFilter lowPassFilter;
    public static AudioReverbFilter reverbFilter;
    private void Start(){
        mixer.SetFloat("freq", Mathf.Pow(2f, freq / 12f));
        mixer.SetFloat("pitch", Mathf.Pow(2f, pitch / 12f));
        chorusFilter = this.gameObject.GetComponent<AudioChorusFilter>();
        distortionFilter = this.gameObject.GetComponent<AudioDistortionFilter>();
        echoFilter = this.gameObject.GetComponent<AudioEchoFilter>();
        highPassFilter = this.gameObject.GetComponent<AudioHighPassFilter>();
        lowPassFilter = this.gameObject.GetComponent<AudioLowPassFilter>();
        reverbFilter = this.gameObject.GetComponent<AudioReverbFilter>();
        // Application.quitting += OnApplicationQuit;
        // FluidManager.Init(Application.streamingAssetsPath + "/FluidR3_GM.sf2");
        // FluidManager.Init(Application.streamingAssetsPath + "/TimGM6mb.sf2", 1000d, 3d);
        FluidManager.Init(Application.streamingAssetsPath + "/TimGM6mb.sf2", 2.8d);
    }
	/*private void Update(){
        if(Time.realtimeSinceStartup >= StaticClass.OverFlowTime
            || Time.unscaledTime >= StaticClass.OverFlowTime
            || Time.fixedUnscaledTime >= StaticClass.OverFlowTime
            || Time.time >= StaticClass.OverFlowTime
            || Time.fixedTime >= StaticClass.OverFlowTime
        ){
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#elif UNITY_STANDALONE
            Application.Quit();
#endif
        }
    }*/
    private void OnApplicationQuit(){
        FluidManager.CleanUp();
        VLCPlayer.VLCRelease();
        BMSInfo.CleanUp();
        Resources.UnloadUnusedAssets();
        AssetBundle.UnloadAllAssetBundles(true);
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, false);
    }
}