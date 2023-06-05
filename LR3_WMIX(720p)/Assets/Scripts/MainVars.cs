using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
public class MainVars : MonoBehaviour{// Game Manager
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
    public static short GreenNumber = 573;
    public static readonly ushort[][] JudgeWindows = new ushort[][]{
        //{PG,GR,GD,BD,PR}
        new ushort[]{ 8,24, 40,200,1000},//v.hard
        new ushort[]{15,30, 60,200,1000},//hard
        new ushort[]{18,40,100,200,1000},//normal
        new ushort[]{21,60,120,200,1000},//easy
        new ushort[]{26,75,150,200,1000},//v.easy
    };
    /*public static decimal[,] Increases = {
        //{EP,Miss,BD,GD,GR,PG}
        {-1.6m,-4.8m,-3.2m, 0.6m, 1.2m, 1.2m},// assist
        {-1.6m,-4.8m,-3.2m, 0.6m, 1.2m, 1.2m},// easy
        {   -2,   -6,   -4, 0.5m,    1,    1},// normal
        {   -2,  -10,   -6,0.05m, 0.1m, 0.1m},// hard (30% halve)
        {   -4,  -20,  -12,0.05m, 0.1m, 0.1m},// exh
        {    0, -100, -100,   +0,   +0,   +0},// FC
        { -100, -100, -100, -100,   -0,   +0},// PA (gr>=-1)
        {   -2,   -3,   -2,0.05m, 0.1m, 0.1m},// normal-Grade (30% halve)
        {   -4,   -6,   -4,0.05m, 0.1m, 0.1m},// hard-Grade
        {   -6,  -10,   -6,0.05m, 0.1m, 0.1m},// exh-Grade
        {-1.6m,-2.4m,-1.6m,0.06m,0.12m,0.12m},// course (30% halve)
        {   -0,   -0,   -0,   +0,   -0,   -0},// G-A
    };*/
    public static readonly Color32[] levelColor32s = {
        new Color32(0xFF,0xFF,0xFF,0xFF),// unknown
        // Color.white,
        new Color32(0x3C,0xCC,0x4D,0xFF),// beginner
        new Color32(0x4A,0x70,0xFF,0xFF),// normal
        new Color32(0xFF,0xC5,0x29,0xFF),// hyper
        new Color32(0xF3,0x23,0x37,0xFF),// another
        new Color32(0xE9,0x4A,0xEB,0xFF),// insane
    };
    public Image meter_form;
    public Image[] bms_note_forms;
    public Image[] pms_note_forms;
    public Image[] bms_ln_start_forms;
    public Image[] bms_ln_center_forms;
    public Image[] bms_ln_end_forms;
    public Image[] pms_ln_start_forms;
    public Image[] pms_ln_center_forms;
    public Image[] pms_ln_end_forms;
    public static Image MeterForm;
    public static Image[] BMSNoteForms;
    public static Image[] PMSNoteForms;
    public static Image[] BMSLNStartForms;
    public static Image[] BMSLNCenterForms;
    public static Image[] BMSLNEndForms;
    public static Image[] pMSLNStartForms;
    public static Image[] pMSLNCenterForms;
    public static Image[] pMSLNEndForms;
    public static RandomNumberGenerator rng = null;
    public static PlayMode playMode = PlayMode.AutoPlay | PlayMode.SingleSong | PlayMode.ExtraStage;
    public Sprite[] stage_sprites;
    public Sprite demo_play;
    public static Sprite[] StageSprites;
    public static Sprite DemoPlay;
    private void Start(){
        MeterForm = meter_form;
        BMSNoteForms = bms_note_forms;
        PMSNoteForms = pms_note_forms;
        BMSLNStartForms = bms_ln_start_forms;
        BMSLNCenterForms = bms_ln_center_forms;
        BMSLNEndForms = bms_ln_end_forms;
        pMSLNStartForms = pms_ln_start_forms;
        pMSLNCenterForms = pms_ln_center_forms;
        pMSLNEndForms = pms_ln_end_forms;
        StageSprites = stage_sprites;
        DemoPlay = demo_play;
        mixer.SetFloat("freq", 1);
        mixer.SetFloat("pitch", 1);
        chorusFilter = this.gameObject.GetComponent<AudioChorusFilter>();
        distortionFilter = this.gameObject.GetComponent<AudioDistortionFilter>();
        echoFilter = this.gameObject.GetComponent<AudioEchoFilter>();
        highPassFilter = this.gameObject.GetComponent<AudioHighPassFilter>();
        lowPassFilter = this.gameObject.GetComponent<AudioLowPassFilter>();
        reverbFilter = this.gameObject.GetComponent<AudioReverbFilter>();
        // FluidManager.Init(Application.streamingAssetsPath + "/FluidR3_GM.sf2");
        // FluidManager.Init(Application.streamingAssetsPath + "/TimGM6mb.sf2", 1000d, 3d);
        // FluidManager.Init(Application.streamingAssetsPath + "/TimGM6mb.sf2", 2.8d);
        rng = RandomNumberGenerator.Create();
    }
}