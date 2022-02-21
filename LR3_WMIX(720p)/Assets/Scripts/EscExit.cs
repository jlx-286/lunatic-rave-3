using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class EscExit : MonoBehaviour {

	// Use this for initialization
	//void Start () {}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyUp(KeyCode.Escape)){
            //sqliteConnection.Close();
            //sqliteConnection = null;
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
