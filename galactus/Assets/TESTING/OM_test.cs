using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class OM_test : MonoBehaviour {

	[TextArea(3, 5)]
	public string input = "{\n"+
		"a:5, \n"+
		"text:\'Hello World!\'\n"+
		"}";
/*

*/
	public TMPro.TMP_Text text;

	// Use this for initialization
	void Start () {
	}

	public NS.ObjectPtr thing;

	// Update is called once per frame
	void Update () {
	}
}
