using System.Linq;
using UnityEngine;
public class KeyLaser : MonoBehaviour{
    [HideInInspector] public KeyCode[] keyCodes;
    [HideInInspector] public bool[] pressed;
    public Canvas[] keys;
    [HideInInspector] public byte[] keyLanes;
    public NotePlayer notePlayer;
    /*private int[] audio_key_nums;
    private ushort[] clipNums;
    private ulong[] judgeWindow;
    private int[] first;
    private bool[] passed;*/
    private void Awake(){
        if(BMSInfo.scriptType == ScriptType.BMS){
            keyCodes = new KeyCode[]{
                // KeyCode.Q, KeyCode.W,
                KeyCode.Z, KeyCode.S,
                KeyCode.X, KeyCode.D, KeyCode.C,
                KeyCode.F, KeyCode.V, KeyCode.G, KeyCode.B,
                KeyCode.N, KeyCode.J, KeyCode.M, KeyCode.K,
                KeyCode.Comma, KeyCode.L, KeyCode.Period,
                KeyCode.Semicolon, KeyCode.Slash,
                // KeyCode.LeftBracket, KeyCode.RightBracket,
            };
            if(BMSInfo.playerType == PlayerType.Keys5 || BMSInfo.playerType == PlayerType.Keys7){
                keyLanes = new byte[]{
                    0, 0,
                    1, 2, 3, 4, 5, 6, 7,
                    1, 2, 3, 4, 5, 6, 7,
                    0, 0,
                };
            }else{
                keyLanes = new byte[]{
                    0, 0,
                    1, 2, 3, 4, 5, 6, 7,
                    9, 10, 11, 12, 13, 14, 15,
                    8, 8,
                };
            }
        }else if(BMSInfo.scriptType == ScriptType.PMS){
            keyCodes = new KeyCode[]{
                KeyCode.C, KeyCode.F, KeyCode.V, KeyCode.G, KeyCode.B,
                KeyCode.H, KeyCode.N, KeyCode.J, KeyCode.M,
                // KeyCode.T, KeyCode.Y,
            };
            keyLanes = new byte[]{0, 1, 2, 3, 4, 5, 6, 7, 8};
        }
        pressed = new bool[keyCodes.Length];
        /*audio_key_nums = Enumerable.Repeat(0, BMSInfo.note_list_lanes.Length).ToArray();
        clipNums = Enumerable.Repeat<ushort>(36*36, BMSInfo.note_list_lanes.Length).ToArray();
        // first = Enumerable.Repeat(-1, BMSInfo.note_list_lanes.Length).ToArray();
        // passed = Enumerable.Repeat(true, BMSInfo.note_list_lanes.Length).ToArray();
        for(int i = 0; i < BMSInfo.note_list_lanes.Length; i++)
            if(BMSInfo.note_list_lanes[i].Count > 0)
                clipNums[i] = BMSInfo.note_list_lanes[i][0].clipNum;
        judgeWindow = new ulong[MainVars.JudgeWindows[BMSInfo.judge_rank].Length];
        for(int i = 0; i < judgeWindow.Length; i++)
            judgeWindow[i] = 1000000ul * MainVars.JudgeWindows[BMSInfo.judge_rank][i];*/
    }
    private void FixedUpdate(){
        /*for(int i = 0; i < BMSInfo.note_list_lanes.Length; i++){
            if(audio_key_nums[i] < BMSInfo.note_list_lanes[i].Count && 
                (BMSInfo.note_list_lanes[i][audio_key_nums[i]].time <= notePlayer.BMS_Player.playingTimeAsNanoseconds + judgeWindow[4]
                && BMSInfo.note_list_lanes[i][audio_key_nums[i]].time + judgeWindow[3] >= notePlayer.BMS_Player.playingTimeAsNanoseconds)
                && BMSInfo.note_list_lanes[i][audio_key_nums[i]].noteType != NoteType.LongnoteEnd
            ){
                clipNums[i] = BMSInfo.note_list_lanes[i][audio_key_nums[i]].clipNum;
                // MainMenu.audioSources[clipNums[i]].Play();
            }
            while(audio_key_nums[i] < BMSInfo.note_list_lanes[i].Count){
                if(BMSInfo.note_list_lanes[i][audio_key_nums[i]].time <= notePlayer.BMS_Player.playingTimeAsNanoseconds)
                    audio_key_nums[i]++;
                else break;
            }
        }*/
        for(int i = 0; i < keyCodes.Length; i++){
            if(!pressed[i] && Input.GetKey(keyCodes[i])){
                // MainMenu.audioSources[clipNums[keyLanes[i]]].Play();
                pressed[i] = keys[i].enabled = true;
            }else if(pressed[i] && !Input.GetKey(keyCodes[i])){
                // Debug.Log($"up:{keyCodes[i]}");
                pressed[i] = keys[i].enabled = false;
            }
        }
    }
    // private void OnDestroy(){}
}
