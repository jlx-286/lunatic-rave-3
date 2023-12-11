using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class SongList : MonoBehaviour, IPointerClickHandler {
    public GameObject activeContent;
    public GameObject initialContent;
    public GameObject emptyContent;
    public Button buttonForm;
    public static bool isInCus;
    public static bool loaded;
    public Button bmsItemForm;
    private string bmsFilename;
    private static readonly Regex regex = new Regex(
        @"\.([Bb][Mm][SsEeLl]|[Pp][Mm][Ss])$",
        StaticClass.regexOption);
    private void Start(){
        loaded = false;
        if(string.CompareOrdinal(MainVars.cur_scene_name, "Start") == 0){
            MainVars.bms_file_path = MainVars.Bms_root_dir;
            bmsFilename = string.Empty;
            isInCus = false;
        }else{
            bmsFilename = Path.GetFileName(MainVars.bms_file_path);
            MainVars.bms_file_path = Path.GetDirectoryName(MainVars.bms_file_path).Replace('\\', '/') + '/';
            isInCus = true;
            BMSInfo.CleanUpTex();
            BMSInfo.CleanUp();
            FFmpegVideoPlayer.Release();
            for(int i = 0; i < 36 * 36; i++) MainMenu.audioSources[i].clip = null;
            AssetBundle.UnloadAllAssetBundles(true);
        }
        MainVars.cur_scene_name = "Select";
        Resources.UnloadUnusedAssets();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, false);
    }
    private void Update(){
        if(!isInCus && !loaded){
            for(int i = 0; i < activeContent.transform.childCount; i++)
                Destroy(activeContent.transform.GetChild(i).gameObject);
            Button[] initBtns = initialContent.gameObject.GetComponentsInChildren<Button>();
            for(int i = 0; i < initBtns.Length; i++){
                Button b = Instantiate(initBtns[i], activeContent.transform);
                if(b.gameObject.name.StartsWith("custom", StringComparison.Ordinal))
                    b.onClick.AddListener(() => {
                        isInCus = true;
                        loaded = false;
                    });
            }
            loaded = true;
        }else if(isInCus && !loaded){
            for(int i = 0; i < activeContent.transform.childCount; i++)
                Destroy(activeContent.transform.GetChild(i).gameObject);
            foreach(string s in Directory.GetDirectories(MainVars.bms_file_path)){
                Button b = Instantiate(buttonForm, activeContent.transform);
                b.GetComponentInChildren<Text>().text = Path.GetFileName(s);
                b.GetComponent<Image>().enabled = true;
                b.GetComponent<Button>().enabled = true;
                b.GetComponent<CustomFolderButton>().enabled = true;
                b.GetComponent<CustomFolderButton>().isFolder = true;
            }
            foreach(string s in Directory.GetFiles(MainVars.bms_file_path, "*.*", SearchOption.TopDirectoryOnly)){
                if(regex.IsMatch(s)){
                    Button b = Instantiate(bmsItemForm, activeContent.transform);
                    b.GetComponentInChildren<Text>().text = Path.GetFileName(s);
                    b.GetComponent<Image>().enabled = true;
                    b.GetComponent<Button>().enabled = true;
                    b.GetComponent<CustomFolderButton>().enabled = true;
                    b.GetComponent<CustomFolderButton>().isFolder = false;
                }
            }
            loaded = true;
        }
    }
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    private const byte RootDirLen = 3;
#else
    private const byte RootDirLen = 1;
#endif
    public virtual void OnPointerClick(PointerEventData pointerEventData){
        if(pointerEventData.button == PointerEventData.InputButton.Right && isInCus){
            MainVars.bms_file_path = MainVars.bms_file_path.Replace('\\', '/');
            if(Path.GetPathRoot(MainVars.bms_file_path).Length >= RootDirLen){
                MainVars.bms_file_path = Path.GetDirectoryName(MainVars.bms_file_path.TrimEnd('/')).Replace('\\', '/') + '/';
                if(MainVars.bms_file_path.Length >= MainVars.Bms_root_dir.Length){
                    isInCus = true;
                    loaded = false;
                }else if(MainVars.bms_file_path.Length < MainVars.Bms_root_dir.Length){
                    MainVars.bms_file_path = MainVars.Bms_root_dir;
                    isInCus = false;
                    loaded = false;
                }
            }
            else if(Path.GetPathRoot(MainVars.bms_file_path).Length < 2 && isInCus){
                isInCus = false;
                loaded = false;
            }
        }
    }
}
