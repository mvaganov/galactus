using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class ResourceSensor : MonoBehaviour {
    public ResourceEater sensorOwner;
    public Transform lookTransform;
    public float range = 500;
	public float radiusExtra = 20;
    public float sensorUpdateTime = 1.0f/2;
    float sensorTimer = 0;

    public static Color selfColor = new Color(1, 1, 1);
    public static Color bigr = new Color(1, 0, 0, .75f);
    public static Color peer = new Color(.75f, .75f, .75f, .75f);
    public static Color lowr = new Color(1, 1, 1, .75f);
    public static Color distColor = new Color(1, 1, 1);

    public GameObject textPrefab;
    public Camera cam;
    /// <summary>all of the text UI</summary>
    List<GameObject> textEntries = new List<GameObject>();
    int usedEntries;

    List<SpriteRenderer> icons = new List<SpriteRenderer>();
    int usedIcons;

    void LateUpdate(){
		for (int i = textEntries.Count-1; i >= 0; --i) {
            if (!textEntries[i]) textEntries.RemoveAt(i);
			else if (textEntries [i].activeInHierarchy) {
				textEntries [i].transform.rotation = transform.rotation;
			}
		}
		for (int i = icons.Count-1; i >= 0; --i) {
            if (!icons[i]) icons.RemoveAt(i);
			else if (icons[i].gameObject.activeInHierarchy) {
                icons[i].transform.rotation = transform.rotation;
			}
		}
	}

	public ResourceEater RefreshSensorOwner(ResourceEater whoIsSensing) {
		sensorOwner = whoIsSensing;
		if (!sensorOwner) {
			Transform t = transform;
			PlayerForce pf = null;
			UserSoul soul = null;
			do {
				pf = t.GetComponent<PlayerForce> ();
				if (!pf) {
                    soul = t.GetComponent<UserSoul> ();
					if (soul) {
                        pf = soul.GetBiggestBody().GetPlayerForce();//GetPossesed();
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

    Text GetFreeText() {
        Text t = null;
        if (textEntries.Count <= usedEntries) {
            GameObject tentr = Instantiate(textPrefab) as GameObject;
            textEntries.Add(tentr);
        }
        t = textEntries[usedEntries++].GetComponent<Text>();
        t.gameObject.SetActive(true);
        return t;
    }
    SpriteRenderer GetFreeIcon() {
        SpriteRenderer i = null;
        if (icons.Count <= usedIcons) {
            GameObject g = new GameObject();
            i = g.AddComponent<SpriteRenderer>();
            icons.Add(i);
        } else {
            i = icons[usedIcons++].GetComponent<SpriteRenderer>();
        }
        i.gameObject.SetActive(true);
        return i;
    }
    void FreeUpAllText() {
        for (int i = 0; i < textEntries.Count; ++i) {
            textEntries[i].SetActive(false);
        }
        usedEntries = 0;
    }
    void FreeUpAllIcons() {
        for (int i = 0; i < icons.Count; ++i) {
            icons[i].gameObject.SetActive(false);
        }
        usedIcons = 0;
    }

    GameObject DBG_rad, DBG_otherRad, DBG_dist;

    void FixedUpdate () {
        if(!sensorOwner) return;
        sensorTimer += Time.deltaTime;
        if(sensorTimer >= sensorUpdateTime) {
            sensorTimer = 0;
            FreeUpAllText();
            FreeUpAllIcons();
            // find everything that *might* need sensor info this time
            Ray r = new Ray (cam.transform.position, cam.transform.forward);
			RaycastHit[] hits = Physics.SphereCastAll(r, sensorOwner.effectsSize + radiusExtra, range+sensorOwner.effectsSize);
            float mostCenteredDist = -1, test;
            Text distText = null;
            Sprite icon = null;
            Vector2 midScreen = new Vector2(0.5f, 0.5f);
            for(int i = 0; i < hits.Length; ++i) {
                ResourceEater reat = hits[i].collider.gameObject.GetComponent<ResourceEater>();
                ResourceNode rn = (reat)? null : hits[i].collider.gameObject.GetComponent<ResourceNode>();
                // only show a few kinds of things. Living ResourceEaters and ResourceNodes
                bool showThisOne = (reat != null && reat.IsAlive()) || rn;
                if (showThisOne) {
                    Collider c = hits[i].collider;
                    if (!c) continue;
                    float distCamera = Vector3.Distance(cam.transform.position, c.transform.position);
                    Vector3 delta = c.transform.position - sensorOwner.transform.position;
                    float dist = delta.magnitude;

                    //Vector3 dir = sensorOwner.transform.forward;
                    //Vector3 radEnd = sensorOwner.transform.position + dir * sensorOwner.GetSize()/2;
                    //if (dist != 0) {
                    //    dir = delta / dist;
                    //    radEnd = sensorOwner.transform.position + dir * sensorOwner.GetSize()/2;
                    //}
                    //Lines.Make(ref DBG_rad, Color.cyan, sensorOwner.transform.position, radEnd, .1f, .1f);
                    //Vector3 otherRadEnd = c.transform.position;

                    dist -= sensorOwner.GetSize()/2;
                    float s = 0.01f;
                    s *= distCamera / 2.0f;
                    if (reat) {

                        //otherRadEnd = reat.transform.position - dir * reat.GetSize()/2;
                        //Lines.Make(ref DBG_otherRad, Color.magenta, reat.transform.position, otherRadEnd, .1f, .1f);

                        dist -= reat.GetSize()/2;
                        //dist -= reat.GetRadius();
                        Color color;
                        FontStyle fontStyle;
                        // name
                        if(reat == sensorOwner) {
                            color = selfColor;
                            fontStyle = FontStyle.Normal;
                        } else if (reat.mass * ResourceEater.MINIMUM_PREY_SIZE > sensorOwner.mass) {
                            color = bigr;
                            fontStyle = FontStyle.Italic;
                        } else if (reat.mass < sensorOwner.mass * ResourceEater.MINIMUM_PREY_SIZE) {
                            color = lowr;
                            fontStyle = FontStyle.Bold;
                        } else {
                            color = peer;
                            fontStyle = FontStyle.Normal;
                        }
                        Text t = DoText(c.name + "\n" + ((int)reat.mass), c.transform, s, color, fontStyle);
                        icon = (reat.team != null) ? reat.team.icon : null;
                        if (icon) {
                            Doicon(icon, t.transform, 30, reat.team.color);
                        }
                    }

                    //Lines.Make(ref DBG_dist, Color.gray, radEnd, otherRadEnd, 0.3f, 0.3f);

                    if (dist > 0) {
                        Vector2 screenPos = cam.WorldToViewportPoint(c.transform.position);
                        // show the distance if it is the distance value closest to the center of the screen (to avoid clutter)
                        test = Vector2.Distance(screenPos, midScreen) * dist;
                        if (!distText || test < mostCenteredDist) {
                            mostCenteredDist = test;
                            if (distText) distText.gameObject.SetActive(false);
                            distText = DoText("\n\n\n" + ((int)dist), c.transform, s * 0.75f, distColor, FontStyle.Normal);
                        }
                    }
                }
            }
        }
	}
    Text DoText(string text, Transform forWho, float size, Color color, FontStyle style) {
        Text t = GetFreeText();
        t.transform.SetParent(null);
        t.transform.localScale = new Vector3(size, size, size);
        t.transform.rotation = lookTransform.rotation;
        t.transform.SetParent(forWho);
        t.transform.localPosition = Vector3.zero;// offset;
        t.text = text;
        t.color = color;
        t.fontStyle = style;
        return t;
    }
    SpriteRenderer Doicon(Sprite img, Transform forWho, float size, Color color) {
        SpriteRenderer sr = GetFreeIcon();
        //sr.transform.SetParent(null);
        //sr.transform.localScale = new Vector3(size, size, size);
        //sr.transform.rotation = lookTransform.rotation;
        sr.transform.SetParent(forWho);
        sr.transform.localScale = new Vector3(size, size, size);
        sr.transform.localPosition = new Vector3(0, 20, 0);//Vector3.zero;// offset;
        sr.sprite = img;
        Color c = color;
        c.a = 0.75f;
        sr.color = c;
        return sr;
    }
}
