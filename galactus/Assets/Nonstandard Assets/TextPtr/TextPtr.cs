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
		public string SourceName { get{
				TextAsset t = data as TextAsset;
				if(t != null) return t.name;
				Component c = data as Component;
				if(c != null) return c.name;
				return null;
			}
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

	public Vector2 ContentSize() {
		GUIStyle style = GUI.skin.box;
		//style.alignment = TextAnchor.UpperLeft;
		Vector2 size = style.CalcSize(new GUIContent(objectSelected.ToString()));
		return size;
	}

	public override float GetPropertyHeight(SerializedProperty _property, GUIContent label) {
		float heightOfContent = StandardCalcPropertyHeight() * 10;
		if(objectSelected != null) {
			Vector2 size = ContentSize();
			if(size.y < heightOfContent) {
				heightOfContent = size.y;
			}
		}
		sizeOfTextArea = showText && objectSelected != null ? heightOfContent + 2 : 0;
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
	Vector2 s;
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
				r = new Rect(8, _position.y + StandardCalcPropertyHeight() + 2,
							 _position.width + _position.x - 8, sizeOfTextArea);
				NS.TextPtr.Sources.TextBody tbody = obj as NS.TextPtr.Sources.TextBody;
				string text = obj.ToString();
				// it might be nice to make this a scrollable TextArea, but there's probably some weird GUI wrestling to do because this is a PropertyDrawer, not a complete Component.
				if(tbody != null) {
					//tbody.text = EditorGUILayout.TextArea(text, GUILayout.ExpandHeight(true));
					tbody.text = EditorGUI.TextArea(r, text);
				} else {
					EditorGUI.BeginDisabledGroup(true);
					//EditorGUILayout.TextArea(text, GUILayout.ExpandHeight(true));
					EditorGUI.TextArea(r, text);
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