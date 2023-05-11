using UnityEngine;
public class KeyLaser : MonoBehaviour{
    private KeyCode[] keyCodes;
    private bool[] pressed;
    public Canvas[] keys;
    // private void OnEnable(){}
    private void Start(){
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
        }else if(BMSInfo.scriptType == ScriptType.PMS){
            keyCodes = new KeyCode[]{
                KeyCode.C, KeyCode.F, KeyCode.V, KeyCode.G, KeyCode.B,
                KeyCode.H, KeyCode.N, KeyCode.J, KeyCode.M,
                // KeyCode.T, KeyCode.Y,
            };
        }
        pressed = new bool[keyCodes.Length];
    }
    private void FixedUpdate(){
        for(int i = 0; i < keyCodes.Length; i++){
            if(!pressed[i] && Input.GetKey(keyCodes[i])){
                // Debug.Log($"down:{keyCodes[i]}");
                pressed[i] = keys[i].enabled = true;
            }else if(pressed[i] && !Input.GetKey(keyCodes[i])){
                // Debug.Log($"up:{keyCodes[i]}");
                pressed[i] = keys[i].enabled = false;
            }
        }
    }
    // private void OnDestroy(){}
}
