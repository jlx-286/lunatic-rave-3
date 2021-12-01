using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SongList : MainVars,IPointerClickHandler {
    public GameObject activeContent;
    public GameObject initialContent;
    public GameObject emptyContent;
    public Button buttonForm;
    public static string songsPath;
    public static string currPath;
    public static bool isInCus;
    public static bool loaded;
    public Button bmsItemForm;
    // Use this for initialization
    private void Start () {
        loaded = false;
        songsPath = @"E:\Programs\BMS\";
        if (cur_scene_name == "Main"){
            currPath = songsPath;
            isInCus = false;
        }else{
            currPath = bms_file_path.TrimEnd('\\', '/').Substring(0, Math.Max(bms_file_path.LastIndexOf('\\'), bms_file_path.LastIndexOf('/')) + 1);
            isInCus = true;
        }
        cur_scene_name = "Select";
        Resources.UnloadUnusedAssets();
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
            foreach (string s in Directory.GetDirectories(currPath)){
                Button b = Instantiate(buttonForm, activeContent.transform);
                b.GetComponentInChildren<Text>().text = s.Replace(currPath, "");
                b.GetComponent<Image>().enabled = true;
                b.GetComponent<Button>().enabled = true;
                b.GetComponent<CustomFolderButton>().enabled = true;
                b.GetComponent<CustomFolderButton>().isFolder = true;
            }
            foreach(string s in Directory.GetFiles(currPath, "*.*", SearchOption.TopDirectoryOnly)){
                if (Regex.IsMatch(s, @"\.(bms|bme|bml|pms)$", RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)){
                    Button b = Instantiate(bmsItemForm, activeContent.transform);
                    b.GetComponentInChildren<Text>().text = s.Replace(currPath, "");
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
            currPath = currPath.Replace(currPath.Split('/', '\\')[currPath.Split('/', '\\').Length - 2] + "\\", "");
            if (currPath.Length >= songsPath.Length){
                isInCus = true;
                loaded = false;
            }else if (currPath.Length < songsPath.Length){
                currPath = songsPath;
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
