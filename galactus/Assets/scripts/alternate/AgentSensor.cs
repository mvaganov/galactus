using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class AgentSensor : MonoBehaviour
{
	public EnergyAgent sensorOwner;
	private Agent_MOB mob;
	public Camera cam;
	public float range = 500;
	public float radiusExtra = 20;
	public float sensorUpdateTime = 1.0f / 32;
	private float sensorTimer = 0;

	public static Color selfColor = new Color (1, 1, 1);
	public static Color bigr = new Color (1, 0, 0, .75f);
	public static Color peer = new Color (.75f, .75f, .75f, .75f);
	public static Color lowr = new Color (1, 1, 1, .75f);
	public static Color ally = new Color (0, 1, 1, .75f);
	public static Color distColor = new Color (1, 1, 1);

	public GameObject textPrefab;
	private GameObject iconPrefab;
	private GameObjectPool textPool, iconPool;

	void Start() {
		if (!cam) {
			cam = GetComponent<Camera> ();
		}
		if (iconPrefab == null) {
			iconPrefab = new GameObject ("<icon prefab>");
			iconPrefab.AddComponent<SpriteRenderer> ();
		}
		textPool = new GameObjectPool (textPrefab);
		iconPool = new GameObjectPool (iconPrefab);
	}

	void LateUpdate ()
	{
		textPool.ForEach (te => te.transform.rotation = transform.rotation);
		iconPool.ForEach(i => i.transform.rotation = transform.rotation);
	}

	/// <summary>
	/// Refreshes the sensor owner. Called by the controlling camera object
	/// </summary>
	/// <param name="whoIsSensing">Who is sensing.</param>
	public void RefreshSensorOwner (EnergyAgent whoIsSensing)
	{
		sensorOwner = whoIsSensing;
	}
	GameObject DBG_rad, DBG_otherRad, DBG_dist;

	void FixedUpdate ()
	{
		if (!sensorOwner)
			return;
		mob = sensorOwner.GetComponent<Agent_MOB> ();
		sensorTimer += Time.deltaTime;
		if (sensorTimer >= sensorUpdateTime) {
			sensorTimer = 0;
			textPool.FreeAll();
			iconPool.FreeAll();
			// find everything that *might* need sensor info this time
			Ray r = new Ray (cam.transform.position, cam.transform.forward);
			RaycastHit[] hits = Physics.SphereCastAll (r, sensorOwner.GetRadius () + radiusExtra, range + sensorOwner.GetRadius ());
			float mostCenteredDist = -1, distFromCenterOfScreen;
			Text distText = null;
			Sprite icon = null;
			Vector2 midScreen = new Vector2 (0.5f, 0.5f);
			for (int i = 0; i < hits.Length; ++i) {
				EnergyAgent nrg = hits [i].collider.gameObject.GetComponent<EnergyAgent> ();
				if (!nrg || nrg == sensorOwner)
					continue;
				GroupMember grp = nrg.GetComponent<GroupMember> ();
				if (nrg) {
					Collider c = hits [i].collider;
					if (!c)
						continue;
					float distCamera = Vector3.Distance (cam.transform.position, c.transform.position);
					Vector3 delta = c.transform.position - sensorOwner.transform.position;
					float dist = delta.magnitude;

					dist -= sensorOwner.GetRadius () / 2;
					float s = 0.01f;
					s *= distCamera / 2.0f;

					dist -= nrg.GetRadius () / 2;
					Color color;
					FontStyle fontStyle;
					// name
					string label;
					if (nrg.GetEatSphere ()) {
						if (nrg == sensorOwner) {
							color = selfColor;
							fontStyle = FontStyle.Normal;
						} else if (nrg.GetRadius () * ResourceEater.MINIMUM_PREY_SIZE > sensorOwner.GetRadius ()) {
							color = bigr;
//                            if (reat.team == sensorOwner.team) color = ally;
							fontStyle = FontStyle.Italic;
						} else if (nrg.GetRadius () < sensorOwner.GetRadius () * ResourceEater.MINIMUM_PREY_SIZE) {
							color = lowr;
//                            if (reat.team == sensorOwner.team) color = ally;
							fontStyle = FontStyle.Bold;
						} else {
							color = peer;
//                            if (reat.team == sensorOwner.team) color = ally;
							fontStyle = FontStyle.Normal;
						}
						label = c.name + "\n" + ((int)nrg.GetRadius ());
						if (nrg == sensorOwner) {
							label += "  "+ ((int)nrg.GetEnergy ()) + "e";
						}
					} else {
						label = "\n" + ((int)nrg.GetEnergy ()) + "e";
						fontStyle = FontStyle.Normal;
						color = Color.white;
					}
					Text t = DoText (label, c.transform, s, color, fontStyle);
					icon = (grp && grp.team) ? grp.team.icon : null;
					if (icon) {
						Doicon (icon, t.transform, 30, grp.team.color);
					}

					//Lines.Make(ref DBG_dist, Color.gray, radEnd, otherRadEnd, 0.3f, 0.3f);

					if (dist > 0) {
						Vector2 screenPos = cam.WorldToViewportPoint (c.transform.position);
						// show the distance if it is the distance value closest to the center of the screen (to avoid clutter)
						distFromCenterOfScreen = Vector2.Distance (screenPos, midScreen) * dist;
						if (!distText || distFromCenterOfScreen < mostCenteredDist) {
							mostCenteredDist = distFromCenterOfScreen;
							if (distText)
								distText.gameObject.SetActive (false);
							FontStyle fstyle = FontStyle.Normal;
							float bDist = mob.GetBrakeDistance ();
							string dText = ((int)dist).ToString();
							if (dist >= (bDist - sensorOwner.GetRadius ()) && dist <= bDist + sensorOwner.GetRadius ()) {
								dText = "{" + dText + "}";
							}
							distText = DoText ("\n\n\n" + dText, c.transform, s * 0.75f, distColor, fstyle);
						}
					}
				}
			}
		}
	}

	Text DoText (string text, Transform forWho, float size, Color color, FontStyle style)
	{
		Text t = textPool.Alloc ().GetComponent<Text> ();
		t.transform.SetParent (null);
		t.transform.localScale = new Vector3 (size, size, size);
		t.transform.rotation = cam.transform.rotation;
		t.transform.SetParent (forWho);
		t.transform.localPosition = Vector3.zero;// offset;
		t.text = text;
		t.color = color;
		t.fontStyle = style;
		return t;
	}

	SpriteRenderer Doicon (Sprite img, Transform forWho, float size, Color color)
	{
		SpriteRenderer sr = iconPool.Alloc ().GetComponent<SpriteRenderer> ();
		sr.transform.SetParent (forWho);
		sr.transform.localScale = new Vector3 (size, size, size);
		sr.transform.localPosition = new Vector3 (0, 20, 0);//Vector3.zero;// offset;
		sr.sprite = img;
		Color c = color;
		c.a = 0.75f;
		sr.color = c;
		return sr;
	}
}
