using UnityEngine;
using System.Collections;

public class RespawningPlayer : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public void Posess(PlayerForce pf)
    {
        transform.parent = pf.transform;
        pf.playerControlled = true;
        MouseLook ml = pf.GetComponent<MouseLook>();
        ml.controlledBy = MouseLook.Controlled.player;
    }
}
