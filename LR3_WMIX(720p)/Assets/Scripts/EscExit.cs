using UnityEditor;
using UnityEngine;
public class EscExit : MonoBehaviour {
	//void Start () {}
	void Update () {
        if (Input.GetKeyUp(KeyCode.Escape)){
            switch (Application.platform){
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.LinuxEditor:
                case RuntimePlatform.OSXEditor:
                    EditorApplication.isPlaying = false;
                    break;
                default:
                    Application.Quit();
                    break;
            }
        }
    }
}
