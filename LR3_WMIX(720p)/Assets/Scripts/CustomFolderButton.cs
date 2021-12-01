using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CustomFolderButton : SongList{
    private Button button;
    private GameObject that;
    public bool isFolder;
    // Use this for initialization
    private void Start(){
        that = this.gameObject;
        button = that.GetComponent<Button>();
        button.onClick.AddListener(()=>{
            if (isFolder){
                currPath += that.GetComponentInChildren<Text>().text + "\\";
                loaded = false;
            }else{
                bms_file_path = currPath + this.gameObject.GetComponentInChildren<Text>().text;
                SceneManager.UnloadSceneAsync("Select");
                SceneManager.LoadScene("7k_1P_Play", LoadSceneMode.Additive);
            }
        });
    }

    // Update is called once per frame
    private void Update(){
        
    }
}
