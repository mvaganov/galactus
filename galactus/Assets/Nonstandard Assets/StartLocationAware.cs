using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartLocationAware : MonoBehaviour {
    public Vector3 StartLocation { get; protected set; }
    public KeyCode restartKey = KeyCode.Q;
    void Start () {
        StartLocation = transform.position;
	}
	
	void Update () {
        if(Input.GetKeyDown(restartKey)) { ReStart(); }
	}

    public void ReStart(){
        transform.position = StartLocation;
    }
}
