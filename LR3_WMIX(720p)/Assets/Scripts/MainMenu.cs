using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour {
    public Button play_btn;
    public Button exit_btn;
	[HideInInspector] public static AudioSource[] audioSources;
	public AudioSource audioSource;
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
        //play_btn.interactable = LoadConfig();
        play_btn.onClick.AddListener(() => {
            if (LoadConfig()){
                audioSources = new AudioSource[36 * 36];
                for(int i = 0; i < 36 * 36; i++){
                    audioSources[i] = Instantiate(audioSource, this.gameObject.transform);
                    audioSources[i].name = $"#WAV{i}";
                }
                SceneManager.LoadScene("Select", LoadSceneMode.Additive);
            }
        });
	}
	
	// Update is called once per frame
	//void Update () {}

    private bool LoadConfig(){
        string configPath = Application.dataPath + "/config.json";
        if (!File.Exists(configPath)){ return false; }
        JObject jObject = JObject.Parse(File.ReadAllText(configPath));
        if (jObject == null){ return false; }
        string platform = string.Empty;
        switch (Application.platform){
            case RuntimePlatform.WindowsEditor:
            case RuntimePlatform.WindowsPlayer:
            case RuntimePlatform.WSAPlayerARM:
            case RuntimePlatform.WSAPlayerX64:
            case RuntimePlatform.WSAPlayerX86:
                platform = "Windows";
                break;
            case RuntimePlatform.LinuxEditor:
            case RuntimePlatform.LinuxPlayer:
                platform = "Linux";
                break;
            case RuntimePlatform.OSXEditor:
            case RuntimePlatform.OSXPlayer:
                platform = "Mac";
                break;
            default:
                //return;
                platform = "Others";
                break;
        }
        if(jObject[platform] == null) { return false; }
        if(jObject[platform]["BMS_root_dir"] == null){ return false; }
        MainVars.Bms_root_dir = jObject[platform]["BMS_root_dir"].ToString();
        MainVars.Bms_root_dir = MainVars.Bms_root_dir.Replace('\\', '/');
        if (Regex.IsMatch(MainVars.Bms_root_dir, @"^file://", StaticClass.regexOption)){
            MainVars.Bms_root_dir = MainVars.Bms_root_dir.Substring(7);
        }
        MainVars.Bms_root_dir = MainVars.Bms_root_dir.TrimEnd('/') + '/';
        if (!Directory.Exists(MainVars.Bms_root_dir)){ return false; }
        MainVars.bms_file_path = MainVars.Bms_root_dir;
        return true;
    }
}
