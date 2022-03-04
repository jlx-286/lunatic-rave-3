using RenderHeads.Media.AVProVideo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class BMSPlayer : MonoBehaviour {
    private BMSReader BMS_Reader;
    public Text title_text;
    [HideInInspector] public bool no_key_notes;
    [HideInInspector] public bool no_bgm_notes;
    [HideInInspector] public bool no_bgi;
    //private sbyte channel;
    public AudioSource audioSourceForm;
    [HideInInspector] public AudioSource currSrc;
    [HideInInspector] public ushort currClipNum;
    [HideInInspector] public double playing_time;
    public Slider[] sliders;
    //private double playing_bga_time;
    [HideInInspector] public bool escaped;
    [HideInInspector] public Dictionary<ushort, AudioSource> totalSrcs;
    [HideInInspector] public int bgm_table_row;
    // Use this for initialization
    private void Start () {
        BMS_Reader = MainVars.BMSReader;
        //MainVars.BMSPlayer = this.GetComponent<BMSPlayer>();
        MainVars.cur_scene_name = BMS_Reader.playing_scene_name;
        no_key_notes = no_bgm_notes = no_bgi = false;
        title_text.text = MainVars.BMSReader.title.text;
        //playing_bga_time = double.Epsilon / 2;
        totalSrcs = new Dictionary<ushort, AudioSource>();
        currSrc = null;
        currClipNum = 0;
        escaped = false;
        for(int a = 0; a < sliders.Length; a++){
            sliders[a].value = float.Epsilon;
        }
        playing_time = 0.000d;
    }

    private void FixedUpdate(){
        if (escaped) { return; }
        if (Input.GetKeyUp(KeyCode.Escape) && !escaped){
            //StartCoroutine(BMS_Reader.NoteTableClear());
            BMS_Reader.NoteTableClear();
            if (totalSrcs != null){
                totalSrcs.Clear();
                totalSrcs = null;
            }
            escaped = true;
            SceneManager.UnloadSceneAsync(MainVars.cur_scene_name);
            SceneManager.LoadScene("Select", LoadSceneMode.Additive);
            return;
        }
        if (!no_bgm_notes && !no_key_notes && !no_bgi){
            if (BMS_Reader.row_key >= BMS_Reader.note_dataTable.Rows.Count
                && bgm_table_row >= BMS_Reader.bgm_note_table.Rows.Count
                && BMS_Reader.bga_table_row >= BMS_Reader.bga_table.Rows.Count
            ){
                no_bgm_notes = no_key_notes = no_bgi = true;
                Debug.Log("last note");
            }
            playing_time += Time.fixedDeltaTime;
            for (int a = 0; a < sliders.Length; a++){
                sliders[a].value = Convert.ToSingle(playing_time / BMS_Reader.total_time);
            }
            //return;
        }
    }

}
