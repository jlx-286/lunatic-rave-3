using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class BMSPlayer : MonoBehaviour {
    public Text title_text;
    [HideInInspector] public long playingTimeAsNanoseconds { get; private set; } = 0;
    [HideInInspector] public long fixedDeltaTimeAsNanoseconds { get; private set; }
    public Slider[] sliders;
    public Sprite[] diffs;
    public Image diff;
    public Text level;
    private const long ns_per_sec = TimeSpan.TicksPerSecond * 100;
    private uint timeLeft = (uint)(BMSInfo.totalTimeAsNanoseconds / ns_per_sec) +
        (uint)(BMSInfo.totalTimeAsNanoseconds % ns_per_sec == 0 ? 0 : 1);
    public Text timeLeftText;
    public Image stage;
    [HideInInspector] public bool escaped { get; private set; } = false;
    // [HideInInspector] public int stop_table_row = 0;
    private void Awake(){
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
        fixedDeltaTimeAsNanoseconds = (long)(Time.fixedDeltaTime * ns_per_sec);
        timeLeftText.text = timeLeft.ToString();
        if((MainVars.playMode & PlayMode.AutoPlay) == PlayMode.AutoPlay)
            stage.sprite = MainVars.DemoPlay;
        else if((MainVars.playMode & PlayMode.SingleSong) == PlayMode.SingleSong)
            stage.sprite = MainVars.StageSprites[(byte)PlayMode.ExtraStage];
        else
            stage.sprite = MainVars.StageSprites[(byte)MainVars.playMode & 0xf];
    }
    private void Start(){
        StartCoroutine(SetTimeLeft());
        // StartCoroutine(Clean());
    }
    private void Update(){
        if(escaped) return;
        if(Input.GetKeyUp(KeyCode.Escape) && !escaped){
            escaped = true;
            SceneManager.UnloadSceneAsync(MainVars.cur_scene_name);
            SceneManager.LoadScene("Select", LoadSceneMode.Additive);
            return;
        }
        if(playingTimeAsNanoseconds <= BMSInfo.totalTimeAsNanoseconds){
            for(byte a = 0; a < sliders.Length; a++)
                sliders[a].value = playingTimeAsNanoseconds;
            //return;
        }
    }
    private void FixedUpdate(){
        if(escaped) return;
        if(playingTimeAsNanoseconds <= BMSInfo.totalTimeAsNanoseconds)
            playingTimeAsNanoseconds += fixedDeltaTimeAsNanoseconds;
        if(playingTimeAsNanoseconds > BMSInfo.totalTimeAsNanoseconds - fixedDeltaTimeAsNanoseconds && playingTimeAsNanoseconds <= BMSInfo.totalTimeAsNanoseconds)
            Debug.Log("last ms");
    }
    private void OnDestroy(){
        StopAllCoroutines();
    }
    private IEnumerator<WaitForFixedUpdate> SetTimeLeft(){
        while(timeLeft > 0){
            for(ushort i = 0; i < 1000u; i++)
                yield return StaticClass.waitForFixedUpdate;
            timeLeft--;
            timeLeftText.text = timeLeft.ToString();
        }
        Debug.Log("this coroutine stopped");
        yield break;
    }
    // private IEnumerator<YieldInstruction> Clean(){
    //     while(timeLeft > 0){
    //         for(uint i = 0; i < 5000u * 60u; i++)
    //             yield return StaticClass.waitForFixedUpdate;
    //         Resources.UnloadUnusedAssets();
    //         GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized, false, false);
    //     }
    //     yield break;
    // }
}
