using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class IR_Toggle : MonoBehaviour {
    public InputField id_input;
    public InputField server_input;
    //private void Start(){}
    //private void Update(){}
    public void OnValueChanged(bool value){
        id_input.interactable = value;
        server_input.interactable = value;
    }
}
