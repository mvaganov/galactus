using UnityEngine;
using System.Collections;

public class NameGen : MonoBehaviour {
	public static string[] namePrefix = { "","","","","","","","the","mr.","the","mr.","mrs.","sir.","ms.","lady","my","ur","sh","","","" };
	public static string[] nameFragments = { "","butt","poop","troll","lol","noob","dude","swag","super","haxor","red","green","blue","lady","leet","harambe","space","dank","trump","squanch" };
	public static string[] nameSuffix = { "","","","","","","","ed","ly","dude","man","lady","guy","TheGreat" };
	public static string RandomName() {
		return RandomName(namePrefix, nameFragments, nameSuffix);
	}
	public static string RandomName(string[] namePrefix, string[] nameFragments, string[] nameSuffix) {
		string n = "";
		do {
			n = namePrefix[Random.Range(0, namePrefix.Length)];
			n += nameFragments[Random.Range(0, nameFragments.Length)];
			if(Random.Range(0, 2) == 0)
				n += nameFragments[Random.Range(0, nameFragments.Length)];
			n += nameSuffix[Random.Range(0, nameSuffix.Length)];
		} while (n.Length == 0);
		return n;
	}


	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
