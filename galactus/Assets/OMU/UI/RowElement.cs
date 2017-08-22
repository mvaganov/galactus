using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RowElement : MonoBehaviour, R3.Reusable, ListUI.HasDirtyFlag {

	protected bool isDirty = false;public void SetDirty(bool dirty){
		isDirty=dirty;
		if(dirty && columnElements != null) System.Array.ForEach(columnElements, (e)=>e.SetDirty(dirty));
	}
	public bool IsDirty(){return isDirty;}
	[SerializeField]
	private int indexOfSubject;
	public ColumnElement[] columnElements;
	[SerializeField]
	private string uiSignature;
	public int Index{ get{return indexOfSubject;} }
	public ListUI manager;
	public object Subject { get{return manager.GetObjects().GetValue(indexOfSubject);} }
	public bool Equals(RowElement o) { return o.manager==manager&&o.indexOfSubject==indexOfSubject; }
	public bool IsUIReady(){return GetComponent<CanvasRenderer>() != null;}
	public void Set(ListUI manager, int indexOfSubject){
		if(!IsUIReady()){InitializeUI();}
		if(this.manager == manager && this.indexOfSubject == indexOfSubject
		&& transform.parent == manager.scrollView.content.transform) return;
		this.manager = manager;this.indexOfSubject = indexOfSubject; SetDirty(true);
		transform.SetParent(manager.scrollView.content.transform);
	}
	public static RowElement Create(ListUI uilist, int index) {
		GameObject go = new GameObject("|");
		RowElement ve = go.AddComponent<RowElement>();
		ve.Set(uilist, index);
		return ve;
	}
	public void InitializeUI(){
		RectTransform r = gameObject.AddComponent<RectTransform>();
		gameObject.AddComponent<CanvasRenderer>();
		UGUI.UPPERLEFT_ANCHOR(r);
	}
	public void Recycle() {
		R3.AddRange(columnElements); columnElements = null;
		transform.SetParent(null);
		SetDirty(true);
	}
	public int GetSelectedColumn() {
		GameObject selected = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
		if(selected == null) return -1;
		for(int i = 0; i < columnElements.Length; ++i) {
			if(columnElements[i].transform.gameObject == selected) { return i; }
		}
		return -1;
	}
	// public void Select(int columnIndex){columnElements[columnIndex].Select();}
	// public void Deselect() {
	// 	columnElements[GetSelectedColumn()].Deselect();
	// 	UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
	// }
	/// <param>indexOfSubject</param>use -1 for the top label
	// public void Refresh(ListUI manager, int indexOfSubject) {Set(manager, indexOfSubject);if(IsDirty())Refresh();}
	public void Refresh() {
		RectTransform r = GetComponent<RectTransform>();
		ListUI.ColumnRule[] rules = manager.format.columnRules;
		if(columnElements == null || columnElements.Length != rules.Length
		|| uiSignature != manager.format.signature) {
			const float DEFAULT_HEIGHT = 8;
			if(columnElements != null) { R3.AddRange(columnElements); }
			// make sure this VisibleElement (row of data) has the correct UI columns
			columnElements = new ColumnElement[rules.Length];
			float cursorX = 0;
			for(int i = 0;i<rules.Length;++i) {
				// by default, make columns text
				System.Type typeNeededHere = rules[i].uiType;//typeof(VisibleElement.Text);
				// labels go above the 0th element
				if(indexOfSubject < 0) { typeNeededHere = typeof(ColumnElement.Label); }
				// see if an extra element of the needed type is floating around, reay for use...
				columnElements[i] = R3.Get(typeNeededHere) as ColumnElement;
				if(columnElements[i] == null) { columnElements[i] = ColumnElement.Create(i, this, typeNeededHere); }
				else { columnElements[i].Set(i, this); }
				RectTransform rc = columnElements[i].GetComponent<RectTransform>();
				UGUI.UPPERLEFT_ANCHOR(rc);
				rc.anchoredPosition = new Vector2(cursorX, 0);
				rc.sizeDelta = new Vector2(rules[i].width, DEFAULT_HEIGHT);
				cursorX += rules[i].width;
			}
			uiSignature = manager.format.signature;
			r.sizeDelta = new Vector2(cursorX, DEFAULT_HEIGHT);
		}
		if(indexOfSubject >= 0) {
			float y = manager.GetOffsetY(indexOfSubject);
			r.anchoredPosition = new Vector2(0, -y);
			ApplySetting(manager.uiSettings.element);
		} else {
			float y = manager.StartY() - manager.uiSettings.label.height;
			r.anchoredPosition = new Vector2(0, -y);
			ApplySetting(manager.uiSettings.label);
		}
		SetDirty(false);
	}
	public float GetXPositionOfColumn(int columnIndex) {
		ListUI.ColumnRule[] r = manager.format.columnRules;
		float xPosition=0;for(int i=0;i<columnIndex;++i){xPosition += r[i].width;}return xPosition;
	}
	public void ApplySetting(ListUI.TextSetting textSetting) {
		RectTransform selfR = GetComponent<RectTransform>();
		if(selfR.sizeDelta.y != textSetting.height) {
			selfR.sizeDelta = new Vector2(selfR.sizeDelta.x, textSetting.height);
			for(int i = 0; i < columnElements.Length; ++i) {columnElements[i].SetHeight(textSetting.height);}
		}
		ListUI.ColumnRule[] rules = manager.format.columnRules;
		for(int i = 0; i < rules.Length; ++i) {
			textSetting.AssignTo(columnElements[i].textProxy);
			columnElements[i].Set(i,this);
			columnElements[i].SetDirty(true);
		}
	}
	public object Resolve(int columnIndex) { return manager.format.columnRules[columnIndex].Resolve(Subject); }
	public string GetLabel(int columnIndex) {return manager.format.columnRules[columnIndex].label as string;}

	void FixedUpdate(){ if(IsDirty()) {Refresh();} }
}