using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class Test : MonoBehaviour {
    private double playingTime = double.Epsilon;
    // Start is called before the first frame update
    private void Start(){
        //Debug.Log(Application.persistentDataPath);
        //Debug.Log(temp - Math.Truncate(temp));
        //Debug.Log(Path.GetDirectoryName(@"\Programs\BMS"));
        //Debug.Log(Path.GetDirectoryName(@"\Programs\BMS\"));
        //Debug.Log(Path.GetFileName(@"\Programs\BMS"));
        //Debug.Log(Path.GetFileName(@"\Programs\BMS\"));
        //Debug.Log(Directory.GetDirectories(@"E:\Programs\BMS\")[0]);
        //Debug.Log(File.Exists(null));
        //Debug.Log(Application.dataPath);
        //Debug.Log($"default:{Encoding.Default}");
        //Debug.Log($"utf-8:{Encoding.UTF8}");
        //Debug.Log($"shift_jis:{Encoding.GetEncoding("shift_jis")}");
    }
    
    // Update is called once per frame
    private void Update(){
        //if (Input.GetKeyUp(KeyCode.Space)){
        //    Debug.Log(KeyCode.Space);
        //}
    }
    private void FixedUpdate(){
        //if (Input.GetKey(KeyCode.Space)){
        //    playingTime += Time.fixedDeltaTime;
        //    Debug.Log(playingTime);
        //}
        //if (unityActions != null && unityActions.Count > 0 && !doingAction){
        //    doingAction = true;
        //    unityActions.Dequeue()();
        //}
        //if (Input.GetKeyUp(KeyCode.Return)){
        //    Debug.Log(KeyCode.Return);
        //}
    }

}

