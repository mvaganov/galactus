using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class Agent_SensorLabeler : MonoBehaviour
{
	private Agent_Sensor sensor;
	private Agent_MOB mob;
	public Camera cam;
	public float sensorUpdateTime = 1.0f / 32;
	private float sensorTimer = 0;

	public static Color selfColor = new Color (1, 1, 1);
	public static Color bigr = new Color (1, 0, 0, .75f);
	public static Color peer = new Color (.75f, .75f, .75f, .75f);
	public static Color lowr = new Color (1, 1, 1, .75f);
	public static Color ally = new Color (0, 1, 1, .75f);
	public static Color distColor = new Color (1, 1, 1);

	public GameObject pfabText3D;
	private GameObject iconPrefab;
	private GameObjectPool textPool, iconPool;

	void Start() {
		if (!cam) {
			cam = GetComponent<Camera> ();
		}
		if (iconPrefab == null) {
			iconPrefab = new GameObject ("<icon prefab>");
			iconPrefab.AddComponent<SpriteRenderer> ();
			iconPrefab.transform.parent = Singleton.Get<TeamManager> ().transform;
		}
		textPool = new GameObjectPool (pfabText3D);
		iconPool = new GameObjectPool (iconPrefab);
	}

	void LateUpdate ()
	{
		textPool.ForEach (te => te.transform.rotation = transform.rotation);
		iconPool.ForEach (i => i.transform.rotation = transform.rotation);
	}

	/// <summary>
	/// Refreshes the sensor owner. Called by the controlling camera object
	/// </summary>
	/// <param name="whoIsSensing">Who is sensing.</param>
	public void RefreshSensorOwner (GameObject whoIsSensing)
	{
		sensor = whoIsSensing.GetComponent<Agent_Sensor> ();
		sensor.EnsureOwnerIsKnown ();
//		sensorOwner = whoIsSensing;
		mob = sensor.sizeAndEffects.GetComponent<Agent_MOB> ();
	}
	GameObject DBG_rad, DBG_otherRad, DBG_dist;

	void UpdateFromSensed(RaycastHit[] hits) {
		textPool.FreeAll();
		iconPool.FreeAll();
		if (hits == null) return;
		float mostCenteredDist = -1, distFromCenterOfScreen;
		Text distText = null;
		Sprite icon = null;
		Vector2 midScreen = new Vector2 (0.5f, 0.5f);
		for (int i = 0; i < hits.Length; ++i) {
			// TODO this seems weird that it isn't Agent_Properties...
			Agent_SizeAndEffects nrg = hits [i].collider.gameObject.GetComponent<Agent_SizeAndEffects> ();
			if (!nrg || nrg == sensor.sizeAndEffects)
				continue;
			TeamMember grp = nrg.GetComponent<TeamMember> ();
			if (nrg) {
				Collider c = hits [i].collider;
				if (!c)
					continue;
				float distCamera = Vector3.Distance (cam.transform.position, c.transform.position);
				Vector3 delta = c.transform.position - sensor.sizeAndEffects.transform.position;
				float dist = delta.magnitude;

				dist -= sensor.sizeAndEffects.GetRadius ();
				float s = 0.01f;
				s *= distCamera / 2.0f;

				dist -= nrg.GetRadius ();
				Color color;
				FontStyle fontStyle;
				// name
				string label;
				if (nrg.GetEatSphere ()) {
					if (nrg == sensor.sizeAndEffects) {
						color = selfColor;
						fontStyle = FontStyle.Normal;
					} else if (nrg.GetSize () * GameRules.MINIMUM_PREY_SIZE > sensor.sizeAndEffects.GetSize ()) {
						color = bigr;
						//                            if (reat.team == sensorOwner.team) color = ally;
						fontStyle = FontStyle.Italic;
					} else if (nrg.GetSize () < sensor.sizeAndEffects.GetSize () * GameRules.MINIMUM_PREY_SIZE) {
						color = lowr;
						//                            if (reat.team == sensorOwner.team) color = ally;
						fontStyle = FontStyle.Bold;
					} else {
						color = peer;
						//                            if (reat.team == sensorOwner.team) color = ally;
						fontStyle = FontStyle.Normal;
					}
					label = c.name + "\n" + ((int)nrg.GetSize ());
					if (nrg == sensor.sizeAndEffects) {
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
						if (dist >= (bDist - sensor.sizeAndEffects.GetRadius ()) && dist <= bDist + sensor.sizeAndEffects.GetRadius ()) {
							dText = "{" + dText + "}";
						}
						distText = DoText ("\n\n\n" + dText, c.transform, s * 0.75f, distColor, fstyle);
					}
				}
			}
		}
	}
	void FixedUpdate ()
	{
		if (!sensor)
			return;
		sensorTimer -= Time.deltaTime;
		if (sensorTimer <= 0) {
			sensorTimer = sensorUpdateTime;
			// find everything that *might* need sensor info this time
			//Ray r = new Ray (cam.transform.position, cam.transform.forward);
			//RaycastHit[] hits = Physics.SphereCastAll (r, sensor.owner.GetRadius () + radiusExtra, range + sensor.owner.GetRadius ());
			UpdateFromSensed (sensor.GetSnapshot().sensed);
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
