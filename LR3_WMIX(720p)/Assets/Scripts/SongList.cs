using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SongList : MonoBehaviour ,IPointerClickHandler {
    public GameObject activeContent;
    public GameObject initialContent;
    public GameObject emptyContent;
    public Button buttonForm;
    [HideInInspector] public static bool isInCus;
    [HideInInspector] public static bool loaded;
    public Button bmsItemForm;
    private string bmsFilename;
    // Use this for initialization
    private void Start () {
        loaded = false;
        if (MainVars.cur_scene_name == "Start"){
            MainVars.bms_file_path = MainVars.Bms_root_dir;
            bmsFilename = string.Empty;
            isInCus = false;
        }else{
            bmsFilename = Path.GetFileName(MainVars.bms_file_path);
            MainVars.bms_file_path = Path.GetDirectoryName(MainVars.bms_file_path).Replace('\\', '/') + '/';
            isInCus = true;
        }
        MainVars.cur_scene_name = "Select";
        MainVars.BMSReader = null;
        //MainVars.BMSPlayer = null;
        Resources.UnloadUnusedAssets();
        GC.Collect();
    }

    // Update is called once per frame
    private void Update(){
        if (!isInCus && !loaded){
            for (int i = 0; i < activeContent.transform.childCount; i++){
                Destroy(activeContent.transform.GetChild(i).gameObject);
            }
            Button[] initBtns = initialContent.gameObject.GetComponentsInChildren<Button>();
            for(int i = 0; i < initBtns.Length; i++){
                Button b = Instantiate(initBtns[i], activeContent.transform);
                if (b.gameObject.name.StartsWith("custom")){
                    InitSongBtn(b);
                }
            }
            loaded = true;
        }else if(isInCus && !loaded){
            for (int i = 0; i < activeContent.transform.childCount; i++){
                Destroy(activeContent.transform.GetChild(i).gameObject);
            }
            foreach (string s in Directory.GetDirectories(MainVars.bms_file_path)){
                Button b = Instantiate(buttonForm, activeContent.transform);
                b.GetComponentInChildren<Text>().text = Path.GetFileName(s);
                b.GetComponent<Image>().enabled = true;
                b.GetComponent<Button>().enabled = true;
                b.GetComponent<CustomFolderButton>().enabled = true;
                b.GetComponent<CustomFolderButton>().isFolder = true;
            }
            foreach(string s in Directory.GetFiles(MainVars.bms_file_path, "*.*", SearchOption.TopDirectoryOnly)){
                if (Regex.IsMatch(s, @"\.(bms|bme|bml|pms)$", StaticClass.regexOption)){
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

    public virtual void OnPointerClick(PointerEventData pointerEventData){
        if (pointerEventData.button == PointerEventData.InputButton.Right && isInCus){
            MainVars.bms_file_path = MainVars.bms_file_path.Replace('\\', '/');
            #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            if (Path.GetPathRoot(MainVars.bms_file_path).Length > 2){
            #else
            if (Path.GetPathRoot(MainVars.bms_file_path).Length > 0){
            #endif
                MainVars.bms_file_path = Path.GetDirectoryName(MainVars.bms_file_path.TrimEnd('/')).Replace('\\', '/') + '/';
                if (MainVars.bms_file_path.Length >= MainVars.Bms_root_dir.Length){
                    isInCus = true;
                    loaded = false;
                }else if (MainVars.bms_file_path.Length < MainVars.Bms_root_dir.Length){
                    MainVars.bms_file_path = MainVars.Bms_root_dir;
                    isInCus = false;
                    loaded = false;
                }
            }
            else if(Path.GetPathRoot(MainVars.bms_file_path).Length < 2 && isInCus) {
                isInCus = false;
                loaded = false;
            }
        }
    }

    private void InitSongBtn(Button button){
        button.onClick.AddListener(() => {
            isInCus = true;
            loaded = false;
        });
    }
}
