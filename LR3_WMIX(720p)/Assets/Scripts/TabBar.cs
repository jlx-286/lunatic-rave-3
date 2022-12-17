using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class TabBar : MonoBehaviour{
    public GameObject[] panels;
    public Toggle[] toggles;
    public Material high_lighted;
    void Start(){
        for (int i = 0; i < toggles.Length; i++){
            int j = i;
            toggles[j].onValueChanged.AddListener(
                value => panels[j].SetActive(value)
            );
        }
        if (high_lighted != null){
            EventTrigger[] eventTriggers;
            //eventTriggers = this.gameObject.GetComponentsInChildren<EventTrigger>();
            eventTriggers = new EventTrigger[toggles.Length];
            Image[] images = new Image[toggles.Length];
            for(int i = 0; i < toggles.Length; i++){
                int j = i;
                images[j] = toggles[j].GetComponentInChildren<Image>();
                if(toggles[j].GetComponent<EventTrigger>() == null)
                    toggles[j].gameObject.AddComponent<EventTrigger>();
                eventTriggers[j] = toggles[j].GetComponent<EventTrigger>();
                EventTrigger.Entry entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.PointerEnter;
                entry.callback.AddListener((data) => {
                    images[j].material = high_lighted;
                });
                eventTriggers[j].triggers.Add(entry);
                EventTrigger.Entry entry_ = new EventTrigger.Entry();
                entry_.eventID = EventTriggerType.PointerExit;
                entry_.callback.AddListener((data) => {
                    images[j].material = null;
                });
                eventTriggers[j].triggers.Add(entry_);

            }
        }
    }
    //void Update () {}
}
