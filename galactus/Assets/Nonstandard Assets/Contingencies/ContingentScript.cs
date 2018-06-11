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
}

namespace NS.Contingency.Response {
[System.Serializable]
public class ScriptableString : ScriptableObject {
	override public string ToString() { return name; }
	public static ScriptableString Create(string text) {
		ScriptableString ss = ScriptableObject.CreateInstance<ScriptableString>();
		ss.name = text;
		return ss;
	}
	public string Text { set { this.name = value; } get { return this.name; } }
}
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(EditorGUIObjectReference))]
public class PropertyDrawer_EditorGUIObjectReference : PropertyDrawer {
	int choice = 0;
	string[] choices = new string[] {};
	Object selected = null;
	System.Type[] possibleResponses = null;
	string[] possibleResponseChoices = null;
	string[] stringEditChoices = new string[] { "<-- edit string", "null" };

	private void CleanTypename(ref string typename) {
		int lastDot = typename.LastIndexOf('.');
		if(lastDot >= 0) { typename = typename.Substring(lastDot+1); }
	}

	public override void OnGUI(Rect _position, SerializedProperty _property, GUIContent _label) {
		EditorGUI.BeginProperty(_position, GUIContent.none, _property);
		SerializedProperty asset = _property.FindPropertyRelative("data");
		_position = EditorGUI.PrefixLabel(_position, GUIUtility.GetControlID(FocusType.Passive), _label);
		Contingentable self = _property.serializedObject.targetObject as Contingentable;
		if (asset != null) {
			float originalWidth = _position.width;
			_position.width = originalWidth * 0.75f;
			if (asset.objectReferenceValue is ScriptableString) {
				ScriptableString ss = asset.objectReferenceValue as ScriptableString;
				ss.Text = EditorGUI.TextField (_position, ss.Text);
				_position.x += _position.width;
				_position.width = originalWidth * 0.25f;
				choice = EditorGUI.Popup(_position, 0, stringEditChoices);
				if (0 != choice) {
					if(asset.objectReferenceValue == null && choice > 0) {
						GameObject go = self.gameObject;//UnityEditor.Selection.activeGameObject;
						if(go) {
							Component c = go.AddComponent(possibleResponses[choice-1]);
							_NS.Contingency.Response.DoActivateBasedOnContingency doEvent = 
								c as _NS.Contingency.Response.DoActivateBasedOnContingency;
							if(c != null) {
								doEvent.RegisterContingency(self);
								asset.objectReferenceValue = doEvent;
							}
						}
					} else {
						if (stringEditChoices[choice] == "null") {
							asset.objectReferenceValue = null;
							choice = 0;
						}
					}
				}
			} else {
				asset.objectReferenceValue = EditorGUI.ObjectField (_position, asset.objectReferenceValue, typeof(Object), true);
				if (selected != asset.objectReferenceValue || selected == null) {
					selected = asset.objectReferenceValue;
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
				if (asset.objectReferenceValue != null
				  && asset.objectReferenceValue.GetType () == typeof(SceneAsset)) {
					// NS.SceneField sf = (NS.SceneField)ScriptableObject.CreateInstance<NS.SceneField>();
					// sf.SceneName = (asset.objectReferenceValue as SceneAsset).name;
					// sf.SceneAsset = asset.objectReferenceValue;
					// asset.objectReferenceValue = sf;
					// choices = new string[]{sf.SceneName};
					// choice = 0;
					GameObject go = self.gameObject;//UnityEditor.Selection.activeGameObject;
					if (go) {
						SceneAsset sa = asset.objectReferenceValue as SceneAsset;
						DoActivateSceneLoad sceneLoad = go.AddComponent<DoActivateSceneLoad> ();
						sceneLoad.RegisterContingency (self);
						asset.objectReferenceValue = sceneLoad;
						Debug.Log (sa + ")  (" + sceneLoad);
						sceneLoad.sceneName = sa.name;
					}
				}
				int lastChoice = choice;
				_position.x += _position.width;
				_position.width = originalWidth * 0.25f;
				choice = EditorGUI.Popup(_position, choice, choices);
				if (lastChoice != choice) {
					if(asset.objectReferenceValue == null && choice > 0 && self != null) {
						GameObject go = self.gameObject;//UnityEditor.Selection.activeGameObject;
						if(go) {
							System.Type nextT = possibleResponses [choice - 1];
							if (nextT.IsSubclassOf (typeof(ScriptableObject))) {
								asset.objectReferenceValue = ScriptableObject.CreateInstance (nextT);
							} else {
								Component c = go.AddComponent (nextT);
								_NS.Contingency.Response.DoActivateBasedOnContingency doEvent = 
									c as _NS.Contingency.Response.DoActivateBasedOnContingency;
								if (c != null) {
									doEvent.RegisterContingency (self);
									asset.objectReferenceValue = doEvent;
								}
							}
						}
					} else {
						if (choices[choice] == "null") {
							asset.objectReferenceValue = null;
							choice = 0;
							choices = possibleResponseChoices;
						} else {
							int index = choice-1;
							GameObject go = selected as GameObject;
							Component[] components = null;
							if(go != null) {
								components = go.GetComponents<Component> ();
							}
							if(components == null && asset.objectReferenceValue is Component && choice == 1) {
								asset.objectReferenceValue = selected;
							}
							if(components != null) {
								if(index < 0) {
									Debug.Log("selected game object");
								} else if(index < components.Length) {
									asset.objectReferenceValue = components[index];
								} else if(index >= components.Length) {
									Debug.Log("selected out of bounds");
								}
							} else {
								if(asset.objectReferenceValue != null) {
									asset.objectReferenceValue = ((Component)asset.objectReferenceValue).gameObject;
								}
							}
						}
					}
				}
			}
		}
		EditorGUI.EndProperty( );
	}
}
#endif