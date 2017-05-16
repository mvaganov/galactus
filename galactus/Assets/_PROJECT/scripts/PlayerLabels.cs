using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerLabels : MonoBehaviour {

	//public Camera mainCam;
	//public PlayerMaker players;
	//public GameObject prefab_label;

	//public float maxDistance = 100;
	//public float minFontSize = 8, maxFontSize = 80;

	//Dictionary<GameObject, GameObject> playerLabels = new Dictionary<GameObject, GameObject>();

	//// Use this for initialization
	//void Start () {
	//}
	
	//// Update is called once per frame
	//void Update () {
	//	if(mainCam == null) {
	//		mainCam = Camera.main;
	//	}
	//	if(mainCam == null) {
	//		return;
	//	}

	//	if(players.agents.GetAllObjects() != null) {
	//		foreach(GameObject p in players.agents.GetAllObjects()) {
	//			GameObject label;
	//			if(!playerLabels.TryGetValue(p, out label)) {
	//				label = Instantiate(prefab_label);
	//				label.transform.SetParent(transform);
	//				playerLabels[p] = label;
	//			}
	//			if(p.activeInHierarchy && p.GetComponent<Renderer>().isVisible) {
	//				Vector3 delta = p.transform.position - mainCam.transform.position;
	//				float dist, dot = Vector3.Dot(mainCam.transform.forward, delta);
	//				if(dot > 0 && (dist = delta.magnitude) < maxDistance) {
	//					label.SetActive(true);
	//					Vector3 point = mainCam.WorldToScreenPoint(p.transform.position);
	//					point.y += (maxDistance - dist)/ (maxDistance / 10);
	//					label.transform.position = point;
	//					UnityEngine.UI.Text t = label.GetComponent<UnityEngine.UI.Text>();
	//					t.fontSize = (int)(minFontSize + ((maxFontSize - minFontSize) * (maxDistance - dist) / maxDistance));
	//					t.text = p.name;
	//				} else {
	//					label.SetActive(false);
	//				}
	//			} else {
	//				label.SetActive(false);
	//			}
	//		}
	//	}
	//}
}
