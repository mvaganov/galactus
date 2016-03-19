using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ResourceSensor : MonoBehaviour {
    public ResourceEater sensorOwner;
    public Transform lookTransform;
    public float range = 200;
    public float sensorUpdateTime = 1.0f/32;
    float sensorTimer = 0;

    public static Color bigr = new Color(.75f, 0, 0, .75f);
    public static Color peer = new Color(.75f, .75f, .75f, .75f);
    public static Color lowr = new Color(0, .75f, 0, .75f);

    public GameObject textPrefab;
    public Camera cam;
    List<GameObject> textEntries = new List<GameObject>();
//    List<GameObject> lines = new List<GameObject>();

    void Start () {
	}
	
	void FixedUpdate () {
        sensorTimer += Time.deltaTime;
        if(sensorTimer >= sensorUpdateTime)
        {
            sensorTimer = 0;
            float rad = sensorOwner.radius * 10;
            RaycastHit[] hits = Physics.CapsuleCastAll(
                cam.transform.position + cam.transform.forward * rad,
                cam.transform.position + cam.transform.forward * range,
                rad, lookTransform.forward);
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
                    if (textEntries.Count <= validOnes)
                    {
                        GameObject tentr = Instantiate(textPrefab) as GameObject;
                        tentr.transform.SetParent(cam.transform);
                        textEntries.Add(tentr);
//                        lines.Add(new GameObject());
                    }
                    Collider c = hits[i].collider;
                    UnityEngine.UI.Text t = textEntries[validOnes].GetComponent<UnityEngine.UI.Text>();

                    Transform p = t.transform.parent;
                    t.transform.SetParent(null);
                    t.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                    t.transform.SetParent(p);

                    //for (int n = 0; n < validOnes; ++n) { t.text += "\n"; }
                    t.text = c.name+"\n"+((int)reat.mass);

                    if (reat.mass * 0.85f > sensorOwner.mass)
                    {
                        t.color = bigr;
                    } else if(reat.mass < sensorOwner.mass * 0.85f)
                    {
                        t.color = lowr;
                    }
                    else
                    {
                        t.color = peer;
                    }

                    Vector3 v = cam.WorldToScreenPoint(c.gameObject.transform.position);
                    Ray r = cam.ScreenPointToRay(v);
                    float d = Vector3.Distance(cam.transform.position, reat.transform.position);
                    t.transform.position = r.origin + r.direction * (d/50);
                    t.transform.rotation = lookTransform.rotation;
                    t.gameObject.SetActive(true);
                    validOnes++;
                }
            }
        }
	}
}
