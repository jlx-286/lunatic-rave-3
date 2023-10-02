using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class MainMenu : MonoBehaviour {
    public Button play_btn;
    public Button exit_btn;
    public readonly static AudioSource[] audioSources = Enumerable.Repeat<AudioSource>(null, 36 * 36 + 1).ToArray();
    public AudioSource audioSource;
    private void Start(){
#if UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX || PLATFORM_STANDALONE_LINUX
        FFmpegPlugins.MatchFFmpegVersion();
        FFmpegVideoPlayer.MatchFFmpegVersion();
#else
        FFmpegVideoPlayer.Init();
#endif
        exit_btn.onClick.AddListener(() => {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#elif UNITY_STANDALONE
            Application.Quit();
#endif
        });
        //play_btn.interactable = LoadConfig();
        play_btn.onClick.AddListener(() => {
            if(LoadConfig()){
                for(ushort i = 0; i < audioSources.Length; i++){
                    audioSources[i] = Instantiate(audioSource, this.gameObject.transform);
                    audioSources[i].name = $"#WAV{i}";
                }
                SceneManager.LoadScene("Select", LoadSceneMode.Additive);
            }
        });
    }
    /*private void Update(){
        if(Time.realtimeSinceStartup >= StaticClass.OverFlowTime
            || Time.unscaledTime >= StaticClass.OverFlowTime
            || Time.fixedUnscaledTime >= StaticClass.OverFlowTime
            || Time.time >= StaticClass.OverFlowTime
            || Time.fixedTime >= StaticClass.OverFlowTime
        ){
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#elif UNITY_STANDALONE
            Application.Quit();
#endif
        }
    }*/
    private bool LoadConfig(){
        string configPath = Application.dataPath + "/config.json";
        if(!File.Exists(configPath)) return false;
        JObject jObject = JObject.Parse(File.ReadAllText(configPath));
        if(jObject == null) return false;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        const string platform = "Windows";
#elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
        const string platform = "Linux";
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        const string platform = "Mac";
#else
        const string platform = "Others";
#endif
        if(jObject[platform] == null) return false;
        if(jObject[platform]["BMS_root_dir"] == null) return false;
        MainVars.Bms_root_dir = jObject[platform]["BMS_root_dir"].ToString();
        MainVars.Bms_root_dir = MainVars.Bms_root_dir.Replace('\\', '/');
        if(Regex.IsMatch(MainVars.Bms_root_dir, @"^file://", StaticClass.regexOption))
            MainVars.Bms_root_dir = MainVars.Bms_root_dir.Substring(7);
        MainVars.Bms_root_dir = MainVars.Bms_root_dir.TrimEnd('/') + '/';
        if(!Directory.Exists(MainVars.Bms_root_dir)) return false;
        MainVars.bms_file_path = MainVars.Bms_root_dir;
        return true;
    }
    private void OnApplicationQuit(){
        MainVars.rng.Dispose();
        // FluidManager.CleanUp();
        FFmpegVideoPlayer.Release();
        FFmpegPlugins.CleanUp();
        BMSInfo.CleanUp();
        Resources.UnloadUnusedAssets();
        // AssetBundle.UnloadAllAssetBundles(true);
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, false);
        Debug.Log("quit");
    }
}
