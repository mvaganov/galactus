using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
using NS.Contingency;
using _NS.Contingency;
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
				validTypes = GetTypesInNamespace(namespaceName);
			}
			List<string> list = TypeNamesCleaned(validTypes, namespaceName);
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
	public static List<string> TypeNamesCleaned(System.Type[] validTypes, string namespaceToClean) {
		List<string> list = new List<string>();
		for(int i = 0; i < validTypes.Length; ++i) {
			string typename = validTypes[i].ToString();
			typename = CleanFront(typename, namespaceToClean+".");
			list.Add(typename);
		}
		return list;
	}
	public static System.Type[] GetTypesInNamespace(string nameSpace, bool includeComponentTypes = false, System.Reflection.Assembly assembly = null) {
		if(assembly == null) {
			assembly = System.Reflection.Assembly.GetExecutingAssembly();
		}
		System.Type[] types = assembly.GetTypes().Where(t => 
			System.String.Equals(t.Namespace, nameSpace, System.StringComparison.Ordinal)
			&& (includeComponentTypes || !t.ToString().Contains('+'))).ToArray();
		return types;
	}
	public static string CleanFront(string str, string trimMe) {
		if(str.StartsWith(trimMe)) { return str.Substring(trimMe.Length); }
		return str;
	}
}
#endif