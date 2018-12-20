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
		public override int GetChildContingencyCount() { return 1; }
		public override Object GetChildContingency(int index) { return whatToInstantiate; }
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
#if UNITY_EDITOR
		public override Object DoGUI(Rect _position, Object obj, Component self, PropertyDrawer_ObjectPtr p) {
			Object selected = whatToInstantiate;
			obj = p.DoGUIEnumLabeledObject(_position, obj, self, ref creationSettings, ref selected);
			whatToInstantiate = selected;
			return obj;
		}
#endif
	}
}