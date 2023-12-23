using System;
using System.Linq;
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
    [Range(-3000, 3000)] public short latency;
    public static long Latency;
    public static readonly Color32[] levelColor32s = {
        new Color32(0xFF,0xFF,0xFF,0xFF),// unknown
        // Color.white,
        new Color32(0x3C,0xCC,0x4D,0xFF),// beginner
        new Color32(0x4A,0x70,0xFF,0xFF),// normal
        new Color32(0xFF,0xC5,0x29,0xFF),// hyper
        new Color32(0xF3,0x23,0x37,0xFF),// another
        new Color32(0xE9,0x4A,0xEB,0xFF),// insane
    };
    public Sprite[] bme_notes_tex;
    public Sprite[] bme_lns_start_tex;
    public Image[] bme_ln_center_forms;
    public Sprite[] bme_lns_end_tex;
    public Sprite[] bms_notes_tex;
    public Sprite[] bms_lns_start_tex;
    public Image[] bms_ln_center_forms;
    public Sprite[] bms_lns_end_tex;
    public Sprite[] pms_notes_tex;
    public Sprite[] pms_lns_start_tex;
    public Image[] pms_ln_center_forms;
    public Sprite[] pms_lns_end_tex;
    public static Texture2D MeterLine;
    public static Texture2D[] BMENotesTex;
    public static Texture2D[] BMELNsStartTex;
    public static Image[] BMELNCenterForms;
    public static Texture2D[] BMELNsEndTex;
    public static Texture2D[] BMSNotesTex;
    public static Texture2D[] BMSLNsStartTex;
    public static Image[] BMSLNCenterForms;
    public static Texture2D[] BMSLNsEndTex;
    public static Texture2D[] PMSNotesTex;
    public static Texture2D[] PMSLNsStartTex;
    public static Image[] PMSLNCenterForms;
    public static Texture2D[] PMSLNsEndTex;
    public static PlayMode playMode = PlayMode.AutoPlay | PlayMode.SingleSong | PlayMode.ExtraStage;
    public Sprite[] stage_sprites;
    public Sprite demo_play;
    public static Sprite[] StageSprites;
    public static Sprite DemoPlay;
    private void Start(){
        Latency = latency * TimeSpan.TicksPerMillisecond * 100;
        MeterLine = new Texture2D(1000, 2, TextureFormat.RGBA32,
            false){filterMode = FilterMode.Point};
        MeterLine.SetPixels32(Enumerable.Repeat(new Color32(255, 255, 255, 127), 2000).ToArray());
        MeterLine.Apply(false, true);
        BMENotesTex = new Texture2D[bme_notes_tex.Length];
        BMELNsStartTex = new Texture2D[bme_lns_start_tex.Length];
        BMELNsEndTex = new Texture2D[bme_lns_end_tex.Length];
        for(int i = 0; i < BMENotesTex.Length; i++){
            BMENotesTex[i] = bme_notes_tex[i].ToTexture2D();
            BMELNsStartTex[i] = bme_lns_start_tex[i].ToTexture2D();
            BMELNsEndTex[i] = bme_lns_end_tex[i].ToTexture2D();
        }
        BMSNotesTex = new Texture2D[bms_notes_tex.Length];
        BMSLNsStartTex = new Texture2D[bms_lns_start_tex.Length];
        BMSLNsEndTex = new Texture2D[bms_lns_end_tex.Length];
        for(int i = 0; i < BMSNotesTex.Length; i++){
            BMSNotesTex[i] = bms_notes_tex[i].ToTexture2D();
            BMSLNsStartTex[i] = bms_lns_start_tex[i].ToTexture2D();
            BMSLNsEndTex[i] = bms_lns_end_tex[i].ToTexture2D();
        }
        PMSNotesTex = new Texture2D[pms_notes_tex.Length];
        PMSLNsStartTex = new Texture2D[pms_lns_start_tex.Length];
        PMSLNsEndTex = new Texture2D[pms_lns_end_tex.Length];
        for(int i = 0; i < PMSNotesTex.Length; i++){
            PMSNotesTex[i] = pms_notes_tex[i].ToTexture2D();
            PMSLNsStartTex[i] = pms_lns_start_tex[i].ToTexture2D();
            PMSLNsEndTex[i] = pms_lns_end_tex[i].ToTexture2D();
        }
        BMELNCenterForms = bme_ln_center_forms;
        BMSLNCenterForms = bms_ln_center_forms;
        PMSLNCenterForms = pms_ln_center_forms;
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
    }
}