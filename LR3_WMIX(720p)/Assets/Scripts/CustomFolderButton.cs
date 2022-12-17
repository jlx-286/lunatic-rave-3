using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CustomFolderButton : SongList{
    private Button button;
    private GameObject that;
    [HideInInspector] public bool isFolder;
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
                SceneManager.LoadScene("Decide", LoadSceneMode.Additive);
            }
        });
    }
    //private void Update(){}
}
