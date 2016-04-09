using UnityEngine;
using System.Collections;

public class UserSoul : MonoBehaviour {
    public ResourceSensor sensor;

    public float cameraDistance = 3;
    public Transform cameraTransform;
    private bool isPosessing = false, needsBody = true;
    private PlayerForce posessed = null;

    private Vector2 move;
    public float xSensitivity = 5, ySensitivity = 5;
    public bool invertY = false;
    public bool holdVector = false;

    public WarpGate warpgate;

    void Update() {
        if(posessed && posessed.IsAlive()) {
            posessed.GetResourceEater().DoUserActions(cameraTransform);
        }
        if (warpgate) {
            warpgate.UpdateKeypress();
        }
    }

    public Transform GetLookTransform() { return cameraTransform; }

    void LateUpdate()
    {
        move = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        if (move.x != 0)
        {
            transform.Rotate(0, move.x * xSensitivity, 0);
        }
        if (move.y != 0)
        {
            if (invertY) { move.y *= -1; }
            transform.Rotate(-move.y * ySensitivity, 0, 0);
        }
        if (posessed)
        {
            var d = Input.GetAxis("Mouse ScrollWheel");
            if (d > 0f) { cameraDistance -= 0.125f; if (cameraDistance < 0) cameraDistance = 0; }
            else if (d < 0f) { cameraDistance += 0.125f; }
            Vector3 delta = cameraTransform.forward.normalized * cameraDistance * posessed.transform.lossyScale.z;
            transform.position = posessed.transform.position - delta;
        }
        holdVector = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    }


    [System.Serializable]
    public class Settings
    {
        public string name = "you";
        // TODO add player preferences for: color, invertY, sensitivityX, sensitivityY
    }
    public Settings settings = new Settings();


    public bool IsPosessing() { return isPosessing; }
	public bool IsDisembodied() { return posessed == null && !isPosessing; }
    public bool IsInNeedOfBody() { return needsBody; }
    public void SetNeedsBody(bool need) { this.needsBody = need; }

    public void Disconnect() {
		if (posessed) {
			PlayerForce pf = posessed.GetComponent<PlayerForce>();
			pf.SetUserSoul(null);
			Prediction pred = GetComponent<Prediction> ();
			pred.toPredict = null;
        }
        posessed = null;
        isPosessing = false;
        sensor.sensorOwner = null;
    }

    public PlayerForce GetPossesed() { return posessed; }

    public void Posess(PlayerForce pf) {
        Disconnect();
        isPosessing = true;
        Transform n = pf.transform;
        SetNeedsBody(false);
        Vector3 oposition = transform.position;
        Vector3 oscale = transform.localScale;
        Quaternion orotation = transform.rotation;
        n.gameObject.SetActive(false); // hide the player until the zeroing-in-on-body process finishes
        TimeMS.LambdaProgress zeroingInOnNextBody = (t) => {
            transform.position = Vector3.Lerp(oposition, n.position, t);
            transform.rotation = Quaternion.Lerp(orotation, n.rotation, t);
            transform.localScale = Vector3.Lerp(oscale, n.lossyScale, t);
            if (t >= 1)
            {
                pf.gameObject.SetActive(true);
                posessed = pf;
                Prediction pred = GetComponent<Prediction>();
                pred.toPredict = n;
                pf.GetResourceEater().name = settings.name;
                pf.SetUserSoul(this);
                transform.localScale = new Vector3(1, 1, 1);
                isPosessing = false;
                sensor.RefreshSensorOwner(pf.GetResourceEater());
                if (warpgate) { warpgate.Setup(pf, cameraTransform); }
                //Debug.Log("watching for " + name + "'s destruction");
                // remove the soul before destruction...
                MemoryPoolRelease.Add(pf.gameObject, (obj) => {
                    //Debug.Log("I'm " + name + " and I'm Dead!");
                    Disconnect();
                });

            }
        };
        TimeMS.CallbackWithDuration(1000, zeroingInOnNextBody);
    }
}
