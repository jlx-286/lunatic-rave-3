using FFmpeg.NET;
using Microsoft.CSharp;
using Microsoft.Scripting.Hosting;
using NAudio;
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Video;
using Debug = UnityEngine.Debug;

public class Test : MonoBehaviour {
    private bool once;
    //public VideoPlayer videoPlayer;
    //public AudioSource audioSourceForm;
    //private AudioSource audioSource;
    private bool completed;
    private Engine engine;
    private MetaData metaData;
    // Start is called before the first frame update
    private void Start(){
        //Debug.Log(Path.GetFileNameWithoutExtension("01.d.ts"));
        //Debug.Log(Application.dataPath);
        //Debug.Log(MainVars.GetEncodingByFilePath(@"E:\Programs\BMS\BOF(2)\T2o(BOF2008)\Summer\Summer.bms"));
        //Debug.Log($"default:{Encoding.Default}");
        //Debug.Log($"utf-8:{Encoding.UTF8}");
        //Debug.Log($"gb2312:{Encoding.GetEncoding("gb2312")}");
        //Debug.Log($"gbk:{Encoding.GetEncoding("gbk")}");
        //Debug.Log($"gb18030:{Encoding.GetEncoding("gb18030")}");
        //Debug.Log($"shift_jis:{Encoding.GetEncoding("shift_jis")}");
    }

    // Update is called once per frame
    private void Update(){
        if (Input.GetKeyUp(KeyCode.Space)){
            Debug.Log(KeyCode.Space);
        }
    }
    private void FixedUpdate(){
        if (Input.GetKeyUp(KeyCode.KeypadEnter)){
            Debug.Log(KeyCode.KeypadEnter);
        }
    }

}
