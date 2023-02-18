using UnityEngine;
public class EscExit : MonoBehaviour {
    //private void Start(){}
    private void Update(){
        if(Input.GetKeyUp(KeyCode.Escape))
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_STANDALONE
            Application.Quit();
#endif
    }
}
