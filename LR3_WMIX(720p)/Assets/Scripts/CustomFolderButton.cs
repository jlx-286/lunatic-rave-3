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
    [HideInInspector] public bool isFolder;
    // Use this for initialization
    private void Start(){
        that = this.gameObject;
        button = that.GetComponent<Button>();
        button.onClick.AddListener(()=>{
            if (isFolder){
                MainVars.bms_file_path += that.GetComponentInChildren<Text>().text + '/';
                loaded = false;
            }else{
                MainVars.bms_file_path += that.GetComponentInChildren<Text>().text;
                SceneManager.UnloadSceneAsync("Select");
                //SceneManager.LoadScene("7k_1P_Play", LoadSceneMode.Additive);
                SceneManager.LoadScene("Decide", LoadSceneMode.Additive);
            }
        });
    }

    // Update is called once per frame
    private void Update(){}
}
