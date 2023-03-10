using System;
using System.Collections.Generic;
// using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class BMSPlayer : MonoBehaviour {
    public Text title_text;
    [HideInInspector] public bool no_key_notes;
    [HideInInspector] public bool no_bgm_notes;
    [HideInInspector] public bool no_bgi;
    [HideInInspector] public bool no_bpm_notes;
    [HideInInspector] public uint playingTimeAsMilliseconds{ get; internal set; }
    [HideInInspector] public uint fixedDeltaTimeAsMilliseconds{ get; internal set; }
    public Slider[] sliders;
    private uint timeLeft;
    public Text timeLeftText;
    // private Timer timer;
    // private bool toUpdate = false;
    [HideInInspector] public bool escaped;
    [HideInInspector] public int bgm_table_row;
    [HideInInspector] public int bga_table_row;
    [HideInInspector] public int row_key;
    [HideInInspector] public int bpm_table_row;
    private void Start(){
        MainVars.cur_scene_name = BMSInfo.playing_scene_name;
        no_key_notes = no_bgm_notes = no_bgi = no_bpm_notes = false;
        title_text.text = BMSInfo.title;
        escaped = false;
        for(byte a = 0; a < sliders.Length; a++){
            sliders[a].maxValue = BMSInfo.totalTimeAsMilliseconds;
            sliders[a].value = float.Epsilon / 2;
        }
        bgm_table_row = bga_table_row = row_key = bpm_table_row = 0;
        playingTimeAsMilliseconds = 0;
        fixedDeltaTimeAsMilliseconds = (uint)(Time.fixedDeltaTime * 1000);
        timeLeft = BMSInfo.totalTimeAsMilliseconds / 1000 +
            (uint)(BMSInfo.totalTimeAsMilliseconds % 1000 == 0 ? 0 : 1);
        timeLeftText.text = timeLeft.ToString();
        // timer = new Timer(obj => {
        //     if(timeLeft == 0){
        //         timer.Dispose();
        //         timer = null;
        //         return;
        //     }
        //     timeLeft--;
        //     // Debug.Log(timeLeft);
        //     toUpdate = true;
        // }, null, 0, 1000);
        StartCoroutine(SetTimeLeft());
    }
    private void Update(){
        if(escaped) return;
        if(Input.GetKeyUp(KeyCode.Escape) && !escaped){
            escaped = true;
            SceneManager.UnloadSceneAsync(MainVars.cur_scene_name);
            SceneManager.LoadScene("Select", LoadSceneMode.Additive);
            return;
        }
        if(!no_bgm_notes && !no_key_notes && !no_bgi && !no_bpm_notes){
            // if(toUpdate){
            //     timeLeftText.text = timeLeft.ToString();
            //     toUpdate = false;
            // }
            for(byte a = 0; a < sliders.Length; a++)
                sliders[a].value = playingTimeAsMilliseconds;
            //return;
        }
    }
    private void FixedUpdate(){
        if(escaped) return;
        // if(playingTimeAsMilliseconds < BMSInfo.totalTimeAsMilliseconds){
        //     playingTimeAsMilliseconds += fixedDeltaTimeAsMilliseconds;
        //     if(playingTimeAsMilliseconds == BMSInfo.totalTimeAsMilliseconds){
        //         no_bgm_notes = no_key_notes = no_bgi = no_bpm_notes = true;
        //         Debug.Log("last note");
        //     }
        // }
        if(!no_bgm_notes && !no_key_notes && !no_bgi && !no_bpm_notes){
            if(row_key >= BMSInfo.note_list_table.Count
                && bgm_table_row >= BMSInfo.bgm_list_table.Count
                && bga_table_row >= BMSInfo.bga_list_table.Count
                && bpm_table_row >= BMSInfo.bpm_list_table.Count
            ){
                no_bgm_notes = no_key_notes = no_bgi = no_bpm_notes = true;
                Debug.Log("last note");
            }
            playingTimeAsMilliseconds += fixedDeltaTimeAsMilliseconds;
        }
    }
    private void OnDestroy(){
        StopAllCoroutines();
        // timer.Dispose();
        // timer = null;
    }
    private IEnumerator<WaitForSeconds> SetTimeLeft(){
        while(timeLeft > 0){
            yield return new WaitForSeconds(1);
            timeLeft--;
            timeLeftText.text = timeLeft.ToString();
        }
        // StopCoroutine(SetTimeLeft1());
        Debug.Log("this coroutine stopped");
        yield break;
    }
}
