using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NS.TextPtr {
	/// Used to more easily reference Components within the Unity editor
	[System.Serializable]
	public struct ObjectPtr : NS.IReference {
		public Object data;
		public Object Data { get { return data; } set { data = value; } }
		public object Dereference() { return data; }
		public override string ToString() {
			// UnityEngine.TextAsset, TextBody
			if(data != null) { return data.ToString(); }
			return null;
		}
	}
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(NS.TextPtr.ObjectPtr))]
public class PropertyDrawer_NS_TextPtr_ObjectPtr : PropertyDrawer_ObjectPtr {

	private float sizeOfTextArea = 0;
	private bool showText;
	private int buttonWidth = 16;
	private object objectSelected;

	protected string[] namespacesToGetDefaultSelectableClassesFrom = { "NS.TextPtr.Sources" };
	protected override string[] GetNamespacesForNewComponentOptions() {
		return namespacesToGetDefaultSelectableClassesFrom;
	}

	public override float GetPropertyHeight(SerializedProperty _property, GUIContent label) {
		float heightOfContent = StandardCalcPropertyHeight() * 10;
		if(objectSelected != null) {
			GUIStyle style = GUI.skin.box;
			style.alignment = TextAnchor.UpperLeft;
			Vector3 size = style.CalcSize(new GUIContent(objectSelected.ToString()));
			if(size.y < heightOfContent){
				heightOfContent = size.y;
			}
		}
		sizeOfTextArea = showText && objectSelected != null ? heightOfContent : 0;
		return StandardCalcPropertyHeight() + sizeOfTextArea;
	}

	public override Object FilterImmidiate(Object obj, Component self) {
		return obj;
	}

	protected override Object FilterNewComponent(System.Type nextT, Component self, Component newlyCreatedComponent) {
		return newlyCreatedComponent;
	}

	public override Object FilterFinal(Object newObjToReference, Object prevObj, Component self) {
		return newObjToReference;
	}

	public override Object EditorGUIObjectReference(Rect _position, Object obj, Component self) {
		int oldIndent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;
		// TODO button to show text. when clicked, it toggles a TextArea below, which changes the height.

		Rect r = _position;
		r.width = buttonWidth;
		showText = EditorGUI.Toggle(r, showText);
		r = _position;
		r.width -= buttonWidth;
		r.x += buttonWidth;
		r.height = StandardCalcPropertyHeight();
		obj = StandardEditorGUIObjectReference(r, obj, self);
		if(sizeOfTextArea > 0) {
			if(obj != null) {
				r = new Rect(8, _position.y + StandardCalcPropertyHeight(), 
				             _position.width + _position.x - 8, sizeOfTextArea);
				NS.TextPtr.Sources.TextBody tbody = obj as NS.TextPtr.Sources.TextBody;
				// TODO make this a scrollable TextArea using EditorGUILayout
				if(tbody != null) {
					tbody.text = EditorGUI.TextArea(r, obj.ToString());
				} else {
					EditorGUI.BeginDisabledGroup(true);
					EditorGUI.TextArea(r, obj.ToString());
					EditorGUI.EndDisabledGroup();
				}
			}
		}
		EditorGUI.indentLevel = oldIndent;
		objectSelected = obj;
		return obj;
	}
}
#endif