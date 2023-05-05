using System;
using System.Collections.Generic;
// using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class BMSPlayer : MonoBehaviour {
    public Text title_text;
    [HideInInspector] public bool no_key_notes { get; internal set; } = false;
    [HideInInspector] public bool no_bgm_notes { get; internal set; } = false;
    [HideInInspector] public bool no_bgi { get; internal set; } = false;
    [HideInInspector] public bool no_bpm_notes { get; internal set; } = false;
    [HideInInspector] public bool no_stop_notes { get; internal set; } = false;
    [HideInInspector] public ulong playingTimeAsNanoseconds { get; internal set; } = 0;
    [HideInInspector] public ulong fixedDeltaTimeAsNanoseconds { get; internal set; }
    public Slider[] sliders;
    public Sprite[] diffs;
    public Image diff;
    public Text level;
    private const ulong ns_per_sec = TimeSpan.TicksPerSecond * 100;
    private uint timeLeft = (uint)(BMSInfo.totalTimeAsNanoseconds / ns_per_sec) +
        (uint)(BMSInfo.totalTimeAsNanoseconds % ns_per_sec == 0 ? 0 : 1);
    public Text timeLeftText;
    // private Timer timer;
    // private bool toUpdate = false;
    [HideInInspector] public bool escaped { get; internal set; } = false;
    [HideInInspector] public int bgm_table_row = 0;
    [HideInInspector] public int bga_table_row = 0;
    [HideInInspector] public int row_key = 0;
    [HideInInspector] public int bpm_table_row = 0;
    [HideInInspector] public int stop_table_row = 0;
    private void Start(){
        MainVars.cur_scene_name = BMSInfo.playing_scene_name;
        title_text.text = BMSInfo.title;
        for(byte a = 0; a < sliders.Length; a++){
            sliders[a].maxValue = BMSInfo.totalTimeAsNanoseconds;
            sliders[a].value = float.Epsilon / 2;
        }
        diff.sprite = diffs[(byte)BMSInfo.difficulty];
        if(BMSInfo.difficulty == Difficulty.Unknown){
            // level.text = string.Empty;
            level.enabled = false;
        }else{
            level.text = BMSInfo.play_level.ToString();
            level.color = MainVars.levelColor32s[(byte)BMSInfo.difficulty];
        }
        fixedDeltaTimeAsNanoseconds = (ulong)(Time.fixedDeltaTime * ns_per_sec);
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
        if(!no_bgm_notes && !no_key_notes && !no_bgi && !no_bpm_notes && !no_stop_notes){
            // if(toUpdate){
            //     timeLeftText.text = timeLeft.ToString();
            //     toUpdate = false;
            // }
            for(byte a = 0; a < sliders.Length; a++)
                sliders[a].value = playingTimeAsNanoseconds;
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
        if(!no_bgm_notes && !no_key_notes && !no_bgi && !no_bpm_notes && !no_stop_notes){
            if(row_key >= BMSInfo.note_list_table.Count
                && bgm_table_row >= BMSInfo.bgm_list_table.Count
                && bga_table_row >= BMSInfo.bga_list_table.Count
                && bpm_table_row >= BMSInfo.bpm_list_table.Count
                // && stop_table_row >= BMSInfo.stop_list_table.Count
            ){
                no_bgm_notes = no_key_notes = no_bgi = no_bpm_notes = no_stop_notes = true;
                Debug.Log("last note");
            }
            playingTimeAsNanoseconds += fixedDeltaTimeAsNanoseconds;
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
