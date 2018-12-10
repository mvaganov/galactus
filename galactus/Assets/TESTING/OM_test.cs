using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OM_test : MonoBehaviour {

    [TextArea(3, 5)]
    public string input = "{\n"+
        "a:5,"+
        "text:\'Hello World!\'\n"+
        "}";

	// Use this for initialization
	void Start () {
        OMU.Value ob = OMU.Value.FromScript(input);
        Debug.Log(OMU.Serializer.Stringify(ob.GetRawObject()));
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
