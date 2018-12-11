using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OM_test : MonoBehaviour {

	[TextArea(3, 5)]
	public string input = "{\n"+
		"a:5, \n"+
		"text:\'Hello World!\'\n"+
		"}";

	public class TestClass {
		public int a;
		public string text;
	}

	// Use this for initialization
	void Start () {
		object ob = OMU.Util.FromScript(input);
		Debug.Log(OMU.Util.ToScript(ob));

		object tc = new TestClass();
		OMU.Util.FromScriptOverwrite(input, ref tc);
		string scripted = OMU.Util.ToScriptTiny(tc);
		Debug.Log(scripted);

		ob = OMU.Util.FromScript(scripted);
		tc = ob as TestClass;
		Debug.Log(ob.GetType());
		Debug.Log(OMU.Util.ToScript(tc));
	}

	// Update is called once per frame
	void Update () {
		
	}
}
