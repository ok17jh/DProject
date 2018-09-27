using UnityEngine;
using System.Collections;

public class MyTriggerExample : MonoBehaviour {

	void DropTrigger (DragDropSystem dragdrop) {
		Debug.Log ("DROP DETECTED");
		Debug.Log ("Origin slot=" + dragdrop.originSlot + " Destination slot=" + dragdrop.destinationSlot + " Item Index=" + dragdrop.droppedItemIndex);
		if(dragdrop.originIsDrag)
			Debug.Log("Origin=DRAG");
		else
			Debug.Log("Origin=DROP");
		if(dragdrop.destinationIsDrag)
			Debug.Log("Destination=DRAG");
		else
			Debug.Log("Destination=DROP");
		Debug.Log ("ERROR CODE=" + dragdrop.dropErrorCode);
	}

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
