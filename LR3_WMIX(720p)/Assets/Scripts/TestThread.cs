using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
public class TestThread : MonoBehaviour, IPointerClickHandler {
	private Thread thread;
	public void Init(){
		Debug.Log("Init");
	}
	private void Awake(){
		Debug.Log("Awake");
	}
	private void OnEnable(){
		Debug.Log("OnEnable");
	}
	private void Start(){
		Debug.Log("Start");
		/*thread = new Thread(()=>{
			while(true){
				Debug.Log(null);
				Thread.Sleep(1000);
			}
		}){ IsBackground = true };*/
		// thread.Start();
	}
	private void OnDestroy(){
		Debug.Log("OnDestroy");
		if(thread != null){
			thread.Abort();
			Debug.Log(thread.IsAlive);
			thread = null;
		}
	}
	public virtual void OnPointerClick(PointerEventData data){
		Debug.Log(data);
		if(thread != null){
			thread.Abort();
			Debug.Log(thread.IsAlive);
		}
	}
}
