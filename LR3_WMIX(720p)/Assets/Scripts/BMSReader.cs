using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
#if UNITY_5_3_OR_NEWER
using UnityEngine;
using UnityEngine.UI;
#elif GODOT
using Godot;
#endif
/// <summary>
/// also supports PMS files
/// </summary>
public partial class BMSReader
#if UNITY_5_3_OR_NEWER
: MonoBehaviour
#elif GODOT
: Node
#endif
{
#if UNITY_5_3_OR_NEWER
    public Sprite[] diffs;
    public Image[] playerDiffs;
    public TMPro.TMP_Text[] levels;
    public Slider slider;
    public Button play_btn;
    public Button back_btn;
    public Button auto_btn;
    public Button replay_btn;
    public Button practice_btn;
    public Text progress;
    public Text genre;
    public Text title;
    public Text sub_title;
    public Text artist;
    public RawImage stageFile;
    public Sprite[] keyTypes;
    public Image keyType;
    private void ShowKeytypePMS(){ keyType.sprite = keyTypes[4]; }
    private void ShowGenre(){ genre.text = BMSInfo.genre; }
    private void ShowTitle(){ title.text = BMSInfo.title; }
    private void ShowSubtitle(){ sub_title.text = BMSInfo.sub_title.Last(); }
    private void ShowArtist(){ artist.text = BMSInfo.artist; }
    // private void ShowStagefile(){}
    // private void ShowBackbmp(){}
    private void ShowDiff(){
        for(byte i = 0; i < playerDiffs.Length; i++){
            levels[i].enabled = playerDiffs[i].enabled = true;
            levels[i].text = BMSInfo.play_level.ToString();
            levels[i].outlineColor = MainVars.levelColor32s[(byte)BMSInfo.difficulty];
            playerDiffs[i].sprite = diffs[(byte)BMSInfo.difficulty];
        }
        slider.maxValue = total_medias_count;
        // slider.value = float.Epsilon / 2;
        progress.text = "Parsing";
    }
    private void ShowStartLoad(){
        progress.text = $"Loaded/Total:0/{total_medias_count}";
        // slider.value = float.Epsilon / 2;
    }
    // private void LoadBGI(){}
    private void ShowLoaded(){
        progress.text = $"Loaded/Total:{loaded_medias_count}/{total_medias_count}";
        slider.value = loaded_medias_count;
    }
    // private void LoadWAV(){}
    private void Parsing(){ progress.text = "Parsing"; }
    private void ShowKeytype(){ keyType.sprite = keyTypes[(byte)BMSInfo.playerType]; }
    private void Done(){
        Debug.Log(illegal);
        progress.text = "Done";
        // if(!illegal && !string.IsNullOrWhiteSpace(playing_scene_name)){
        if(!string.IsNullOrWhiteSpace(BMSInfo.playing_scene_name)){
            play_btn.interactable = auto_btn.interactable = true;
        }
        else Debug.LogWarning("Unknown player type");
        isDone = true;
    }
    private IEnumerator<byte> DequeueLoop(){
        while(!isDone){
            while(actions.TryDequeue(
                out Action action)) action();
            yield return byte.MinValue;
        }
        yield break;
    }
    private void Start(){
        thread = new Thread(ReadScript){ IsBackground = true };
        thread.Start();
        StartCoroutine(DequeueLoop());
    }
    private void OnDestroy(){
        inThread = false;
        if(thread != null){
            if(sorting){
                try{ thread.Abort(); }
                catch(Exception e){
                    Debug.LogWarning(e.GetBaseException());
                }
            }
            while(thread.IsAlive);
            thread = null;
        }
        StopAllCoroutines();
        CleanUp();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, false);
    }
#elif GODOT
    private void ShowKeytypePMS(){}
    private void ShowGenre(){}
    private void ShowTitle(){}
    private void ShowSubtitle(){}
    private void ShowArtist(){}
    // private void ShowStagefile(){}
    // private void ShowBackbmp(){}
    private void ShowDiff(){}
    private void ShowStartLoad(){}
    // private void LoadBGI(){}
    private void ShowLoaded(){}
    // private void LoadWAV(){}
    private void Parsing(){}
    private void ShowKeytype(){}
    private void Done(){
        GD.Print(illegal);
        // if(!illegal && !string.IsNullOrWhiteSpace(BMSInfo.playing_scene_name))
        if(!string.IsNullOrWhiteSpace(BMSInfo.playing_scene_name))
        {}
        else GD.PushWarning("Unknown player type");
        isDone = true;
    }
#endif
}
