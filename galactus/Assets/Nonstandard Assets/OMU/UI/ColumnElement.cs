using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public abstract class ColumnElement : MonoBehaviour, R3.Reusable, ListUI.HasDirtyFlag {
	public static float DEFAULT_HEIGHT = 7;
	public RowElement row;
	public int columnIndex;
	private bool hasInitializedUI = false;
	public bool IsUIReady(){return hasInitializedUI;}
	public ListUI.ColumnRule GetRule() { return row.manager.format.columnRules[columnIndex]; }
	abstract public void InitializeUI();
	protected bool isDirty = false;public void SetDirty(bool dirty){isDirty=dirty;}public bool IsDirty(){return isDirty;}
	public virtual void Set(int columnIndex,RowElement e){
		if(row==e&&this.columnIndex==columnIndex)return;
		SetDirty(true);row=e;this.columnIndex=columnIndex;
		if(!IsUIReady()){transform.SetParent(row.transform); InitializeUI();hasInitializedUI=true;}
	}
	public virtual void RefreshPosition() {
		transform.SetParent(row.transform);
		RectTransform rc = GetComponent<RectTransform>();
		UGUI.UPPERLEFT_ANCHOR(rc);
		rc.anchoredPosition = new Vector2(GetRule().GetX(), 0);
		rc.sizeDelta = new Vector2(GetRule().width, row.manager.GetStandardElementHeight());
	}
	public void SetHeight(float height) {
		RectTransform r = transform.GetComponent<RectTransform>();
		r.sizeDelta = new Vector2(r.sizeDelta.x, height);
	}
	/// tell the UI element to create itself if it hasn't been properly created yet
	//public virtual void CreateIfNeeded(int columnIndex,RowElement e){Set(columnIndex,e);}
	public static ColumnElement Create(int columnIndex, RowElement row, System.Type typeNeededHere) {
		GameObject go = new GameObject("-");
		ColumnElement elem = go.AddComponent(typeNeededHere) as ColumnElement;
		elem.Set(columnIndex,row);
		return elem;
	}
	public virtual void Refresh() {
        if(textProxy!=null) {
            textProxy.text=row.Resolve(columnIndex).ToString();
        }
		RefreshPosition();
        SetDirty(false);
	}
	/// cleans self up for parent system so it can be repurposed
	public virtual void Recycle(){row=null;columnIndex=-1;transform.SetParent(null);SetDirty(true);}
	public virtual void Select() {UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(gameObject);}
	public virtual void Deselect(){}
	/// convenience, since most UI elements have some kind of text output.
	public UnityEngine.UI.Text textProxy;

	public static char GetElementCodeLetter(System.Type t) {
		char c = '?';
		     if(t == typeof(ColumnElement.Input)) {c = 'I';}
		else if(t == typeof(ColumnElement.Text)) {c = 'T';}
		else if(t == typeof(ColumnElement.TextButton)) {c = 'B';}
		else if(t == typeof(ColumnElement.SublistButton)) {c = 'S';}
		else if(t == typeof(ColumnElement.Label)) {c = 'L';}
		return c;
	}

	void FixedUpdate(){ if(IsDirty()) {Refresh();} }

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class Text : ColumnElement {override public void InitializeUI() {
		textProxy = gameObject.AddComponent<UnityEngine.UI.Text>();
	}}
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class TextButton : ColumnElement {
		public UnityEngine.UI.Button GetButton(){return GetComponent<UnityEngine.UI.Button>();}
		override public void InitializeUI() {
			// create a button
			UnityEngine.UI.Image img = gameObject.AddComponent<UnityEngine.UI.Image>();
			img.color = new Color(img.color.r,img.color.g,img.color.b,0.5f);
			UnityEngine.UI.Button b = gameObject.AddComponent<UnityEngine.UI.Button>();
			b.image = img;
			textProxy = new GameObject("txt").AddComponent<UnityEngine.UI.Text>();
			textProxy.raycastTarget = false;
			textProxy.transform.SetParent(transform);
			UGUI.MaximizeRectTransform(textProxy.transform);
		}
	}
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class Label : TextButton {
		public bool reverseSort = false;
		override public void InitializeUI() {
			base.InitializeUI();
			ListUI.ColumnRule cr = row.manager.format.columnRules[columnIndex];
			GetComponent<UnityEngine.UI.Button>().onClick.AddListener(()=>{
				cr.Sort(row.manager.GetObjects(), reverseSort);
				row.manager.ForceRefreshAllElements(row.Index);
                reverseSort = !reverseSort;
			});
		}
		override public void Refresh(){string t=row.GetLabel(columnIndex);textProxy.text=t;RefreshPosition();SetDirty(false);}
	}
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class Input : ColumnElement {
		public class InputCaret : MonoBehaviour {
			public Input input;
		}
		public Transform caret;
		override public void InitializeUI() {
			UnityEngine.UI.InputField inf = gameObject.AddComponent<UnityEngine.UI.InputField>();
			UnityEngine.UI.Text t = gameObject.AddComponent<UnityEngine.UI.Text>();
			inf.textComponent = textProxy = t;
			inf.onEndEdit.AddListener((s)=>{
				if(row != null) {
                    ListUI.ColumnRule cr = row.manager.format.columnRules[columnIndex];
                    //Debug.Log("~~~ " + row.manager.format.signature + " ~~ " + row.UI_Signature + "  " +
                    //          OMU.Serializer.Stringify(row.manager.format.columnRules));
                    //Debug.Log("assigning in " + row.GetPath());
                    cr.Assign(row.Subject, s);
                }
			//Debug.Log("Set "+s+" "+inf.selectionFocusPosition+" "+inf.selectionAnchorPosition);
			});
		}
		new void FixedUpdate(){
			base.FixedUpdate();
			if(caret == null){
				for(int i=0;i<transform.parent.childCount;++i) {
					Transform c = transform.parent.GetChild(i);
					if(c.name.EndsWith("Input Caret")) {
						RectTransform rt = GetComponent<RectTransform>();
						RectTransform ct = c.GetComponent<RectTransform>();
						if(rt.anchorMin == ct.anchorMin && rt.anchorMax == ct.anchorMax
						&& rt.offsetMin == ct.offsetMin && ct.offsetMax == ct.offsetMax){
                        //&& rt.offsetMin == ct.offsetMin && rt.offsetMax == ct.offsetMax){ // TODO is this better?
							InputCaret ic = c.GetComponent<InputCaret>();
							if(ic==null){ic=c.gameObject.AddComponent<InputCaret>();ic.input=this;caret=c;}break;
						}
					}
				}
			}
		}
		override public void Refresh(){
			RefreshPosition();
			string t=row.Resolve(columnIndex).ToString();
			UnityEngine.UI.InputField inf = GetComponent<UnityEngine.UI.InputField>();
			ListUI.UIAdjustmentState adj = row.manager.ADJUST_Get(row.Index,columnIndex,true);
			if(adj != null) {
				if(adj.adjustment == ListUI.UIAdjustmentState.Adjustment.selected) {
					Select();
					inf.selectionAnchorPosition = adj.caretAnchor;
					inf.selectionFocusPosition = adj.caretFocus;
				}
				if(adj.textAsItWas != null) {
					string originalText = t;
					cancelTextEdit = (s)=>{
						if(inf.wasCanceled) { inf.text = originalText; }
						else { inf.text=s; }
						FinishCancelTextEdit();
					};
					inf.onEndEdit.AddListener(cancelTextEdit);
					t=adj.textAsItWas;
				}
			}
			textProxy.text=t;inf.text=t;
			if(caret != null) { // move the caret to where it is supposed to be
				caret.SetParent(transform.parent);
				RectTransform rt = GetComponent<RectTransform>();
				RectTransform ct = caret.GetComponent<RectTransform>();
				ct.anchoredPosition=rt.anchoredPosition;
				caret.SetSiblingIndex(transform.GetSiblingIndex());
			}
			SetDirty(false);
		}
		private UnityEngine.Events.UnityAction<string> cancelTextEdit;
		private void FinishCancelTextEdit(){if(cancelTextEdit != null){
			GetInputField().onEndEdit.RemoveListener(cancelTextEdit);cancelTextEdit=null;}}
		override public void Select() {GetInputField().Select();}
		override public void Deselect() {
			UnityEngine.UI.InputField inf = GetInputField();
			inf.text=row.Resolve(columnIndex).ToString();
			inf.selectionFocusPosition=inf.selectionAnchorPosition=0;
			FinishCancelTextEdit();
		}
		override public void Recycle() {
			if(row != null) {
				int selectedColumn = row.GetSelectedColumn();
				if(selectedColumn == columnIndex) {
					ListUI.UIAdjustmentState uias = new ListUI.UIAdjustmentState(row.Index,columnIndex);
					uias.adjustment = ListUI.UIAdjustmentState.Adjustment.selected;
					UnityEngine.UI.InputField inf = GetInputField();
					uias.textAsItWas = inf.text;
					// Debug.Log("remembering "+uias.textAsItWas+" -->"+ve.Index+" "+columnIndex);
					uias.caretAnchor = inf.selectionAnchorPosition;
					uias.caretFocus = inf.selectionFocusPosition;
					row.manager.ADJUST_Add(uias);
					Deselect();
				}
			}
			base.Recycle();
		}
		public UnityEngine.UI.InputField GetInputField(){return GetComponent<UnityEngine.UI.InputField>();}
	}
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// can only deal with lists or non-value types
	public class SublistButton : TextButton {
		ListUI sublist = null;
        private bool isSingleObject = false;
		public IList GetList() {
            object data = row.Resolve(columnIndex);
            IList list = data as IList;
            if(list == null) {
                list = new List<object>();
                if(data != null) {
                    list.Add(data);
                }
                isSingleObject = true;
            }
            return list;
        }
		/// prevent recursive expansion. best case, double-up on elements. worst case, stack overflow. TODO remove once HasDirtyFlag is working
		private bool inTheMiddleOfExpanding = false;
		public bool IsExpanded() { return sublist != null && sublist.gameObject.activeInHierarchy; }
		override public void Recycle() {
			if(sublist != null) {
				sublist.Recycle(); // recycle children.
				// recycle self, using data compiled by children, that just recycled themselves
				ListUI.UIAdjustmentState adj = new ListUI.UIAdjustmentState(row.Index,columnIndex);
				if(IsExpanded()) { adj.adjustment = ListUI.UIAdjustmentState.Adjustment.expanded; }
				if(sublist.adjustments != null && sublist.adjustments.Count != 0) {
					adj.subAdjustments = sublist.adjustments;
					sublist.adjustments = null;
				}
				if(adj.adjustment != ListUI.UIAdjustmentState.Adjustment.none || adj.subAdjustments != null) {
					row.manager.ADJUST_Add(adj);
				}
				R3.Add(sublist);sublist=null;
			}
			base.Recycle();
		}
		public void ExpandSubtable(bool show) {
			if(inTheMiddleOfExpanding) return;
			inTheMiddleOfExpanding = true;
			// whether enabling the table or disabling, the location of the next element must be calculated
			float yOfThisElement = row.manager.GetOffsetY(row.Index);
			float yOfNextElement = yOfThisElement + row.manager.GetStandardElementHeight();
			bool needUpdate = show != IsExpanded();
			// enable or disable
			if(show) {
				if(sublist==null) {
					// create a new subtable, and put it below this VisibleElement, but parented to the same parent
					sublist = R3.Get(typeof(ListUI)) as ListUI;
					if(sublist == null) { sublist = new GameObject("table").AddComponent<ListUI>(); }
					if(sublist.visibleRows != null && sublist.visibleRows.Count > 0) throw new System.Exception("expected empty list... (no visible rows)");
					if(sublist.scrollView != null && sublist.scrollView.content.transform.childCount > ((sublist.labelElement!=null)?1:0))
						throw new System.Exception("expected empty list... (no content children, except maybe label)");
					sublist.transform.SetParent(transform.parent);
				} 
				// expand the recorded height of this VisibleElement to include the sub-table
                sublist.Set(GetList(), null, row.manager, row.Index);
				float xIndent = sublist.IndentationLevel * row.manager.uiSettings.indentation;
				RectTransform r = sublist.GetComponent<RectTransform>();
				r.anchoredPosition = new Vector2(xIndent, -yOfNextElement);
				yOfNextElement += sublist.CalculateHeight();//r.sizeDelta.y;
				// sublist.RefreshVisibleElements();
				sublist.gameObject.SetActive(true);
			} else {
				if(sublist!=null){sublist.gameObject.SetActive(false);}
			}
            // TODO verify that if !show, yOfNextElement is the standard calculated value expected for (ve.Index+1)
            // float stdexpected = ve.manager.GetOffsetY(ve.Index) + ve.manager.GetStandardElementHeight();
            // Debug.Log((ve.Index+1)+" "+yOfNextElement+" "+stdexpected);

            //Debug.Log("update? " + needUpdate);
			if(needUpdate) {
                // TODO ve.manager.MarkItemDimensions
                //				ve.manager.MarkItemDimensions(ve.Index, yOfThisElement, yOfNextElement-yOfThisElement);
                // ve.manager.itemPositions.AddInconsistency(ve.Index, yOfNextElement-yOfThisElement, yOfThisElement);
                // ve.manager.MarkItemAt(ve.Index+1, yOfNextElement);
                // ve.manager.ForceRefreshAllElements(ve.Index+1);
                float nextHeight = yOfNextElement - yOfThisElement;
                //Debug.Log("next: " + nextHeight);
                row.manager.MarkItemDimension(row.Index, nextHeight);
                row.manager.ForceRefreshAllElements(row.Index + 1);
			}
			SetDirty(true);
			inTheMiddleOfExpanding = false;
		}
		override public void InitializeUI() {
			base.InitializeUI();
			GetButton().onClick.AddListener(()=>{ExpandSubtable(!IsExpanded());});
		}
		override public void Refresh() {
            //Debug.Log("Refresh ColumnElement "+this.GetPath());
			RefreshPosition();
			ListUI.UIAdjustmentState adj = row.manager.ADJUST_Get(row.Index,columnIndex,true);
			string t = row.GetLabel(columnIndex);
			IList l = GetList();
            if (!isSingleObject) {
                if (l != null) { t += "(" + l.Count + ")"; }
            } else {
                if (l != null) {
                    if(l.Count == 0) {
                        t = "(null)";
                    } else {
                        t = "(" + t + ")";
                    }
                }
            }
			textProxy.text = t;
			if(adj != null) {
				if(adj.subAdjustments != null) {row.manager.adjustments=adj.subAdjustments;}
				if(adj.adjustment == ListUI.UIAdjustmentState.Adjustment.expanded) {
					if(sublist == null) { ExpandSubtable(true); }
				} else {
					ExpandSubtable(false);
				}
			}
			SetDirty(false);
		}
        override public void RefreshPosition() {
            base.RefreshPosition();
            // update the sublist if there is one
            if(sublist != null) {
                float yOfThisElement = row.manager.GetOffsetY(row.Index);
                float yOfNextElement = yOfThisElement + row.manager.GetStandardElementHeight();
                float xIndent = sublist.IndentationLevel * row.manager.uiSettings.indentation;
                RectTransform r = sublist.GetComponent<RectTransform>();
                r.anchoredPosition = new Vector2(xIndent, -yOfNextElement);
            }
        }
	}
}