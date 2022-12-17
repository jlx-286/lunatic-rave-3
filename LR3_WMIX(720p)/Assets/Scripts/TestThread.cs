using System.Threading;
using UnityEngine;
public class TestThread : MonoBehaviour {
	private Thread thread;
	void Start () {
		thread = new Thread(()=>{
			while(true){
				Debug.Log(null);
				Thread.Sleep(1000);
			}
		});
		thread.Start();
	}
	private void OnDestroy() {
		Debug.Log("OnDestroy");
		if(thread != null){
			thread.Abort();
			Debug.Log(thread.IsAlive);
			thread = null;
		}
	}
}
