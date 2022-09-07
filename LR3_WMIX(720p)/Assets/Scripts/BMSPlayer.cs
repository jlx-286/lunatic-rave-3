//using RenderHeads.Media.AVProVideo;
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
    [HideInInspector] public ushort currClipNum;
    [HideInInspector] public double playing_time;
    public Slider[] sliders;
    [HideInInspector] public bool escaped;
    [HideInInspector] public int bgm_table_row;
    [HideInInspector] public int bga_table_row;
    [HideInInspector] public int row_key;
    // [HideInInspector] public double playing_time_frame;
    // Use this for initialization
    private void Start () {
        BMS_Reader = MainVars.BMSReader;
        //MainVars.BMSPlayer = this.GetComponent<BMSPlayer>();
        MainVars.cur_scene_name = BMS_Reader.playing_scene_name;
        no_key_notes = no_bgm_notes = no_bgi = false;
        title_text.text = MainVars.BMSReader.title.text;
        currClipNum = 0;
        escaped = false;
        for(int a = 0; a < sliders.Length; a++){
            sliders[a].value = float.Epsilon;
        }
        bgm_table_row = bga_table_row = row_key = 0;
        playing_time = double.Epsilon / 2;
        // playing_time_frame = double.Epsilon / 2;
    }
    private void Update() {
        if (escaped) { return; }
        if (Input.GetKeyUp(KeyCode.Escape) && !escaped){
            //StartCoroutine(BMS_Reader.NoteTableClear());
            BMS_Reader.NoteTableClear();
            escaped = true;
            VLCPlayer.VLCRelease();
            SceneManager.UnloadSceneAsync(MainVars.cur_scene_name);
            SceneManager.LoadScene("Select", LoadSceneMode.Additive);
            return;
        }
        if (!no_bgm_notes && !no_key_notes && !no_bgi){
            // playing_time_frame += Time.deltaTime;
            for (int a = 0; a < sliders.Length; a++){
                sliders[a].value = Convert.ToSingle(playing_time / BMS_Reader.total_time);
                // sliders[a].value = Convert.ToSingle(playing_time_frame / BMS_Reader.total_time);
            }
            //return;
        }
    }
    private void FixedUpdate(){
        if (escaped) { return; }
        if(!no_bgm_notes && !no_key_notes && !no_bgi){
            if (row_key >= BMS_Reader.note_dataTable.Rows.Count
                && bgm_table_row >= BMS_Reader.bgm_note_table.Rows.Count
                && bga_table_row >= BMS_Reader.bga_table.Rows.Count
            ){
                no_bgm_notes = no_key_notes = no_bgi = true;
                Debug.Log("last note");
            }
            playing_time += Time.fixedDeltaTime;
        }
    }

}
