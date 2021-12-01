using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class LeftMenu : MonoBehaviour{
    //public GameObject[] options;
    public GameObject[] panels;
    public GameObject[] buttons;
    public Sprite[] lighted_icons;
    public Sprite[] default_icons;
    // Use this for initialization
    void Start () {
		for(int i = 0; i < buttons.Length; i++){
            int j = i;
            if (buttons[j] != null){
                buttons[j].GetComponent<Button>().onClick.AddListener(delegate(){
                    TogglePanel(j);
                });
            }
        }
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyUp(KeyCode.Escape)){
            //sqliteConnection.Close();
            //sqliteConnection = null;
            #if UNITY_EDITOR
                EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
    }
    private void TogglePanel(int index){
        for(int i = 0; i < buttons.Length; i++){
            if (i != index && panels[i] != null){
                buttons[i].GetComponent<Image>().sprite = default_icons[i];
                panels[i].SetActive(false);
            }else if(i == index && panels[i] != null){
                if (panels[i].activeInHierarchy){
                    panels[i].SetActive(false);
                    buttons[i].GetComponent<Image>().sprite = default_icons[i];
                }else{
                    panels[i].SetActive(true);
                    buttons[i].GetComponent<Image>().sprite = lighted_icons[i];
                }
            }
        }
    }
}
