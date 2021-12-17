using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour {
    public Button play_btn;
    public Button exit_btn;
	// Use this for initialization
	void Start () {
        exit_btn.onClick.AddListener(() => {
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
        });
        play_btn.onClick.AddListener(() => {
            SceneManager.LoadScene("Select", LoadSceneMode.Additive);
        });
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
