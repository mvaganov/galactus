using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CmdTest : MonoBehaviour {
	[TextArea(3,10)]
	public string doThis;
	// Use this for initialization
	void Start () {
		CmdLine.DoCommand(doThis);
	}
}
