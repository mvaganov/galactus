using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NS.Contingency.Response {
	public class DoCreateInstance : _NS.Contingency.Response.DoActivateBasedOnContingency {
		public Object whatToInstantiate;
		public enum CreationSettings {
			allowMultiples,
			onlyEverCreateOne
		}
		public CreationSettings creationSettings = CreationSettings.allowMultiples;
		private List<Object> created = null;
		public void DoActivateTrigger () {
			if(created == null) { created = new List<Object>(); }
			if(creationSettings != CreationSettings.onlyEverCreateOne 
			|| created.Count == 0) {
				Object o = Instantiate(whatToInstantiate, transform.position, transform.rotation);
				created.Add(o);
			}
		}
		public void DoDeactivateTrigger () {
			if(created != null) {
				int lastIndex = created.Count-1;
				Destroy(created[lastIndex]);
				created.RemoveAt(lastIndex);
			}
		}
	}
}