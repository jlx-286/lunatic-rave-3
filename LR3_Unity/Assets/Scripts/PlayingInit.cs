using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayingInit : MonoBehaviour{
    public BMSPlayer BMS_Player;
    public BGMPlayer BGM_Player;
    public BPMPlayer BPM_Player;
    public BGAPlayer BGA_Player;
    public NoteViewer note_viewer;
    public NotePlayer note_player;
    public ManualNotePlayer manualNotePlayer;
    private Button play;
    private bool pressed = false;
    // public Button exit;
    // press "Start" + "Select" + any black key to exit
    // press "Start" + "Select" + any white key to start
    /*private KeyCode[] blackKeysSet;
    private bool[] pressedBlackKeys;
    private KeyCode[] whiteKeysSet;
    private bool[] pressedWhiteKeys;
    private KeyCode[] startKeys;
    private bool[] pressedStartKeys;
    private KeyCode[] selectKeys;
    private bool[] pressedSelectKeys;
    // "Start", "Select", "black", "white"
    private readonly bool[] pressed = Enumerable.Repeat(false, 4).ToArray();*/
    private void Play(){
        BMS_Player.enabled =
        BGM_Player.enabled =
        BPM_Player.enabled =
        BGA_Player.enabled =
        // note_player.enabled =
        note_viewer.enabled =
        true;
        if((MainVars.playMode & PlayMode.AutoPlay) != 0)
            note_player.enabled = true;
        else manualNotePlayer.enabled = true;
        // DestroyImmediate(play.gameObject, true);
        DestroyImmediate(this.gameObject, true);
    }
    private void Awake() {
        play = this.GetComponent<Button>();
    }
    private void Start(){
        play.onClick.AddListener(Play);
        // exit.onClick.AddListener(()=>{
        //     waiting = false;
        // });
        /*if(BMSInfo.scriptType == ScriptType.BMS){
            startKeys = new KeyCode[]{KeyCode.W, KeyCode.LeftBracket};
            selectKeys = new KeyCode[]{KeyCode.Q, KeyCode.RightBracket};
            blackKeysSet = new KeyCode[]{KeyCode.D, KeyCode.F, KeyCode.G,
                KeyCode.J, KeyCode.K, KeyCode.L,};
            whiteKeysSet = new KeyCode[]{
                KeyCode.X, KeyCode.C, KeyCode.V, KeyCode.B,
                KeyCode.N, KeyCode.M, KeyCode.Comma, KeyCode.Period,
            };
        }else if(BMSInfo.scriptType == ScriptType.PMS){
            startKeys = new KeyCode[]{KeyCode.T};
            selectKeys = new KeyCode[]{KeyCode.Y};
            blackKeysSet = new KeyCode[]{KeyCode.F,
                KeyCode.G, KeyCode.H, KeyCode.J,};
            whiteKeysSet = new KeyCode[]{KeyCode.C, KeyCode.V,
                KeyCode.B, KeyCode.N, KeyCode.M,};
        }*/
    }
    private void Update(){
        if(pressed) return;
        if(Input.GetKeyUp(KeyCode.Escape)){
            pressed = true;
            SceneManager.UnloadSceneAsync(MainVars.cur_scene_name);
            SceneManager.LoadScene("Select", LoadSceneMode.Additive);
            return;
        }else if(Input.GetKeyUp(KeyCode.Return) || Input.GetKeyUp(KeyCode.KeypadEnter)){
            pressed = true;
            Play();
            return;
        }
        /*Array.Clear(pressed, 0, pressed.Length);
        for(int i = 0; i < startKeys.Length; i++){
            if(Input.GetKey(startKeys[i])){
                pressed[0] = true;
                break;
            }
        }
        for(int i = 0; i < selectKeys.Length; i++){
            if(Input.GetKey(startKeys[i])){
                pressed[1] = true;
                break;
            }
        }*/
    }
}
