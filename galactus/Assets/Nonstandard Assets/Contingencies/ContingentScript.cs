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
		public abstract bool IsContingencyFor(Object whatToActivate);
		#if UNITY_EDITOR
		public virtual Object DoGUI(Rect _position, Object obj, _NS.Contingency.Contingentable self, PropertyDrawer_EditorGUIObjectReference p) {
			return p.StandardEditorGUIObjectReference (_position, obj, self);
		}
		public virtual float GetPropertyHeight (SerializedProperty _property, GUIContent label, PropertyDrawer_EditorGUIObjectReference p) {
			return p.StandardGetPropertyHeight(_property, label);
		}
		#endif
	}
}
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

		public override bool IsContingencyFor (Object whatToActivate) { return this.whatToActivate.data == whatToActivate; }

		[System.Serializable]
		public struct ActivateOptions {
			public float delayInSeconds;
			[Tooltip("If true, *deactivate* whatToActivate instead. May not be valid for all activatable Objects")]
			public bool deactivate;
		}
		public ActivateOptions activateOptions = new ActivateOptions();
		public virtual void DoActivateTrigger () { DoActivateTrigger(null); }
		public virtual void DoActivateTrigger (object causedActivate) {
			NS.F.DoActivate (whatToActivate.data, causedActivate, this, !activateOptions.deactivate, activateOptions.delayInSeconds);
		}
		public virtual void DoTriggerMouse() {
			NS.F.DoActivate (whatToActivate.data, Input.mousePosition, this, !activateOptions.deactivate);
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
				NS.F.DoActivate (whatToActivate, causedActivate, this, !activateOptions.deactivate, activateOptions.delayInSeconds);
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

// namespace NS.Contingency.Response {
// 	[System.Serializable]
// 	public class ScriptableString : ScriptableObject {
// 		public string text;
// 		override public string ToString() { return name; }
// 		public static ScriptableString Create(string text) {
// 			ScriptableString ss = null;
// 			#if UNITY_EDITOR
// 			ss = ScriptableObjectUtility.CreateAsset<ScriptableString>();
// 			#else
// 			ss = ScriptableObject.CreateInstance<ScriptableString>();
// 			ss.text = text;
// 			#endif
// 			return ss;
// 		}
// 		public string Text { set { this.text = value; } get { return this.text; } }
// 	}
// 	// [System.Serializable]
// 	// public class ScriptableList : ScriptableObject {
// 	// 	public List<EditorGUIObjectReference> elements = new List<EditorGUIObjectReference>();
// 	// }
// }

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(EditorGUIObjectReference))]
public class PropertyDrawer_EditorGUIObjectReference : PropertyDrawer {
	public int choice = 0;
	string[] choices = new string[] {};
	Object selected = null;
	System.Type[] possibleResponses = null;
	string[] possibleResponseChoices = null;
	public static string[] editChoiceOrNullify = new string[] { "<-- edit", "null" };

	private void CleanTypename(ref string typename) {
		int lastDot = typename.LastIndexOf('.');
		if(lastDot >= 0) { typename = typename.Substring(lastDot+1); }
	}

	public override float GetPropertyHeight (SerializedProperty _property, GUIContent label) {
		SerializedProperty asset = _property.FindPropertyRelative("data");
		Contingentable c = asset.objectReferenceValue as Contingentable;
		if (c != null) {
			return c.GetPropertyHeight(_property, label, this);
			// SerializedObject childObj = new SerializedObject (sl);
			// SerializedProperty prop = childObj.FindProperty("elements");
			// return EditorGUI.GetPropertyHeight (prop);
		}
		return StandardGetPropertyHeight (asset, label);
	}
	public float StandardGetPropertyHeight(SerializedProperty _property, GUIContent label) {
		SerializedProperty asset = _property.FindPropertyRelative("data");
		return base.GetPropertyHeight (asset, label);
	}

	public static float defaultOptionWidth = 16, defaultLabelWidth = 100, unitHeight = 16;

	public override void OnGUI(Rect _position, SerializedProperty _property, GUIContent _label) {
		EditorGUI.BeginProperty(_position, GUIContent.none, _property);
		SerializedProperty asset = _property.FindPropertyRelative("data");
		//_position = EditorGUI.PrefixLabel(_position, GUIUtility.GetControlID(FocusType.Passive), _label);
		Contingentable self = _property.serializedObject.targetObject as Contingentable;
		if (asset != null) {
			asset.objectReferenceValue = EditorGUIObjectReference(_position, asset.objectReferenceValue, self);
		}
		EditorGUI.EndProperty( );
	}

	public static T EditorGUI_EnumPopup<T>(T value, Rect _position) {
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
		obj = EditorGUI.ObjectField (_position, obj, typeof(Object), true);
		if (selected != obj || selected == null) {
			selected = obj;
			if (selected != null) {
				List<string> components = new List<string> ();
				// if(selected is NS.SceneField) {
				// 	components.Add(((NS.SceneField)selected).SceneName);
				// } else {
				string typename = selected.GetType ().ToString ();
				CleanTypename (ref typename);
				components.Add (typename);
				GameObject go = selected as GameObject;
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
					components.Add ("null");
				} else if (selected is Component) {
					components.Add (".gameObject");
					components.Add ("null");
				}
				// }
				choices = components.ToArray ();
				choice = 0;
			} else {
				if (possibleResponseChoices == null) {
					string namespaceName = "NS.Contingency.Response";
					possibleResponses = PropertyDrawer_ContingencyChoice.GetTypesInNamespace (namespaceName);
					List<string> list = PropertyDrawer_ContingencyChoice.TypeNamesCleaned (possibleResponses, namespaceName);
					list.Insert (0, "<-- select Object or create...");
					possibleResponseChoices = list.ToArray ();
				}
				choices = possibleResponseChoices;
			}
		}
		// if a scene asset is given...
		if (obj != null
			&& obj.GetType () == typeof(SceneAsset)) {
			// NS.SceneField sf = (NS.SceneField)ScriptableObject.CreateInstance<NS.SceneField>();
			// sf.SceneName = (asset.objectReferenceValue as SceneAsset).name;
			// sf.SceneAsset = asset.objectReferenceValue;
			// asset.objectReferenceValue = sf;
			// choices = new string[]{sf.SceneName};
			// choice = 0;
			GameObject go = self.gameObject;//UnityEditor.Selection.activeGameObject;
			if (go) {
				SceneAsset sa = obj as SceneAsset;
				DoActivateSceneLoad sceneLoad = go.AddComponent<DoActivateSceneLoad> ();
				sceneLoad.RegisterContingency (self);
				obj = sceneLoad;
				Debug.Log (sa + ")  (" + sceneLoad);
				sceneLoad.sceneName = sa.name;
			}
		}
		int lastChoice = choice;
		_position.x += _position.width;
		_position.width = defaultOptionWidth;
		choice = EditorGUI.Popup(_position, choice, choices);
		if (lastChoice != choice) {
			if(obj == null && choice > 0 && self != null) {
				GameObject go = self.gameObject;//UnityEditor.Selection.activeGameObject;
				if(go) {
					System.Type nextT = possibleResponses [choice - 1];
					if (nextT.IsSubclassOf (typeof(ScriptableObject))) {
						obj = ScriptableObjectUtility.CreateAsset(nextT);
						//obj = ScriptableObject.CreateInstance (nextT);
					} else {
						Component c = go.AddComponent (nextT);
						_NS.Contingency.Response.DoActivateBasedOnContingency doEvent = 
							c as _NS.Contingency.Response.DoActivateBasedOnContingency;
						if (c != null) {
							doEvent.RegisterContingency (self);
							obj = doEvent;
						}
					}
				}
			} else {
				if (choices[choice] == "null") {
					obj = null;
					choice = 0;
					choices = possibleResponseChoices;
				} else {
					int index = choice-1;
					GameObject go = selected as GameObject;
					Component[] components = null;
					if(go != null) {
						components = go.GetComponents<Component> ();
					}
					if(components == null && obj is Component && choice == 1) {
						// TODO prevent recursion too...
						if(selected != self) {
							obj = selected;
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
		wl = PropertyDrawer_EditorGUIObjectReference.defaultLabelWidth,
		h = PropertyDrawer_EditorGUIObjectReference.unitHeight;
		r.width = wl;
		enumValue = EditorGUI_EnumPopup<T>(enumValue, r);
		r.x += r.width;
		r.width = _position.width - w - wl;
		textValue = EditorGUI.TextField (r, textValue);
		r.x += r.width;
		r.width = w;
		p.choice = EditorGUI.Popup(r, 0, options);
		if (0 != p.choice && options[p.choice] == "null") {
			obj = null;
			p.choice = 0;
		}
		return obj;
	}
}
#endif