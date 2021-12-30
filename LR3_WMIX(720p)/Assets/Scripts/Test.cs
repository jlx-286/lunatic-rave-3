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
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Video;
using Debug = UnityEngine.Debug;

public class Test : MonoBehaviour {
    private bool once;
    //public VideoPlayer videoPlayer;
    private MetaData metaData;
    // Start is called before the first frame update
    private void Start(){
        //once = false;
        //Debug.Log(Application.dataPath);
        //Debug.Log(M().Result);
        Debug.Log(MainVars.GetEncodingByFilePath(@"E:\Programs\BMS\BOF(2)\T2o(BOF2008)\Summer\Summer.bms"));
        Debug.Log($"default:{Encoding.Default}");
        Debug.Log($"utf-8:{Encoding.UTF8}");
        Debug.Log($"gb2312:{Encoding.GetEncoding("gb2312")}");
        Debug.Log($"gbk:{Encoding.GetEncoding("gbk")}");
        Debug.Log($"gb18030:{Encoding.GetEncoding("gb18030")}");
        Debug.Log($"shift_jis:{Encoding.GetEncoding("shift_jis")}");
        //string json = "{'encoding':'utf-8'}";
        //Debug.Log(json.Substring(json.IndexOf('{'), json.LastIndexOf('}') - json.IndexOf('{') + 1));
    }

    private async Task<string> M(){
        //Engine engine = new Engine(Application.dataPath + "/Programs/Windows/ffmpeg/ffmpeg.exe");
        //MediaFile mediaFile = new MediaFile(this.GetComponentInChildren<VideoPlayer>().url.Replace("file://", ""));
        //metaData = await engine.GetMetaDataAsync(mediaFile);
        return "666";
    }
    
    // Update is called once per frame
    void Update(){
        //if (!once && metaData != null){
        //    once = true;
        //    Debug.Log(metaData);
        //}
    }

}
