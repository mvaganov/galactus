using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NS.Contingency.Response;
using NS.Contingency;
#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
#endif

namespace _NS.Contingency.Response {
	public class DoActivateBasedOnContingency : MonoBehaviour {
		#if UNITY_EDITOR
		public EditorGUI_BasedOnContingency pleaseConnectContingency = new EditorGUI_BasedOnContingency();
		#endif
		public void RegisterContingency(_NS.Contingency.Contingentable c) {
			#if UNITY_EDITOR
			pleaseConnectContingency.data = c;
			#endif
		}
	}
}
#if UNITY_EDITOR
[System.Serializable] public class EditorGUI_BasedOnContingency {
	public _NS.Contingency.Contingentable data;
}

[CustomPropertyDrawer(typeof(EditorGUI_BasedOnContingency))]
public class PropertyDrawer_EditorGUI_BasedOnContingency : PropertyDrawer {

	public override void OnGUI(Rect _position, SerializedProperty _property, GUIContent _label) {
		EditorGUI.BeginProperty(_position, GUIContent.none, _property);
		SerializedProperty asset = _property.FindPropertyRelative("data");
		if (asset != null) {
			_NS.Contingency.Contingentable data = asset.objectReferenceValue as _NS.Contingency.Contingentable;
			_NS.Contingency.Response.DoActivateBasedOnContingency self = 
				_property.serializedObject.targetObject as _NS.Contingency.Response.DoActivateBasedOnContingency;
			if(data == null || !data.IsContingencyFor(self)) {
				_position = EditorGUI.PrefixLabel(_position, GUIUtility.GetControlID(FocusType.Passive), _label);
				asset.objectReferenceValue = EditorGUI.ObjectField(
				_position, asset.objectReferenceValue, 
				typeof(_NS.Contingency.Contingentable), true) as _NS.Contingency.Contingentable;
			} else {
				EditorGUI.LabelField(_position, "Properly connected to a contingency script.");
			}
		}
		EditorGUI.EndProperty( );
	}
}
#endif