using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IR_Toggle : MonoBehaviour {
    public InputField id_input;
    public InputField server_input;
	// Use this for initialization
	//private void Start () {}
	
	// Update is called once per frame
	//private void Update () {}

    public void OnValueChanged(bool value){
        id_input.interactable = value;
        server_input.interactable = value;
    }
}
