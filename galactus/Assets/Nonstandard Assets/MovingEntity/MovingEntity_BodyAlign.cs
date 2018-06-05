using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingEntity_BodyAlign : MonoBehaviour {
    public GameObject body;
    float distance;
    MovingEntity me;
    void Start() {
        Vector3 d = body.transform.position - transform.position;
        distance = d.magnitude;
        me = GetComponent<MovingEntity>();
        me.UpdateFacingDelegate = UpdateFacing;
        body.transform.SetParent(null);
    }
    public void UpdateFacing(Vector3 forward, Vector3 up) {
        Quaternion desiredRot = Quaternion.LookRotation(forward, up);
        //if(desiredRot != body.transform.rotation) {
            body.transform.position = transform.position + up * distance;
            body.transform.rotation = Quaternion.RotateTowards(body.transform.rotation, desiredRot, 
                Time.deltaTime*me.TurnSpeed);
        //}
    }
}
