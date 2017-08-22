using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestListUI : ListUI {
	public class TC_ { public string word; public int n; public TC_(string str) { word = str; n = str.Length; } 
	public override string ToString(){return "{"+word+":"+n+"}";}}
	public class TC {
		public string name, description; public TC(){name=RandomString(1);description=RandomString(10);}
		public List<TC_> words = new List<TC_>();
		public static string RandomString(int length) {
			string s=""; for(int i=0;i<length;++i){s+=(char)(((int)'a')+Random.Range(0,26));}return s;
		}
	}
	void Start() {
		// OMU.Test t = new OMU.Test();
		// t.TestScript();
		List<TC> thingies = new List<TC>();
		for(int i =0;i<15;++i) { thingies.Add(new TC()); (thingies[i] as TC).name += i; }
		(thingies[2] as TC).words.Add(new TC_("Hello"));
		(thingies[2] as TC).words.Add(new TC_("World!"));
		// table.columnRules = ColumnRule.GenerateFor(typeof(TC));
		Set(thingies);
	}
}
