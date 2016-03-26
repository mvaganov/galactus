using UnityEngine;
using System.Collections;

public class RespawningPlayer : MonoBehaviour {
    public ResourceSensor sensor;
    [System.Serializable]
    public class Settings
    {
        public string name= "you";
        // TODO add player preferences for: color, invertY, sensitivityX, sensitivityY
    }
    public Settings settings = new Settings();
    private bool isPosessing = false;
    public bool IsPosessing() { return isPosessing; }
    public bool IsDisembodied() { return transform.parent == null && !isPosessing; }

    public void Disconnect()
    {
        if (transform.parent)
        {
            PlayerForce pf = transform.parent.GetComponent<PlayerForce>();
            pf.playerControlled = false;
            MouseLook ml = pf.GetComponent<MouseLook>();
            ml.controlledBy = MouseLook.Controlled.player;
        }
        isPosessing = false;
        sensor.sensorOwner = null;
        transform.SetParent(null);
    }

    public void Posess(PlayerForce pf)
    {
        Disconnect();
        isPosessing = true;
        Transform n = pf.transform;
        TimeMS.CallbackWithDuration(1000, (t) => {
            transform.position = Vector3.Lerp(transform.position, n.position, t);
            transform.rotation = Quaternion.Lerp(transform.rotation, n.rotation, t);
            transform.localScale = Vector3.Lerp(transform.localScale, n.lossyScale, t);
            if (t >= 1)
            {
                transform.parent = n;
                pf.playerControlled = true;
                MouseLook ml = pf.GetComponent<MouseLook>();
                ml.controlledBy = MouseLook.Controlled.player;
                pf.GetResourceEater().name = settings.name;
                transform.localScale = new Vector3(1, 1, 1);
                isPosessing = false;
                sensor.RefreshSensorOwner();

                //Debug.Log("watching for " + name + "'s destruction");
                // remove the soul before destruction...
                MemoryPoolRelease.Add(pf.gameObject, (obj) => {
                    //Debug.Log("I'm " + name + " and I'm Dead!");
                    Disconnect();
                });

            }
        });
    }
}
