using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class BMSPlayer : MonoBehaviour {
    public Text title_text;
    [HideInInspector] public bool no_key_notes;
    [HideInInspector] public bool no_bgm_notes;
    [HideInInspector] public bool no_bgi;
    //private sbyte channel;
    [HideInInspector] public uint playingTimeAsMilliseconds{ get; internal set; }
    [HideInInspector] public uint fixedDeltaTimeAsMilliseconds{ get; internal set; }
    public Slider[] sliders;
    [HideInInspector] public bool escaped;
    [HideInInspector] public int bgm_table_row;
    [HideInInspector] public int bga_table_row;
    [HideInInspector] public int row_key;
    private void Start(){
        MainVars.cur_scene_name = BMSInfo.playing_scene_name;
        no_key_notes = no_bgm_notes = no_bgi = false;
        title_text.text = BMSInfo.title;
        escaped = false;
        for(int a = 0; a < sliders.Length; a++)
            sliders[a].value = float.Epsilon / 2;
        bgm_table_row = bga_table_row = row_key = 0;
        playingTimeAsMilliseconds = 0;
        fixedDeltaTimeAsMilliseconds = (uint)(Time.fixedDeltaTime * 1000);
    }
    private void Update(){
        if(escaped) return;
        if(Input.GetKeyUp(KeyCode.Escape) && !escaped){
            escaped = true;
            SceneManager.UnloadSceneAsync(MainVars.cur_scene_name);
            SceneManager.LoadScene("Select", LoadSceneMode.Additive);
            return;
        }
        if(!no_bgm_notes && !no_key_notes && !no_bgi){
            for(int a = 0; a < sliders.Length; a++)
                sliders[a].value = Convert.ToSingle(playingTimeAsMilliseconds) / BMSInfo.totalTimeAsMilliseconds;
            //return;
        }
    }
    private void FixedUpdate(){
        if(escaped) return;
        if(!no_bgm_notes && !no_key_notes && !no_bgi){
            if(row_key >= BMSInfo.note_num_arr.Length
                && bgm_table_row >= BMSInfo.bgm_num_arr.Length
                && bga_table_row >= BMSInfo.bga_num_arr.Length
            ){
                no_bgm_notes = no_key_notes = no_bgi = true;
                Debug.Log("last note");
            }
            playingTimeAsMilliseconds += fixedDeltaTimeAsMilliseconds;
        }
    }

}
