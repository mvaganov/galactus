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
    public static Color peer = new Color(0, 0, 1, .75f);
    public static Color lowr = new Color(0, 1, 0, .75f);

    public GameObject textPrefab;
    public Camera cam;
    List<GameObject> textEntries = new List<GameObject>();
//    List<GameObject> lines = new List<GameObject>();

	void LateUpdate(){
		for (int i = textEntries.Count-1; i >= 0; --i) {
            if (!textEntries[i]) textEntries.RemoveAt(i);
			else if (textEntries [i].activeInHierarchy) {
				textEntries [i].transform.rotation = transform.rotation;
			}
		}
	}

	public ResourceEater RefreshSensorOwner(ResourceEater whoIsSensing)
    {
		sensorOwner = whoIsSensing;
		if (!sensorOwner) {
			Transform t = transform;
			PlayerForce pf = null;
			RespawningPlayer rp = null;
			do {
				pf = t.GetComponent<PlayerForce> ();
				if (!pf) {
					rp = t.GetComponent<RespawningPlayer> ();
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
        if(sensorTimer >= sensorUpdateTime)
        {
            sensorTimer = 0;
			//float rad = sensorOwner.effectsRadius + radiusExtra;
			Ray r = new Ray (cam.transform.position, cam.transform.forward);
			RaycastHit[] hits = Physics.SphereCastAll(r, sensorOwner.effectsRadius + radiusExtra, range+sensorOwner.effectsRadius);
            for(int i = 0; i < textEntries.Count; ++i)
            {
                textEntries[i].SetActive(false);
//                lines[i].SetActive(false);
            }
            int validOnes = 0;
            for(int i = 0; i < hits.Length; ++i)
            {
                ResourceEater reat = hits[i].collider.gameObject.GetComponent<ResourceEater>();
                bool showThisOne = reat != null && reat.mass > 0;
                if (showThisOne)
                {
					float d = Vector3.Distance(cam.transform.position, reat.transform.position);
                    if (textEntries.Count <= validOnes)
                    {
                        GameObject tentr = Instantiate(textPrefab) as GameObject;
						tentr.transform.SetParent (reat.transform);//cam.transform);
                        textEntries.Add(tentr);
//                        lines.Add(new GameObject());
                    }
                    Collider c = hits[i].collider;
                    UnityEngine.UI.Text t = textEntries[validOnes].GetComponent<UnityEngine.UI.Text>();

                    //Transform p = t.transform.parent;
                    t.transform.SetParent(null);
					float s = 0.01f;
					s *= d/2.0f;
                    t.transform.localScale = new Vector3(s,s,s);
					t.transform.rotation = lookTransform.rotation;
                    //t.transform.SetParent(p);
					t.transform.SetParent(c.transform);
					t.transform.localPosition = Vector3.zero;

                    //for (int n = 0; n < validOnes; ++n) { t.text += "\n"; }
                    t.text = c.name+"\n"+((int)reat.mass);

                    if (reat.mass * ResourceEater.minimumPreySize > sensorOwner.mass)
                    {
                        t.color = bigr;
                    } else if(reat.mass < sensorOwner.mass * ResourceEater.minimumPreySize)
                    {
                        t.color = lowr;
                    }
                    else
                    {
                        t.color = peer;
                    }

                    //Vector3 v = cam.WorldToScreenPoint(c.gameObject.transform.position);
                    //Ray r = cam.ScreenPointToRay(v);
                    //t.transform.position = r.origin + r.direction * (d/50);
                    t.gameObject.SetActive(true);
                    validOnes++;
                }
            }
        }
	}
}
