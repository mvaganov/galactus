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
	public bool IsDisembodied() { return posessed == null && !isPosessing; }
	public Transform posessed = null;

    public void Disconnect()
    {
		if (posessed)
        {
			PlayerForce pf = posessed.GetComponent<PlayerForce>();
			pf.controllingTransform = null;
			//EntitySteering ml = pf.GetComponent<EntitySteering>();
			//ml.controlledBy = EntitySteering.Controlled.player;
			ThirdPersonCamera cam3 = GetComponent<ThirdPersonCamera> ();
			cam3.followedEntity = null;
			Prediction pred = GetComponent<Prediction> ();
			pred.toPredict = null;
        }
        isPosessing = false;
        sensor.sensorOwner = null;
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
                posessed = n;
				ThirdPersonCamera cam3 = GetComponent<ThirdPersonCamera> ();
				cam3.followedEntity = n;
				Prediction pred = GetComponent<Prediction> ();
				pred.toPredict = n;
                pf.GetResourceEater().name = settings.name;
				pf.controllingTransform = transform;
                transform.localScale = new Vector3(1, 1, 1);
                isPosessing = false;
				sensor.RefreshSensorOwner(pf.GetResourceEater());

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
