using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NS;
#if UNITY_EDITOR
using UnityEditor;
using _NS.Contingency;
using System.Linq;
#endif

namespace _NS.Contingency {
	/// <summary>
	///A Contingentable can reference other Contingables, like a list, or like a Contingency modifier
	/// </summary>
	public abstract class Contingentable : MonoBehaviour {
		public abstract int GetChildContingencyCount();
		public abstract Object GetChildContingency(int index);

		public virtual bool IsContingencyFor(Object whatToActivate) {
			for(int i = 0; i < GetChildContingencyCount(); ++i) {
				if(GetChildContingency(i) == whatToActivate) return true;
			}
			return false;
		}

		public virtual Object ContingencyRecursionCheck(List<Contingentable> stack = null) {
			if(stack == null) {
				stack = new List<Contingentable>();
			}
			if(stack.Contains(this)) return this;
			stack.Add(this);
			//Debug.Log("recursion test "+this+"     "+stack.Count);
			Object result = null;
			for(int i = 0; i < GetChildContingencyCount(); ++i) {
				Contingentable c = GetChildContingency(i) as Contingentable;
				if(c != null) {
					result = c.ContingencyRecursionCheck(stack);
					if(result != null) {
						return result;
					}
				}
			}
			if(stack[stack.Count - 1] != this) {
				throw new System.Exception("Malformed recursion stack exception");
			}
			stack.RemoveAt(stack.Count - 1);
			return null;
		}

#if UNITY_EDITOR
		public virtual Object DoGUI(Rect _position, Object obj, Component self, PropertyDrawer_ObjectPtr p) {
			return p.StandardEditorGUIObjectReference(_position, obj, self);
		}
		public virtual float CalcPropertyHeight(PropertyDrawer_ObjectPtr p) {
			return PropertyDrawer_ObjectPtr.StandardCalcPropertyHeight();
		}
#endif
	}
}
namespace NS.Contingency {
	/// <summary>
	/// A Contingency that must be triggered from a C# script, or by another Contingnecy
	/// </summary>
	public class ContingentScript : _NS.Contingency.Contingentable {
		[Tooltip("* Transform: teleport activating object to the Transform\n" +
			"* SceneAsset: load the scene\n" +
			"* AudioClip: play audio here\n" +
			"* GameObject: SetActivate(true)\n" +
			"* Material: set this Renderer's .material property, make a note of what the previous material was\n" +
			"* <any other>: activate a \'DoActivateTrigger()\' method (if available)\n" +
			"* IEnumerable: activate each element in the list")]
		public _NS.Contingency.ObjectPtr whatToActivate = new _NS.Contingency.ObjectPtr();

		public override bool IsContingencyFor(Object whatToActivate) {
			return this.whatToActivate.Data == whatToActivate;
		}

		public override int GetChildContingencyCount() { return 1; }
		public override Object GetChildContingency(int index) { return whatToActivate.Data; }

		// public ActivateOptions activateOptions = new ActivateOptions();
		public virtual void DoActivateTrigger() { DoActivateTrigger(null); }
		public virtual void DoActivateTrigger(object causedActivate) {
			NS.ActivateAnything.DoActivate(whatToActivate.Data, causedActivate, this, true);
		}

		public virtual void DoTriggerMouse() {
			NS.ActivateAnything.DoActivate(whatToActivate.Data, Input.mousePosition, this, true);
		}
	}
}
namespace _NS.Contingency {
	/// <summary>
	/// Activate a contingency when a collision happens with the Collider this is attached to
	/// </summary>
	public class ContingencyCollide : NS.Contingency.ContingentScript {
		public string onlyForObjectsTagged;
		public bool IsTriggeringObject(GameObject o) {
			return onlyForObjectsTagged == "" || o.tag == onlyForObjectsTagged || o.tag == "";
		}
		public override void DoActivateTrigger(object causedActivate) {
			GameObject go = NS.ActivateAnything.ConvertToGameObject(causedActivate);
			if(go != null && IsTriggeringObject(go)) {
				NS.ActivateAnything.DoActivate(whatToActivate, causedActivate, this, true);
			}
		}
	}

	/// Used to more easily reference Components within the Unity editor
	[System.Serializable]
	public struct ObjectPtr : NS.IReference {
		public Object data;
		public Object Data { get { return data; } set { data = value; } }
		public object Dereference() { return data; }
	}
}
#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(_NS.Contingency.ObjectPtr))]
public class PropertyDrawer_NS_Contingency_ObjectPtr : PropertyDrawer_ObjectPtr {
	protected string[] namespacesToGetDefaultSelectableClassesFrom = { "NS.Contingency.Response" };
	protected override string[] GetDefaultSelectableClassNamespace() {
		return namespacesToGetDefaultSelectableClassesFrom;
	}

	public override float GetPropertyHeight(SerializedProperty _property, GUIContent label) {
		SerializedProperty asset = ObjectPtrAsset(_property);
		Contingentable c = asset.objectReferenceValue as Contingentable;
		if(c != null) {
			return c.CalcPropertyHeight(this);
		}
		return StandardCalcPropertyHeight();
	}
	public override Object FilterDirectReferenceAdjustment(Object newObjToReverence, Object prevObj, Component self) {
		return FilterDirectReferenceAdjustment_(newObjToReverence, prevObj, self);
	}
	public static Object FilterDirectReferenceAdjustment_(Object newObjToReverence, Object prevObj, Component self) {
		Contingentable cself = self as Contingentable;
		if(newObjToReverence != null && prevObj != newObjToReverence && cself != null && cself.ContingencyRecursionCheck() != null) {
			Debug.LogWarning("Disallowing recursion of " + newObjToReverence);
			newObjToReverence = prevObj;
		}
		return newObjToReverence;
	}
	public override Object EditorGUIObjectReference(Rect _position, Object obj, Component self) {
		int oldIndent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;
		if(obj is Contingentable) {
			Contingentable c = obj as Contingentable;
			obj = c.DoGUI(_position, obj, self, this);
		} else {
			obj = StandardEditorGUIObjectReference(_position, obj, self);
		}
		EditorGUI.indentLevel = oldIndent;
		return obj;
	}

	public override Object ImmidiateObjectFilter(Object obj, Component self) {
		// if a scene asset is given... do a quick conversion TODO make a filter function...
		if(obj != null && obj.GetType() == typeof(SceneAsset)) {
			GameObject go = self.gameObject;
			if(go) {
				SceneAsset sa = obj as SceneAsset;
				NS.Contingency.Response.DoActivateSceneLoad sceneLoad = 
					go.AddComponent<NS.Contingency.Response.DoActivateSceneLoad>();
				sceneLoad.RegisterContingency(self as Contingentable);
				obj = sceneLoad;
				sceneLoad.sceneName = sa.name;
			}
		}
		return obj;
	}

	protected override Object FilterNewTypes(System.Type nextT, Component self, Component c, Object obj) {
		_NS.Contingency.Response.DoActivateBasedOnContingency doEvent =
	c as _NS.Contingency.Response.DoActivateBasedOnContingency;
		if(doEvent != null) {
			Contingentable contingencyMaster = self as Contingentable;
			if(self is NS.Contingency.ContingentScript) {
				NS.Contingency.ContingentScript cs = self as NS.Contingency.ContingentScript;
				if(cs.whatToActivate.Data is NS.Contingency.Response.ContingentList) {
					contingencyMaster = cs.whatToActivate.Data as NS.Contingency.Response.ContingentList;
				}
			}
			doEvent.RegisterContingency(contingencyMaster);
			obj = doEvent;
		}
		return obj;
	}
}
#endif