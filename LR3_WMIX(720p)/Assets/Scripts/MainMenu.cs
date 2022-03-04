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
using FFmpegEngine = FFmpeg.NET.Engine;

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
            if (LoadConfig()){
                SceneManager.LoadScene("Select", LoadSceneMode.Additive);
            }
        });
	}
	
	// Update is called once per frame
	//void Update () {}

    private bool LoadConfig(){
        string configPath = Application.dataPath + "/config.json";
        if (!File.Exists(configPath)) {
            //play_btn.interactable = false;
            return false;
        }
        JObject jObject = JObject.Parse(File.ReadAllText(configPath));
        if (jObject == null) {
            //play_btn.interactable = false;
            return false;
        }
        if(jObject["BMS_root_dir"] == null){
            //play_btn.interactable = false;
            return false;
        }
        MainVars.Bms_root_dir = jObject["BMS_root_dir"].ToString();
        MainVars.Bms_root_dir = MainVars.Bms_root_dir.Replace('\\', '/');
        if (Regex.IsMatch(MainVars.Bms_root_dir, @"^file://", StaticClass.regexOption)){
            MainVars.Bms_root_dir = MainVars.Bms_root_dir.Substring(7);
        }
        MainVars.Bms_root_dir = MainVars.Bms_root_dir.TrimEnd('/') + '/';
        if (!Directory.Exists(MainVars.Bms_root_dir)){
            //play_btn.interactable = false;
            return false;
        }
        MainVars.bms_file_path = MainVars.Bms_root_dir;
        if(jObject["FFmpegPath"] == null) { return false; }
        string ffmpegPath = string.Empty;
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
        if (jObject["FFmpegPath"][platform] == null){ return false; }
        ffmpegPath = jObject["FFmpegPath"][platform].ToString();
        if (!File.Exists(ffmpegPath)){ return false; }
        try{
            StaticClass.ffmpegEngine = new FFmpegEngine(ffmpegPath);
        }
        catch (Exception e){
            Debug.LogWarning(e.Message);
            //throw;
        }
        return true;
    }
}
