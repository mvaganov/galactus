using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NS.Contingency;
using NS.Contingency.Response;
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

		public virtual bool IsContingencyFor(Object obj) {
			for(int i = 0; i < GetChildContingencyCount(); ++i) {
				if(GetChildContingency(i) == obj) return true;
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
			if(stack[stack.Count-1] != this) {
				throw new System.Exception("Malformed recursion stack exception");
			}
			stack.RemoveAt(stack.Count-1);
			return null;
		}

		#if UNITY_EDITOR
		public virtual Object DoGUI(Rect _position, Object obj, Component self, PropertyDrawer_ObjectPtr p) {
			return p.StandardEditorGUIObjectReference (_position, obj, self);
		}
		public virtual float CalcPropertyHeight (PropertyDrawer_ObjectPtr p) {
			return PropertyDrawer_ObjectPtr.StandardCalcPropertyHeight();
		}
		#endif
	}
}
namespace NS.Contingency {
	/// <summary>
	/// A Contingency that must be triggered from a C# script, or by another Contingnecy
	/// </summary>
	public class ContingentScript : Contingentable {
		[Tooltip("* Transform: teleport activating object to the Transform\n"+
			"* SceneAsset: load the scene\n"+
			"* AudioClip: play audio here\n"+
			"* GameObject: SetActivate(true)\n"+
			"* Material: set this Renderer's .material property, make a note of what the previous material was\n"+
			"* <any other>: activate a \'DoActivateTrigger()\' method (if available)\n"+
			"* IEnumerable: activate each element in the list")]
		public ObjectPtr whatToActivate = new ObjectPtr();

		public override bool IsContingencyFor (Object whatToActivate) { 
			return this.whatToActivate.Data == whatToActivate;
		}

		public override int GetChildContingencyCount() {return 1;}
		public override Object GetChildContingency(int index) { return whatToActivate.Data; }

		// public ActivateOptions activateOptions = new ActivateOptions();
		public virtual void DoActivateTrigger () { DoActivateTrigger(null); }
		public virtual void DoActivateTrigger (object causedActivate) {
			NS.ActivateAnything.DoActivate (whatToActivate.Data, causedActivate, this, true);
		}

		public virtual void DoTriggerMouse() {
			NS.ActivateAnything.DoActivate (whatToActivate.Data, Input.mousePosition, this, true);
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
		public override void DoActivateTrigger (object causedActivate) {
			GameObject go = NS.ActivateAnything.ConvertToGameObject(causedActivate);
			if (go != null && IsTriggeringObject(go)) {
				NS.ActivateAnything.DoActivate (whatToActivate, causedActivate, this, true);
			}
		}
	}
}

namespace NS {
	/// Used to more easily reference Components within the Unity editor
	[System.Serializable]
	public struct ObjectPtr : IReference {
		[SerializeField]
		public Object data;
		public Object Data { get { return data as Object; } set { data = value; } }
		//public ObjectPtr(){data = null;}
		public ObjectPtr(Object obj) { data = obj; }
		public override string ToString() { return "ObjectPtr -> " + data; }
		public object Dereference() { return Data; }
	}
}


#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(NS.ObjectPtr))]
public class PropertyDrawer_ObjectPtr : PropertyDrawer {
	delegate Object SelectNextObjectFunction();
	public static bool showLabel = true;
	public int choice = 0;
	string[] choices_name = new string[] {};
	SelectNextObjectFunction[] choices_selectFunc = new SelectNextObjectFunction[] {};
	Object choicesAreFor = null;
	System.Type[] possibleResponses = null;
	string[] cached_typeCreationList_names = null;
	SelectNextObjectFunction[] cached_TypeCreationList_function = null;

	public static string setToNull = "set to null", delete = "delete";
	public static float defaultOptionWidth = 16, defaultLabelWidth = 48, unitHeight = 16;
	/// <summary>The namespaces to get default selectable classes from</summary>
	protected string[] namespacesToGetDefaultSelectableClassesFrom = { "NS.Contingency.Response" };

	public override float GetPropertyHeight (SerializedProperty _property, GUIContent label) {
		SerializedProperty asset = _property.FindPropertyRelative("data");
		Contingentable c = asset.objectReferenceValue as Contingentable;
		if (c != null) {
			return c.CalcPropertyHeight(this);
		}
		return StandardCalcPropertyHeight ();
	}
	public static float StandardCalcPropertyHeight() {
		// SerializedProperty asset = _property.FindPropertyRelative("data");
		return unitHeight;//base.GetPropertyHeight (asset, label);
	}

	/// <summary>
	/// When the ObjectPtr points to nothing, this method generates the objects that can be created by default
	/// </summary>
	/// <param name="self">Self.</param>
	/// <param name="names">Names.</param>
	/// <param name="functions">Functions.</param>
	private void GenerateTypeCreationList(Component self, out string[] names, out SelectNextObjectFunction[] functions) {
		List<string> list = new List<string>();
		List<SelectNextObjectFunction> list_of_data = new List<SelectNextObjectFunction>();
		if(namespacesToGetDefaultSelectableClassesFrom != null) {
			for(int i = 0; i < namespacesToGetDefaultSelectableClassesFrom.Length; ++i) {
				string namespaceName = namespacesToGetDefaultSelectableClassesFrom[i];
				possibleResponses = NS.Reflection.GetTypesInNamespace(namespaceName);
				list.AddRange(NS.Reflection.TypeNamesCleaned(possibleResponses, namespaceName));
				for(int t = 0; t < possibleResponses.Length; t++){
					System.Type nextT = possibleResponses[t];
					list_of_data.Add(() => {
						return CreateSelectedClass(nextT, self);
					});
				}
			}
		}
		list.Insert(0, "<-- select Object or create...");
		list_of_data.Insert(0, null);
		names = list.ToArray();
		functions = list_of_data.ToArray();
	}

	private void CleanTypename(ref string typename) {
		int lastDot = typename.LastIndexOf('.');
		if(lastDot >= 0) { typename = typename.Substring(lastDot + 1); }
	}

	private void GenerateChoicesForSelectedObject(Component self, out string[] names, out SelectNextObjectFunction[] functions) {
		List<string> components = new List<string>();
		List<SelectNextObjectFunction> nextSelectionFunc = new List<SelectNextObjectFunction>();
		string typename = choicesAreFor.GetType().ToString();
		CleanTypename(ref typename);
		components.Add(typename);
		nextSelectionFunc.Add(null);
		GameObject go = choicesAreFor as GameObject;
		bool addSetToNull = false;
		Object addDelete = null;
		if(go != null) {
			Component[] c = go.GetComponents<Component>();
			for(int i = 0; i < c.Length; i++) {
				Component comp = c[i];
				if(comp != self) {
					typename = comp.GetType().ToString();
					CleanTypename(ref typename);
					components.Add(typename);
					nextSelectionFunc.Add(() => { return comp; });
				}
			}
			addSetToNull = true;
		} else if(choicesAreFor is Component) {
			components.Add(".gameObject");
			GameObject gob = (choicesAreFor as Component).gameObject;
			nextSelectionFunc.Add(() => { return gob; });
			addSetToNull = true;
			addDelete = choicesAreFor;
		}
		if(addSetToNull) {
			components.Add(setToNull);
			nextSelectionFunc.Add(() => {
				choice = 0; return null;
			});
		}
		if(addDelete != null) {
			components.Add(delete);
			nextSelectionFunc.Add(() => {
				Object.DestroyImmediate(addDelete); choice = 0; return null;
			});
		}
		names = components.ToArray();
		functions = nextSelectionFunc.ToArray();
	}

	private Object CreateSelectedClass(System.Type nextT, Component self) {
		Object obj = null;
		if(self != null && self.gameObject != null) {
			GameObject go = self.gameObject;
			if(nextT.IsSubclassOf(typeof(ScriptableObject))) {
				obj = ScriptableObjectUtility.CreateAsset(nextT);
			} else {
				Component c = go.AddComponent(nextT);
				_NS.Contingency.Response.DoActivateBasedOnContingency doEvent =
					c as _NS.Contingency.Response.DoActivateBasedOnContingency;
				if(c != null && doEvent != null) {
					Contingentable contingencyMaster = self as Contingentable;
					if(self is ContingentScript) {
						ContingentScript cs = self as ContingentScript;
						if(cs.whatToActivate.Data is ContingentList) {
							contingencyMaster = cs.whatToActivate.Data as ContingentList;
						}
					}
					doEvent.RegisterContingency(contingencyMaster);
					obj = doEvent;
				}
			}
		}
		return obj;
	}

	public override void OnGUI(Rect _position, SerializedProperty _property, GUIContent _label) {
		EditorGUI.BeginProperty(_position, GUIContent.none, _property);
		SerializedProperty asset = _property.FindPropertyRelative("data");
		int oldIndent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;
		if(PropertyDrawer_ObjectPtr.showLabel) {
			_position = EditorGUI.PrefixLabel(_position, GUIUtility.GetControlID(FocusType.Passive), _label);
		}
		Component self = _property.serializedObject.targetObject as Component;
		if (asset != null) {
			Object prevObj = asset.objectReferenceValue;
			asset.objectReferenceValue = EditorGUIObjectReference(_position, asset.objectReferenceValue, self);
			Contingentable cself = self as Contingentable;
			if(prevObj != asset.objectReferenceValue && cself != null && cself.ContingencyRecursionCheck() != null) {
				Debug.LogWarning("Disallowing recursion of "+asset.objectReferenceValue);
				asset.objectReferenceValue = prevObj;
			}

		}
		EditorGUI.indentLevel = oldIndent;
		EditorGUI.EndProperty( );
	}

	public Object EditorGUIObjectReference(Rect _position, Object obj, Component self) {
		int oldIndent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;
		if (obj is Contingentable) {
			Contingentable c = obj as Contingentable;
			obj = c.DoGUI(_position, obj, self, this);
		} else {
			obj = StandardEditorGUIObjectReference (_position, obj, self);
		}
		EditorGUI.indentLevel = oldIndent;
		return obj;
	}

	public Object ShowChoicesPopup(Rect _position, Object obj, Component self, bool recalculateChoices) {
		// if the object needs to have it's alternate forms calculated
		if(recalculateChoices || choicesAreFor != obj || choices_name.Length == 0) {
			choicesAreFor = obj;
			// if these choices are for an actual object
			if(choicesAreFor != null) {
				GenerateChoicesForSelectedObject(self, out choices_name, out choices_selectFunc);
				choice = 0;
			} else {
				if(cached_typeCreationList_names == null) {
					GenerateTypeCreationList(self,
						out cached_typeCreationList_names, out cached_TypeCreationList_function);
				}
				choices_name = cached_typeCreationList_names;
				choices_selectFunc = cached_TypeCreationList_function;
			}
		}
		// give the alternate options for the object
		int lastChoice = choice;
		_position.x += _position.width;
		_position.width = defaultOptionWidth;
		choice = EditorGUI.Popup(_position, choice, choices_name);
		if(lastChoice != choice) {
			if(choices_selectFunc[choice] != null) {
				obj = choices_selectFunc[choice]();
			}
		}
		return obj;
	}

	public Object StandardEditorGUIObjectReference(Rect _position, Object obj, Component self) {
		float originalWidth = _position.width;
		_position.width = originalWidth - defaultOptionWidth;
		Object prevSelection = obj;
		obj = EditorGUI.ObjectField (_position, obj, typeof(Object), true);

		// if a scene asset is given... do a quick conversion TODO make a filter function...
		if (obj != null && obj.GetType () == typeof(SceneAsset)) {
			GameObject go = self.gameObject;
			if (go) {
				SceneAsset sa = obj as SceneAsset;
				DoActivateSceneLoad sceneLoad = go.AddComponent<DoActivateSceneLoad> ();
				sceneLoad.RegisterContingency (self as Contingentable);
				obj = sceneLoad;
				sceneLoad.sceneName = sa.name;
			}
		}
		return ShowChoicesPopup(_position, obj, self, obj != prevSelection);
	}

	public static Object DoGUIEnumLabeledString<T>(Rect _position, Object obj, Component self, PropertyDrawer_ObjectPtr p,
		ref T enumValue, ref string textValue) {
		int oldindent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;
		Rect r = _position;
		float w = defaultOptionWidth, wl = defaultLabelWidth;
		r.width = wl;
		enumValue = NS.Reflection.EditorGUI_EnumPopup<T>(r, enumValue);
		r.x += r.width;
		r.width = _position.width - w - wl;
		textValue = EditorGUI.TextField (r, textValue);
		obj = p.ShowChoicesPopup(r, obj, self, true);
		r.x += r.width;
		r.width = w;
		EditorGUI.indentLevel = oldindent;
		return obj;
	}
}
#endif