using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ResourceSensor : MonoBehaviour {
    public ResourceEater sensorOwner;
    public Transform lookTransform;
    public float range = 500;
	public float radiusExtra = 20;
    public float sensorUpdateTime = 1.0f/2;
    float sensorTimer = 0;

    public static Color bigr = new Color(1, 0, 0, .75f);
    public static Color peer = new Color(.75f, .75f, .75f, .75f);
    public static Color lowr = new Color(1, 1, 1, .75f);

    public GameObject textPrefab;
    public Camera cam;
    List<GameObject> textEntries = new List<GameObject>();

	void LateUpdate(){
		for (int i = textEntries.Count-1; i >= 0; --i) {
            if (!textEntries[i]) textEntries.RemoveAt(i);
			else if (textEntries [i].activeInHierarchy) {
				textEntries [i].transform.rotation = transform.rotation;
			}
		}
	}

	public ResourceEater RefreshSensorOwner(ResourceEater whoIsSensing) {
		sensorOwner = whoIsSensing;
		if (!sensorOwner) {
			Transform t = transform;
			PlayerForce pf = null;
			UserSoul rp = null;
			do {
				pf = t.GetComponent<PlayerForce> ();
				if (!pf) {
					rp = t.GetComponent<UserSoul> ();
					if (rp && rp.IsPosessing ()) {
						pf = rp.posessed.GetComponent<PlayerForce> ();
					}
				}
				t = t.parent;
			} while (!pf && t);
			if (pf) {
				sensorOwner = pf.GetResourceEater ();
			}
		}
        return sensorOwner;
    }

    void FixedUpdate () {
        if(!sensorOwner) return;
        sensorTimer += Time.deltaTime;
        if(sensorTimer >= sensorUpdateTime) {
            sensorTimer = 0;
			Ray r = new Ray (cam.transform.position, cam.transform.forward);
			RaycastHit[] hits = Physics.SphereCastAll(r, sensorOwner.effectsRadius + radiusExtra, range+sensorOwner.effectsRadius);
            for(int i = 0; i < textEntries.Count; ++i) {
                textEntries[i].SetActive(false);
            }
            int validOnes = 0;
            for(int i = 0; i < hits.Length; ++i) {
                ResourceEater reat = hits[i].collider.gameObject.GetComponent<ResourceEater>();
                bool showThisOne = reat != null && reat.mass > 0;
                if (showThisOne) {
					float d = Vector3.Distance(cam.transform.position, reat.transform.position);
                    if (textEntries.Count <= validOnes) {
                        GameObject tentr = Instantiate(textPrefab) as GameObject;
						tentr.transform.SetParent (reat.transform);//cam.transform);
                        textEntries.Add(tentr);
                    }
                    Collider c = hits[i].collider;
                    UnityEngine.UI.Text t = textEntries[validOnes].GetComponent<UnityEngine.UI.Text>();

                    t.transform.SetParent(null);
					float s = 0.01f;
					s *= d/2.0f;
                    t.transform.localScale = new Vector3(s,s,s);
					t.transform.rotation = lookTransform.rotation;
					t.transform.SetParent(c.transform);
					t.transform.localPosition = Vector3.zero;
                    t.text = c.name+"\n"+((int)reat.mass);
                    if (reat.mass * ResourceEater.minimumPreySize > sensorOwner.mass) {
                        t.color = bigr;
                        t.fontStyle = FontStyle.Italic;
                    }
                    else if(reat.mass < sensorOwner.mass * ResourceEater.minimumPreySize) {
                        t.color = lowr;
                        t.fontStyle = FontStyle.Bold;
                    } else {
                        t.color = peer;
                        t.fontStyle = FontStyle.Normal;
                    }
                    t.gameObject.SetActive(true);
                    validOnes++;
                }
            }
        }
	}
}
