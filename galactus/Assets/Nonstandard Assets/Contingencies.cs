using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

// author: mvaganov@hotmail.com
// license: Copyfree, public domain. This is free code!
namespace NS {
	// TODO add ContingentOnKey, ContingentOnKeyDown, ContingentOnKeyUp, 
	// TODO serialized boolean callbacks to allow ContingentOnCondition
	public class Contingencies : MonoBehaviour {
		#if UNITY_EDITOR
		public ContingencyChoice contingencyToAdd; // empty structure, only for editor UI purposes
		#endif
	}
}
#if UNITY_EDITOR
/// this class is empty, it only exists to enable UI code in the UnityEditor
[System.Serializable] public class ContingencyChoice {}

[CustomPropertyDrawer(typeof(ContingencyChoice))]
public class PropertyDrawer_ContingencyChoice : PropertyDrawer {
	int choice = 0;
	string[] choices = null;
	System.Type[] validTypes = null;
	public override void OnGUI(Rect _position, SerializedProperty _property, GUIContent _label) {
		if(choices == null) {
			const string namespaceName = "NS.Contingency";
			if(validTypes == null) {
				validTypes = NS.Reflection.GetTypesInNamespace(namespaceName);
			}
			List<string> list = NS.Reflection.TypeNamesCleaned(validTypes, namespaceName);
			list.Insert(0, "<select new contingency>");
			choices = list.ToArray();
		}
		EditorGUI.BeginProperty(_position, GUIContent.none, _property);
		_position = EditorGUI.PrefixLabel(_position, GUIUtility.GetControlID(FocusType.Passive), _label);
		int lastChoice = choice;
		choice = EditorGUI.Popup(_position, choice, choices);
		if (lastChoice != choice) {
			GameObject go = UnityEditor.Selection.activeGameObject;
			if(go) {
				go.AddComponent(validTypes[choice-1]);
			}
		}
		EditorGUI.EndProperty( );
	}
}
#endif