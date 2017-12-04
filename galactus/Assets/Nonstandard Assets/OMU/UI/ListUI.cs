using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ListUI : MonoBehaviour, R3.Reusable, ListUI.HasDirtyFlag {
	public interface HasDirtyFlag { bool IsDirty(); void Refresh(); void SetDirty(bool dirty=true); bool IsUIReady(); void InitializeUI(); }
	void FixedUpdate(){ if(IsDirty()) Refresh(); }
	/// objects managed by this list/table user interface
	private System.Array objects;
	public System.Array GetObjects(){return objects;}
	/// which elements are currently being displayed in the list
	public List<RowElement> visibleRows;
	/// used to identify which elements should be created in the view rectangle
	private int[] elementsFound;
	/// TODO rename? remove? seems awkward?
	public RectTransform Self() { return GetComponent<RectTransform>(); }
	/// scrolling UI element
	public ScrollRect scrollView;
	public RowElement labelElement; // TODO private
	// TODO replace with RowElement.Text?
	private GameObject titleElement;
	public int IndentationLevel { get; set;}
	public enum Labeling {none, firstElement, alwasOnTop }
	public float CalculateWidth() { return format.width+uiSettings.padding.x*2+margin.x*2; }
	public float CalculateHeight() { return GetOffsetY(objects.Length)+uiSettings.padding.y*2+margin.y*2; }
	public Vector2 margin = new Vector2(4,4);
	private bool isDirty = false;
	public void SetDirty(bool dirty){
		isDirty=dirty;
		if(dirty){
			if(visibleRows != null)
			for(int i=0;i<visibleRows.Count;++i){visibleRows[i].SetDirty(dirty);}
		}
	}
	public bool IsDirty(){return isDirty;}
	[SerializeField]
	private string title;
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[System.Serializable]
	public class UISettings {
		public Labeling labeling = Labeling.firstElement;
		public TextSetting element = new TextSetting(null,11,12,Color.black,TextAnchor.LowerLeft), 
		label = new TextSetting(null,11,12,Color.black,TextAnchor.MiddleCenter),
		title = new TextSetting(null,14,16,Color.black,TextAnchor.MiddleCenter);
		public Vector2 padding = new Vector2(1,1);
		public float scrollbarWidth = 8;
		public float indentation = 8;
		public UISettings(Labeling labeling,TextSetting element, TextSetting label, TextSetting title,Vector2 padding = default(Vector2)){
			this.labeling=labeling;this.element=element;this.label=label;this.title=title;this.padding=padding;
		}
		public void Init() { element.FixIfBadInitialValues(); label.FixIfBadInitialValues(); title.FixIfBadInitialValues(); }
		public UISettings Clone() { return this.MemberwiseClone() as UISettings; }
	}
	public UISettings uiSettings = default(UISettings);
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class Format {
		public string signature = null;
		public ColumnRule[] columnRules = null;
		public object generatedForType = null;
		public float width = -1;
		public Format(ColumnRule[] rules, object generatedFor = null) { SetColumnRules(rules);this.generatedForType=generatedFor; }
		public Format(System.Type autoGenerateForThisType) { AutoGenerateColumnRules(autoGenerateForThisType);}
		public void AutoGenerateColumnRules(System.Type t) {
			generatedForType = t;
			SetColumnRules(ColumnRule.GenerateFor(t));
		}
		public void SetColumnRules(ColumnRule[] columnRules) {
			this.columnRules = columnRules;
			this.signature = "";
			this.width = 0;
			if(columnRules != null){
				for(int i = 0; i < columnRules.Length; ++i) { 
					signature += ColumnElement.GetElementCodeLetter(columnRules[i].uiType);
					columnRules[i].SetX(width);
					width += columnRules[i].width;
				}
			}
		}
	}
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public Format format = null;
	public void Init() { uiSettings.Init(); }
	public string GetTitle(){ return title; }
	public void SetTitle(string title){this.title=title;calculatedBaseOffset=-1;}
	[HideInInspector]
	public float calculatedBaseOffset = -1;
	public float StartY() {
		if(calculatedBaseOffset == -1) {
			calculatedBaseOffset = 0;
			if(uiSettings.labeling == Labeling.firstElement) {
				calculatedBaseOffset += uiSettings.label.height;
				if(title != null && title.Length > 0) { calculatedBaseOffset += uiSettings.title.height; }
			}
		}
		return calculatedBaseOffset;
	}	
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// keep track of UI element state to remember when elements scroll out of view.
	/// TODO maybe these elements should just not dissapear, and stay in scope? maybe don't recycle them if they're active?
	public class UIAdjustmentState : System.IComparable {
		public int Index, columnIndex, caretAnchor=-1, caretFocus=-1;
		public enum Adjustment { none, selected, expanded }
		public string textAsItWas;
		public Adjustment adjustment;
		public List<UIAdjustmentState> subAdjustments;
		public UIAdjustmentState(int index, int column, Adjustment a=Adjustment.none,
		string text=null, List<UIAdjustmentState> subAdjustments=null) {
			Index=index;columnIndex=column;adjustment=a;textAsItWas=text;this.subAdjustments=subAdjustments;
		}
		public int CompareTo(object b) {
			UIAdjustmentState b_ = b as UIAdjustmentState;
			int d=Index.CompareTo(b_.Index);if(d==0){d=columnIndex.CompareTo(b_.columnIndex);}return d;
		}
	}
	public List<UIAdjustmentState> adjustments;
	public void ADJUST_Add(UIAdjustmentState uias) {
		int index = 0;
		if(adjustments != null) {index = adjustments.BinarySearch(uias);} 
		else {adjustments = new List<UIAdjustmentState>();}
		if(index >= 0 && adjustments.Count > 0) { Debug.Log("overwriting..."); adjustments[index] = uias; }
		else {
			if(index==0 && adjustments.Count==0) {adjustments.Add(uias);} 
			else {adjustments.Insert(~index, uias);}
		}
	}
	public UIAdjustmentState ADJUST_Get(int index, int column, bool alsoRemove = false) {
		if(adjustments == null || adjustments.Count == 0) return null;
		UIAdjustmentState uias = new UIAdjustmentState(index, column);
		int found = adjustments.BinarySearch(uias);
		if(found >= 0) {
			uias = adjustments[found];
			if(alsoRemove) { adjustments.RemoveAt(found); }
			return uias;
		}
		return null;
	}
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[System.Serializable]
	public class TextSetting {
		public Font font;
		public float size = 10, height = 12;
		public Color color = Color.black;
		public TextAnchor alignment = TextAnchor.UpperLeft;
		public TextSetting(Font f, float s, float h, Color c, TextAnchor a){
			font=f;size=s;height=h;color=c;alignment=a;
		}
		public void FixIfBadInitialValues(){
			if(font==null){font=Resources.GetBuiltinResource<Font>("Arial.ttf");}
			if(color==Color.clear)color=Color.black;
			if(size==0 && height==0){size=10;height=12;}
		}
		public void AssignTo(UnityEngine.UI.Text t){t.font=font;t.color=color;t.fontSize=(int)size;t.alignment=alignment;}
	}
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public bool IsUIReady(){return scrollView != null;}
	public void InitializeUI(){
		if(GetComponent<CanvasRenderer>() == null) gameObject.AddComponent<CanvasRenderer>();
		RectTransform rect = GetComponent<RectTransform>();
		if(rect == null) rect = gameObject.AddComponent<RectTransform>();
		RectTransform contentArea = GetOrCreateScrollableRect(transform.GetComponent<RectTransform>(), out scrollView, uiSettings.scrollbarWidth);
		contentArea.anchoredPosition = Vector2.zero;
		scrollView.onValueChanged.RemoveAllListeners();
		scrollView.onValueChanged.AddListener(RefreshVisibleElements);
		if(itemPositions==null||itemPositions.flux.Count==0){
			itemPositions=new LinearFlux(GetStandardElementHeight(),StartY());
		}
		calculatedBaseOffset = -1;
		RefreshHeaderSize();
	}
	public void Set(IList data, Format format = null, ListUI parentList = null) {
		if(parentList != null){
			uiSettings = parentList.uiSettings.Clone();
			uiSettings.labeling = Labeling.firstElement;
			IndentationLevel = parentList.IndentationLevel+1;
			margin = Vector2.zero;
		}
		uiSettings.Init();
		if(format == null && data != null){CreateInferredFormatIfNeeded(data);}
		else { SetFormat(format); }
		if(!IsUIReady()){InitializeUI();}
		SetData(data);
		if(parentList != null){
			transform.SetParent(parentList.scrollView.content.transform);
			RectTransform rect = GetComponent<RectTransform>();
			UGUI.UPPERLEFT_ANCHOR(rect);
			rect.sizeDelta = new Vector2(CalculateWidth(), CalculateHeight());
		}
	}
	public void GetVisibleElementRange(out float min, out float max) {
		float viewRectH = scrollView.viewport.sizeDelta.y;
		if(viewRectH == 0) {
			RectTransform r = scrollView.viewport.parent.GetComponent<RectTransform>();
			while(r != null && viewRectH <= 0) {
				viewRectH = r.sizeDelta.y;
				r = r.parent.GetComponent<RectTransform>();
			}
		}
		float contentH = scrollView.content.sizeDelta.y;
		// Debug.Log(scrollView.verticalNormalizedPosition+" -- "+contentH+" .. "+viewRectH);
		if(contentH < viewRectH) { min = 0; max = objects.Length; }
		else {
			float minPixelShown = (1-scrollView.verticalNormalizedPosition) * (contentH-viewRectH);// - table.StartY();
			float maxPixelShown = minPixelShown + viewRectH;
			min = GetItemAtOffset(minPixelShown);
			max = GetItemAtOffset(maxPixelShown);
		}
		if(max >= objects.Length) { max = objects.Length-1; }
	}
	public float GetStandardElementHeight() { return uiSettings.element.height; }
	public static System.Type GetIListType(System.Type type) {
		foreach (System.Type interfaceType in type.GetInterfaces()) {
			if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IList<>))
				return type.GetGenericArguments()[0];
		} return null;
	}

	public LinearFlux itemPositions;
	// TODO!!!!
	public void MarkItemDimensions(int index, float start, float delta) {
		if(start < 0 || delta < 0) { throw new System.Exception("what is making this negative? "+delta+"t + "+start);}
//		MarkItemAt(index, start, delta);
//		MarkItemAt(index+1, start + delta, GetStandardElementHeight());
		itemPositions.AddInconsistency(index, delta, start);
		// TODO instead of GetStandardElementHeight(), figure out what the correct next height is...
		itemPositions.AddInconsistency(index+1, GetStandardElementHeight(), start+delta);
		ForceRefreshAllElements(index+1);
		// SetDirty(true); // TODO does SetDirtyWithoutChildren work better?
	}
	/// only call this on the item *after* the one that was expanded/contracted
	// public void MarkItemAt(int index, float offsetY) {
	// 	if(offsetY < 0) { throw new System.Exception("what is making the offset negative?");}
	// 	itemPositions.AddInconsistency(index, GetStandardElementHeight(), offsetY);
	// }
	public float GetOffsetY(int indexOfElement) { return itemPositions.GetPosition(indexOfElement); }
	public float GetItemAtOffset(float offsetY) { return itemPositions.GetT(offsetY); }
	public void SetFormat(Format format) { this.format = format;}
	public System.Type CreateInferredFormatIfNeeded(IList objects){
		System.Type elementType = null;
		if(objects.Count > 0) { elementType = objects[0].GetType(); }
		if(elementType == null) { objects.GetType().GetElementType(); }
		if(elementType == null) { elementType = objects.GetType().GetGenericArguments()[0]; }
		if(format == null) { SetFormat(new Format(elementType)); }
		return elementType;
	}
	public void SetData(IList objects) {
		System.Type elementType = CreateInferredFormatIfNeeded(objects);
		this.objects = System.Array.CreateInstance(elementType, objects.Count);
		for(int i=0;i<objects.Count;++i) { this.objects.SetValue(objects[i], i); }
		SetDirty(true);
		//RefreshVisibleElements();
		// Debug.Log("Making table of "+objects.Count+" elements!");
		// string s="";for(int i=0;i<objects.Count;++i){s+=objects[i]+",";}Debug.Log(s);
	}
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class ColumnRule {
		public object label;
		public float width;
		private float startX = -1;
		public OMU.Expression expr;
		public System.Type uiType = null;
		public float GetX(){return startX;}
		public void SetX(float startX){this.startX=startX;}
		public object Resolve(object forObject) {
			OMU.Expression EXPR = expr as OMU.Expression;
			if(EXPR != null) return EXPR.Resolve(forObject);
			throw new System.Exception("Can only resolve an OMU.Expression");
		}
		public ColumnRule(object label, float width, OMU.Expression expr, System.Type uiType = null) {
			this.label = label;
			this.width = width;
			if(expr is OMU.Expression) {
				this.expr = expr;
			} else {
				throw new System.Exception("can only accept an OMU.Expression-that-reference-members, or a sub-table");
			}
			this.uiType = (uiType!=null)?uiType:typeof(ColumnElement.Text);
		}
		public void Assign(object o, string newValueString) {
			if(expr is OMU.Expression) {
				OMU.Expression assign = new OMU.Expression(OMU.Expression.OP_ASSIGN, expr, newValueString);
				assign.Resolve(o);
			} else {
				throw new System.Exception("Cannot assign using type "+expr.GetType()+", must use "+typeof(OMU.Expression));
			}
		}
		class ObjectExpressionComparer : IComparer {
			/// the expression that will be resolved on object A and object B. The result of this expression determines ordering.
			public OMU.Expression EXPR;
			public ObjectExpressionComparer(OMU.Expression expr){EXPR = expr;}
			public int Compare(object obja, object objb){
				if(EXPR != null) {
					object a = EXPR.Resolve(obja), b = EXPR.Resolve(objb);
					return (a as System.IComparable).CompareTo(b);
				} else {
					throw new System.Exception("Can only sort by expressions.");
				}
			}
		}
		private static ObjectExpressionComparer expCmp = new ObjectExpressionComparer(null);
		public void Sort(System.Array list, bool reverse = false) {
			expCmp.EXPR = expr as OMU.Expression;
			System.Array.Sort(list, expCmp);
			if(reverse) { System.Array.Reverse(list); }
		}
		public static ColumnRule[] GenerateFor(System.Type t) {
			// new ColumnRule[] {
			// new ColumnRule("name",         50, Expr("(name)")),
			// new ColumnRule("description", 100, Expr("(desc)")
			// 	// new OMU.Expression(OMU.Expression.OP_SCRIPTED_VALUE, "desc")
			// 	, true),
			if(t.IsValueType) { return null; }
			List<ColumnRule> rules = new List<ColumnRule>();
			System.Reflection.FieldInfo[] fi = t.GetFields();
			if(fi != null) {
				for(int i=0;i<fi.Length;++i){
					System.Type eType = GetIListType(fi[i].FieldType);
					// Debug.Log(fi[i].Name+"\n"+fi[i]+"\n1 "+fi[i].ReflectedType+"\n2 "+fi[i].MemberType+
					// "\n3 "+fi[i].DeclaringType+"\n4 "+fi[i].FieldType+"\n5 "+fi[i].FieldType.IsArray+"\n6 "+eType);
					string n = fi[i].Name;
					if(fi[i].FieldType == typeof(string)
					|| fi[i].FieldType == typeof(float)
					|| fi[i].FieldType == typeof(int)
					|| fi[i].FieldType == typeof(double)
					|| fi[i].FieldType == typeof(long)
					) {
						rules.Add(new ColumnRule(n, n.Length*12, new OMU.Expression(OMU.Expression.OP_SCRIPTED_VALUE, n), typeof(ColumnElement.Input)));
					}
					if(eType != null) {
						rules.Add(new ColumnRule(n, n.Length*12, new OMU.Expression(OMU.Expression.OP_SCRIPTED_VALUE, n), typeof(ColumnElement.SublistButton)));
					}
				}
			}
			return rules.ToArray();
		}
	}
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	private static RectTransform GetOrCreateScrollableRect(Transform transform, out ScrollRect scrollView, float scrollbarWidth) {
		RectTransform selfR = transform.GetComponent<RectTransform>();
		if(selfR == null){selfR=transform.gameObject.AddComponent<RectTransform>();}
		scrollView = transform.GetComponent<ScrollRect>();
		if (!scrollView) {
			GameObject scrollViewO = new GameObject("scrollview");
			// Image img = scrollViewO.AddComponent<Image>();
			// img.color = new Color(0, 0, 0, 0.5f);
			scrollViewO.AddComponent<RectTransform>();
			scrollViewO.AddComponent<CanvasRenderer>();
			scrollView = scrollViewO.AddComponent<ScrollRect>();
			scrollViewO.transform.SetParent(transform);
			scrollView.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
			scrollView.verticalScrollbarSpacing = -3;
			scrollView.horizontal = false;
			scrollView.movementType = ScrollRect.MovementType.Clamped;
			UGUI.MaximizeRectTransform(scrollView.transform);
		}
		if (scrollView.viewport == null) {
			GameObject viewport = new GameObject("viewport");
			viewport.transform.SetParent(scrollView.transform);
			Image img = viewport.AddComponent<Image>();
			img.color = new Color(0, 0, 0, 0.5f);
			Mask m = viewport.AddComponent<Mask>();
			m.showMaskGraphic = false;
			RectTransform r = UGUI.MaximizeRectTransform(viewport.transform);
			r.offsetMax = new Vector2(0, 0);
			r.pivot = new Vector2(0, 1);
			r.anchorMax = Vector2.one;
			r.anchorMin = Vector2.zero;
			r.offsetMax = r.offsetMin = Vector2.zero;
			scrollView.viewport = r;
			// img.enabled = false;//disables masking. useful for debugging items being drawn off-mask
		}
		if(scrollView.verticalScrollbar == null) {
			GameObject scrollbarObj = new GameObject("vertical scrollbar");
			scrollbarObj.AddComponent<RectTransform>();
			scrollbarObj.AddComponent<CanvasRenderer>();
			// Image img = scrollbarObj.AddComponent<Image>();
			// img.color = Color.magenta;
			scrollbarObj.transform.SetParent(scrollView.transform);
			RectTransform rsb = scrollbarObj.GetComponent<RectTransform>();
			rsb.anchorMin = new Vector2(1,0);
			rsb.anchorMax = new Vector2(1,1);
			rsb.offsetMax = Vector2.zero;
			rsb.offsetMin = new Vector2(-scrollbarWidth,0);
			Scrollbar scrollbar = scrollbarObj.AddComponent<Scrollbar>();
			GameObject scrollHandle = new GameObject("handle");
			Image himg = scrollHandle.AddComponent<Image>();
			himg.color = Color.white;
			scrollHandle.transform.SetParent(scrollbarObj.transform);
			RectTransform rh = scrollHandle.GetComponent<RectTransform>();
			rh.offsetMin = rh.offsetMax = Vector2.zero;
			scrollbar.handleRect = himg.transform.GetComponent<RectTransform>();
			scrollbar.direction = Scrollbar.Direction.BottomToTop;
			scrollView.verticalScrollbar = scrollbar;
			scrollView.verticalScrollbarSpacing = -3;
			scrollView.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
		}
		if (scrollView.content == null) {
			GameObject content = new GameObject("content");
			content.AddComponent<CanvasRenderer>();
			content.AddComponent<RectTransform>();
			content.transform.SetParent(scrollView.viewport.transform);
			RectTransform r = content.GetComponent<RectTransform>();
			r.anchorMin = new Vector2(0, 1);
			r.anchorMax = Vector2.one;
			r.offsetMin = r.offsetMax = Vector2.zero;
			r.pivot = new Vector2(0, 1); // scroll from the top
			scrollView.content = r;
		}
		return scrollView.content;
	}
	private void CreateLabelIfNeeded() {
		if(labelElement == null) {
			labelElement = R3.Get(typeof(RowElement)) as RowElement;
			if(labelElement == null) { labelElement = RowElement.Create(this, -1); }
			else {labelElement.Set(this,-1);}
			labelElement.transform.SetParent(scrollView.content.transform);
		}
	}
	public void Refresh(){
		//ForceRefreshAllElements();
		RefreshVisibleElements();
		SetDirty(false);
	}
	/// TODO don't call this any more. instead, check if it's dirty, and only call Refresh() then.
	public void ForceRefreshAllElements(int atOrAfterIndex = 0) {
		int s=visibleRows.Count;
		for(int i=visibleRows.Count-1;i>=0;--i){
			RowElement e = visibleRows[i];
			// if(e.Index >= atOrAfterIndex) { R3.Add(e); visibleRows.RemoveAt(i); }
			if(e.Index >= atOrAfterIndex) { e.SetDirty(true); }
		}
		// Debug.Log("refreshingafter "+atOrAfterIndex+". total before:"+s+", total after: "+visibleRows.Count+" for "+this.name);
		// RefreshVisibleElements();
		SetDirty(true);
	}
	public void RefreshHeaderSize() {
		RectTransform sr = scrollView.GetComponent<RectTransform>();
		Vector2 offset = Vector2.zero;
		if(uiSettings.labeling != Labeling.none) {
			if(uiSettings.labeling == Labeling.alwasOnTop) {
				offset = new Vector3(uiSettings.padding.x+margin.x, -(uiSettings.padding.y+margin.y));
			}
			CreateLabelIfNeeded();
			string title = GetTitle();
			if(title != null && title.Length > 0) {
				RectTransform r;
				if(titleElement==null) {
					titleElement = new GameObject("title");
					Text t = titleElement.AddComponent<UnityEngine.UI.Text>();
					t.text = title;
					r = titleElement.transform.GetComponent<RectTransform>();
					UGUI.UPPERLEFT_ANCHOR(r);
					titleElement.transform.SetParent(Self());
					r.anchoredPosition = offset;
					r.sizeDelta = new Vector2(format.width, uiSettings.title.height);
					uiSettings.title.AssignTo(t);
				} else { r = titleElement.transform.GetComponent<RectTransform>(); }
				if(uiSettings.labeling == Labeling.firstElement) {
					titleElement.transform.SetParent(scrollView.content);
				}
				offset.y -= uiSettings.title.height;
			}
		}
		if(uiSettings.labeling == Labeling.alwasOnTop) {
			RectTransform lr = labelElement.GetComponent<RectTransform>();
			lr.anchoredPosition = offset;
			offset.y -= uiSettings.label.height;
			sr.offsetMax = new Vector2(-(uiSettings.padding.x+margin.x), offset.y);
		} else if(uiSettings.labeling == Labeling.firstElement) {
			RectTransform lr = labelElement.GetComponent<RectTransform>();
			lr.SetParent(scrollView.content);
			lr.anchoredPosition = offset;
			sr.offsetMax = -(uiSettings.padding+margin);
		} else if(uiSettings.labeling == Labeling.none) {
			sr.offsetMax = -(uiSettings.padding+margin);
		}
		sr.offsetMin = (uiSettings.padding+margin);
	}
	private bool isInTheMiddleOfRefeshing=false; // DEBUG TODO remove once HasDirtyFlag is set up correctly
	public void RefreshVisibleElements(Vector2 delta = default(Vector2)) {
		if(isInTheMiddleOfRefeshing) {Debug.Log("uh oh... recursive ListUI refresh...");return;}
		isInTheMiddleOfRefeshing=true;
		if(visibleRows == null) { visibleRows = new List<RowElement>(); }
		float minShown = 0, maxShown = objects.Length-1;
		GetVisibleElementRange(out minShown, out maxShown);
		// Debug.Log("show "+minShown+" to "+maxShown);
		int iMin = (int)minShown, iMax = (int)maxShown;
		float height = (objects != null)?GetOffsetY(objects.Length):0;
		RectTransform contentArea = scrollView.content;
		contentArea.sizeDelta = new Vector2(contentArea.sizeDelta.x, height);
		int totalShown = (iMax-iMin)+1, ind;
		if(elementsFound == null || elementsFound.Length < totalShown) { elementsFound = new int[totalShown]; }
		for(int i = 0; i < totalShown; ++i) { elementsFound[i] = 0; }
		// check to see which elements are represented
		for(int i = visibleRows.Count-1; i >= 0; --i) {
			RowElement e = visibleRows[i]; ind = e.Index;
			// if this visible element doesn't need to be represented (because it's out of bounds)
			// or if this element is a duplicate of one already marked as correct
			if(ind < minShown || ind > maxShown || elementsFound[ind-iMin] != 0) {
				R3.Add(e); visibleRows.RemoveAt(i); // recycle it
			}
			// otherwise, mark that the element is correct
			else { elementsFound[ind-iMin] = 1; }
		}
		for(int i = 0; i < totalShown; ++i) {
			int index = iMin+i;
			// if there is a valid, unrepresented element
			if(index >= 0 && elementsFound[i] == 0) {
				// this element needs to exist. allocate it and add it to the UI.
				RowElement e = R3.Get(typeof(RowElement)) as RowElement;
				if(e==null) { e = RowElement.Create(this, index); }
				else { e.Set(this, index); }
				AddVisibleElement(e);
			}
		}
		isInTheMiddleOfRefeshing=false;
	}
	public void AddVisibleElement(RowElement ve) {
		// DEBUG if the element is already in here, CRASH!
		for(int i=0;i<visibleRows.Count;++i){if(visibleRows[i].Equals(ve)){throw new System.Exception("adding a duplicate.... "+ve.Index);}}
		visibleRows.Add(ve);
	}
	public void Recycle() {
		R3.AddRange(visibleRows);
		visibleRows.Clear();
		objects = null;
		transform.SetParent(null);
		SetDirty(true);
	}

	public OMU.Expression Expr(string text) {
		//new OMU.Expression(OMU.Expression.OP_SCRIPTED_VALUE, "name")
		//is the same as Expr("(name)")
		return OMU.Parser.Parse(text) as OMU.Expression;
	}
}