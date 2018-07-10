using UnityEngine;
using System.Collections;

namespace BT {
	public class RandomLocationFromBox : Behavior {
		public string boxName;
		public string var;

		override public Status Execute (BTOwner who) {
			// TODO make this work for non-axis aligned boxes too!
			object obj;
			OMU.Data.TryDeReferenceGet(who.variables, boxName, out obj);
			BoxCollider box = null;
//Debug.Log ("found \""+obj+"\" named \""+boxName+"\"");
			if(obj is GameObject) {
				GameObject go = (obj as GameObject);
				if(go.transform.rotation == Quaternion.identity)
					box = go.GetComponent<BoxCollider>();
			}
			if(box == null){
				throw new System.Exception("only know how to get boxes from un-rotated objects with box colliders");
			}
			Vector3 area = box.size;
			area.x *= box.transform.lossyScale.x;
			area.y *= box.transform.lossyScale.y;
			area.z *= box.transform.lossyScale.z;
			Vector3 p = new Vector3 (
				Random.Range (0, area.x),
				Random.Range (0, area.y),
				Random.Range (0, area.z));
			Vector3 offset = area / 2;//new Vector3 (area.x/2,area.y/2,area.z/2);
			Vector3 location = box.transform.position - offset + p;
			who.variables.Add (var, location);
			return Status.success;
		}
		//override public bool HasState(){return false;}
	}
}