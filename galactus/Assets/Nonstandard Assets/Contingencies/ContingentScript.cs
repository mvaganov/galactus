using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NS.Contingency;
using NS.Contingency.Response;
#if UNITY_EDITOR
using UnityEditor;
using _NS.Contingency;
#endif

namespace _NS.Contingency {
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
		public virtual Object DoGUI(Rect _position, Object obj, _NS.Contingency.Contingentable self, PropertyDrawer_EditorGUIObjectReference p) {
			return p.StandardEditorGUIObjectReference (_position, obj, self);
		}
		public virtual float CalcPropertyHeight (PropertyDrawer_EditorGUIObjectReference p) {
			return p.StandardCalcPropertyHeight();
		}
		#endif
	}
}
// TODO rename namespace to Z.
namespace NS.Contingency {
	public class ContingentScript : Contingentable {
		[Tooltip("* Transform: teleport activating object to the Transform\n"+
			"* SceneAsset: load the scene\n"+
			"* AudioClip: play audio here\n"+
			"* GameObject: SetActivate(true)\n"+
			"* Material: set this Renderer's .material property, make a note of what the previous material was\n"+
			"* <any other>: activate a \'DoActivateTrigger()\' method (if available)\n"+
			"* IEnumerable: activate each element in the list")]
		public EditorGUIObjectReference whatToActivate = new EditorGUIObjectReference();

		public override bool IsContingencyFor (Object whatToActivate) { 
			return this.whatToActivate.data == whatToActivate;
		}

		public override int GetChildContingencyCount() {return 1;}
		public override Object GetChildContingency(int index) { return whatToActivate.data; }


		// [System.Serializable]
		// public struct ActivateOptions {
		// 	public float delayInSeconds;
		// 	[Tooltip("If true, *deactivate* whatToActivate instead. May not be valid for all activatable Objects")]
		// 	public bool deactivate;
		// }
		// public ActivateOptions activateOptions = new ActivateOptions();
		public virtual void DoActivateTrigger () { DoActivateTrigger(null); }
		public virtual void DoActivateTrigger (object causedActivate) {
			NS.F.DoActivate (whatToActivate.data, causedActivate, this, true);
		}
		public virtual void DoTriggerMouse() {
			NS.F.DoActivate (whatToActivate.data, Input.mousePosition, this, true);
		}
	}
}
namespace _NS.Contingency {
	public class ContingencyCollide : NS.Contingency.ContingentScript {
		public string onlyForObjectsTagged;
		public bool IsTriggeringObject(GameObject o) {
			return onlyForObjectsTagged == "" || o.tag == onlyForObjectsTagged || o.tag == "";
		}
		public override void DoActivateTrigger (object causedActivate) {
			GameObject go = NS.F.ConvertToGameObject(causedActivate);
			if (go != null && IsTriggeringObject(go)) {
				NS.F.DoActivate (whatToActivate, causedActivate, this, true);
			}
		}
	}
}

/// Used to more easily reference Components within the Unity editor
[System.Serializable] public struct EditorGUIObjectReference {
	public Object data;
	//public EditorGUIObjectReference(){data = null;}
	public EditorGUIObjectReference(Object obj){ data = obj; }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(EditorGUIObjectReference))]
public class PropertyDrawer_EditorGUIObjectReference : PropertyDrawer {
	public static bool showLabel = true;
	public int choice = 0;
	string[] choices = new string[] {};
	Object choicesAreFor = null;
	System.Type[] possibleResponses = null;
	string[] defaultChoicesIfNullObject = null;
	public static string setToNull = "set to null", delete = "delete";
	public static string[] editChoiceOrNullify = new string[] { "<-- edit", setToNull, delete };

	private void CleanTypename(ref string typename) {
		int lastDot = typename.LastIndexOf('.');
		if(lastDot >= 0) { typename = typename.Substring(lastDot+1); }
	}

	public override float GetPropertyHeight (SerializedProperty _property, GUIContent label) {
		SerializedProperty asset = _property.FindPropertyRelative("data");
		Contingentable c = asset.objectReferenceValue as Contingentable;
		if (c != null) {
			return c.CalcPropertyHeight(this);
		}
		return StandardCalcPropertyHeight ();
	}
	public float StandardCalcPropertyHeight() {
		// SerializedProperty asset = _property.FindPropertyRelative("data");
		return unitHeight+2;//base.GetPropertyHeight (asset, label);
	}

	public static float defaultOptionWidth = 16, defaultLabelWidth = 100, unitHeight = 16;

	public override void OnGUI(Rect _position, SerializedProperty _property, GUIContent _label) {
		EditorGUI.BeginProperty(_position, GUIContent.none, _property);
		SerializedProperty asset = _property.FindPropertyRelative("data");
		if(PropertyDrawer_EditorGUIObjectReference.showLabel) {
			_position = EditorGUI.PrefixLabel(_position, GUIUtility.GetControlID(FocusType.Passive), _label);
		}
		Contingentable self = _property.serializedObject.targetObject as Contingentable;
		if (asset != null) {
			Object prevObj = asset.objectReferenceValue;
			asset.objectReferenceValue = EditorGUIObjectReference(_position, asset.objectReferenceValue, self);
			if(prevObj != asset.objectReferenceValue && self.ContingencyRecursionCheck() != null) {
				Debug.LogWarning("Disallowing recursion of "+asset.objectReferenceValue);
				asset.objectReferenceValue = prevObj;
			}

		}
		EditorGUI.EndProperty( );
	}

	public static T EditorGUI_EnumPopup<T>(Rect _position, T value) {
		System.Type t = typeof(T);
		if(t.IsEnum) {
			string[] names = System.Enum.GetNames(t);
			string thisone = value.ToString();
			int index = System.Array.IndexOf(names, thisone);
			index = EditorGUI.Popup(_position, index, names);
			value = (T)System.Enum.Parse(t, names[index]);
		}
		return value;
	}
	public Object EditorGUIObjectReference(Rect _position, Object obj, Contingentable self) {
		if (obj is Contingentable) {
			Contingentable c = obj as Contingentable;
			obj = c.DoGUI(_position, obj, self, this);
		} else {
			obj = StandardEditorGUIObjectReference (_position, obj, self);
		}
		return obj;
	}

	public Object StandardEditorGUIObjectReference(Rect _position, Object obj, Contingentable self) {
		float originalWidth = _position.width;
		_position.width = originalWidth - defaultOptionWidth;
		Object prevSelection = obj;
		obj = EditorGUI.ObjectField (_position, obj, typeof(Object), true);

		// if a scene asset is given... do a quick conversion
		if (obj != null && obj.GetType () == typeof(SceneAsset)) {
			GameObject go = self.gameObject;
			if (go) {
				SceneAsset sa = obj as SceneAsset;
				DoActivateSceneLoad sceneLoad = go.AddComponent<DoActivateSceneLoad> ();
				sceneLoad.RegisterContingency (self);
				obj = sceneLoad;
				sceneLoad.sceneName = sa.name;
			}
		}

		// if the object needs to have it's alternate forms calculated
		if (obj != prevSelection || choicesAreFor != obj || choices.Length == 0) {
			// check if recursion is happening... if so, bail!
			// if(obj == self || obj  == self) {
			// 	Debug.LogWarning("preventing recursion...");
			// 	return prevSelection;
			// }

			choicesAreFor = obj;
			// if these choices are for an actual object
			if (choicesAreFor != null) {
				List<string> components = new List<string> ();
				string typename = choicesAreFor.GetType ().ToString ();
				CleanTypename (ref typename);
				components.Add (typename);
				GameObject go = choicesAreFor as GameObject;
				if (go != null) {
					Component[] c = go.GetComponents<Component> ();
					for (int i = 0; i < c.Length; i++) {
						typename = c [i].GetType ().ToString ();
						CleanTypename (ref typename);
						if(c[i] == self) {
							typename = "<--";
						}
						components.Add (typename);
					}
					components.Add (setToNull);
				} else if (choicesAreFor is Component) {
					components.Add (".gameObject");
					components.Add (setToNull);
				}
				choices = components.ToArray ();
				choice = 0;
			} else {
				if (defaultChoicesIfNullObject == null) {
					string namespaceName = "NS.Contingency.Response";
					possibleResponses = PropertyDrawer_ContingencyChoice.GetTypesInNamespace (namespaceName);
					List<string> list = PropertyDrawer_ContingencyChoice.TypeNamesCleaned (possibleResponses, namespaceName);
					list.Insert (0, "<-- select Object or create...");
					defaultChoicesIfNullObject = list.ToArray ();
				}
				choices = defaultChoicesIfNullObject;
			}
		}
		// give the alternate options for the object
		int lastChoice = choice;
		_position.x += _position.width;
		_position.width = defaultOptionWidth;
		choice = EditorGUI.Popup(_position, choice, choices);
		if (lastChoice != choice) {
			if(obj == null && choice > 0 && self != null && self.gameObject != null) {
				GameObject go = self.gameObject;
				System.Type nextT = possibleResponses [choice - 1];
				if (nextT.IsSubclassOf (typeof(ScriptableObject))) {
					obj = ScriptableObjectUtility.CreateAsset(nextT);
				} else {
					Component c = go.AddComponent (nextT);
					_NS.Contingency.Response.DoActivateBasedOnContingency doEvent = 
						c as _NS.Contingency.Response.DoActivateBasedOnContingency;
					if (c != null) {
						Contingentable contingencyMaster = self;
						if(self is ContingentScript){
							ContingentScript cs = self as ContingentScript;
							if(cs.whatToActivate.data is ContingentList) {
								contingencyMaster = cs.whatToActivate.data as ContingentList;
							}
						}
						doEvent.RegisterContingency (contingencyMaster);
						obj = doEvent;
					}
				}
			} else {
				if (choices[choice] == setToNull) {
					obj = null;
					choice = 0;
					choices = defaultChoicesIfNullObject;
				} else {
					int index = choice-1;
					GameObject go = choicesAreFor as GameObject;
					Component[] components = null;
					if(go != null) {
						components = go.GetComponents<Component> ();
					}
					if(components == null && obj is Component && choice == 1) {
						// TODO prevent recursion too...
						if(prevSelection != self) {
							Debug.LogWarning("prevent recursion");
							obj = prevSelection;
						}
					}
					if(components != null) {
						if(index < 0) {
							Debug.Log("selected game object");
						} else if(index < components.Length) {
							obj = components[index];
						} else if(index >= components.Length) {
							Debug.Log("selected out of bounds");
						}
					} else {
						if(obj != null) {
							obj = ((Component)obj).gameObject;
						}
					}
				}
			}
		}
		return obj;
	}

	public static Object DoGUIEnumLabeledString<T>(Rect _position, Object obj, _NS.Contingency.Contingentable self, PropertyDrawer_EditorGUIObjectReference p,
		ref T enumValue, ref string textValue, string[] options = null) {
		if(options == null) {
			options = editChoiceOrNullify;
		}
		Rect r = _position;
		float w = PropertyDrawer_EditorGUIObjectReference.defaultOptionWidth, 
		wl = PropertyDrawer_EditorGUIObjectReference.defaultLabelWidth;
		r.width = wl;
		enumValue = EditorGUI_EnumPopup<T>(r, enumValue);
		r.x += r.width;
		r.width = _position.width - w - wl;
		textValue = EditorGUI.TextField (r, textValue);
		r.x += r.width;
		r.width = w;
		StandardOptionPopup(r, ref obj);
		p.choice = EditorGUI.Popup(r, 0, options);
		if (0 != p.choice) {
			if(options[p.choice] == setToNull) {
				obj = null;
				p.choice = 0;
			} else if(options[p.choice] == delete) {
				// Debug.Log("delete "+obj);
				GameObject.DestroyImmediate(obj);
				obj = null;
				p.choice = 0;
			}
		}
		return obj;
	}

	public static int StandardOptionPopup(Rect r, ref Object obj) {
		int choice = EditorGUI.Popup(r, 0, PropertyDrawer_EditorGUIObjectReference.editChoiceOrNullify);
		if (0 != choice){
			if(PropertyDrawer_EditorGUIObjectReference.editChoiceOrNullify[choice] == PropertyDrawer_EditorGUIObjectReference.setToNull) {
				obj = null;
				choice = 0;
			} else if(PropertyDrawer_EditorGUIObjectReference.editChoiceOrNullify[choice] == PropertyDrawer_EditorGUIObjectReference.delete) {
				GameObject.DestroyImmediate(obj);
				obj = null;
				choice = 0;
			}
		}
		return choice;
	}
}
#endif