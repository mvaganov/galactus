using UnityEngine;
using System.Collections;

namespace BT {
	public class Instantiate : Behavior {
		public string prefabVarName;
		public string locationVarName;

		override public Status Execute (BTOwner who) {
			Object prefab;// = who.variables.GetProperty(prefabVarName) as Object;
			object obj;
			if(!OMU.Data.TryDeReferenceGet(who.variables, prefabVarName, out obj)){
				Debug.LogWarning(who+" has no "+prefabVarName+" variable");
			}
			prefab = obj as Object;
			Spatial.Locatable location = who.GetLocation (locationVarName);
			GameObject go = MonoBehaviour.Instantiate(prefab, location.GetLocation(), Quaternion.identity) as GameObject;
			if(go == null) {
				return Status.error;
			}
			return Status.success;
		}
		//override public bool HasState(){return false;}
	}
}