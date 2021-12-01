using NAudio;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Video;
using Debug = UnityEngine.Debug;

public class Test : MainVars {
    private Process process;
    private AudioSource audioSource;
    // Start is called before the first frame update
    void Start(){
        //audioSource = this.GetComponent<AudioSource>();
        //audioSource.clip = GetAudioClipByFilePath(Path.GetFullPath("./Assets/Sounds/test/back_1.wav"));
        //audioSource.Play();
        //Debug.Log(double.Epsilon);
        //Debug.Log(double.Epsilon - double.Epsilon);
        Debug.Log(Equals(double.Epsilon - double.Epsilon, 0.0d));
        Debug.Log(Equals(double.Epsilon / 2d, 0.0d));
    }

    // Update is called once per frame
    void Update(){}

}
