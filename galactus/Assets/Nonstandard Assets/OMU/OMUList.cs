using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OMUList : MonoBehaviour {

	public class UIListing {
		public static RectTransform UPPERLEFT_ANCHOR(RectTransform r) {
			r.pivot = new Vector2(0,1);
			r.anchorMin = new Vector2(0,1);
			r.anchorMax = r.anchorMin;
			return r;
		}
		public RectTransform rect, root;
		private List<object> objects;
		public List<VisibleElement> visibleElements;
		public List<VisibleElement> unusedElements;
		public bool minimumObjectUse = true;
		private string uiSignature;
		public int[] elementsFound;
		public void GetVisibleElementRange(out int min, out int max) {
			float viewRectH = scrollView.viewport.sizeDelta.y;
			if(viewRectH == 0) {
				RectTransform r = scrollView.viewport.parent.GetComponent<RectTransform>();
				while(r != null && viewRectH <= 0) {
					viewRectH = r.sizeDelta.y;
					r = r.parent.GetComponent<RectTransform>();
				}
			}
			float contentH = scrollView.content.sizeDelta.y;
			// Debug.Log("  "+contentH+" "+viewRectH);
			if(contentH < viewRectH) {
				min = 0;
				max = objects.Count;
			} else {
				float minPixelShown = (1-scrollView.verticalNormalizedPosition) * (contentH-viewRectH);
				float maxPixelShown = minPixelShown + viewRectH;
				if(labeling == Labeling.firstElement) {
					minPixelShown -= labelHeight;
					maxPixelShown -= labelHeight;
				}
				min = (int)(minPixelShown / elementHeight);
				max = (int)(maxPixelShown / elementHeight);
			}
			if(max >= objects.Count) {
				max = objects.Count-1;
			}
		}
		public ScrollRect scrollrect;
		public Font font;
		public Color color = Color.black;
		public ScrollRect scrollView;
		private VisibleElement labelElement;
		public enum Labeling {none, firstElement, alwasOnTop }
		public Labeling labeling = Labeling.alwasOnTop;
		public UIListing (RectTransform rt) {
			root = rt;
			rect = rt;
			Vector2 size = rect.sizeDelta;
			if (font == null) {
				font = Resources.GetBuiltinResource<Font>("Arial.ttf");
			}
			rect = GetOrCreateScrollableRect(rect, out scrollView);
			rect.sizeDelta = size;
			rect.anchoredPosition = Vector2.zero;
			scrollView.onValueChanged.AddListener(RefreshVisibleElements);
		}
		public void SetData(IList objects, float height, ColumnRule[] rules) {
			this.objects = new List<object>();
			for(int i=0;i<objects.Count;++i){
				this.objects.Add(objects[i]);
			}
			elementHeight = height;
			labelHeight = height * 1.5f;
			columnRules = rules;
			RefreshColumnRules();
		}
		public class ColumnRule {
			public object label;
			public float width;
			public OMU.Expression expr;
			public bool editable = false;
			public ColumnRule(object label, OMU.Expression expr, float width, bool editable = false) {
				this.label = label;
				this.expr = expr;
				this.width = width;
				this.editable = editable;
			}
			public void Assign(object o, string newValueString) {
				OMU.Expression assign = new OMU.Expression(OMU.Expression.OP_ASSIGN, expr, newValueString);
				assign.Resolve(o);
			}
		}
		/// <summary>Convenience method. Finds the component here, or in a parent object.</summary>
		public static T FindComponentUpHierarchy<T>(Transform t) where T : Component {
			T found = null;
			while (t != null && found == null) { found = t.GetComponent<T>(); t = t.parent; }
			return found;
		}
		public static Canvas GetOrCreateRootCanvas(Transform t) {
			Canvas c = FindComponentUpHierarchy<Canvas>(t);
			if (!c) {
				GameObject CANVAS = new GameObject("canvas");
				c = CANVAS.AddComponent<Canvas>(); // so that the UI can be drawn at all
				c.renderMode = RenderMode.ScreenSpaceOverlay;
				if (!CANVAS.GetComponent<CanvasScaler>()) {
					CANVAS.AddComponent<CanvasScaler>(); // so that text is pretty when zoomed
				}
				if (!CANVAS.GetComponent<GraphicRaycaster>()) {
					CANVAS.AddComponent<GraphicRaycaster>(); // so that mouse can select input area
				}
				CANVAS.transform.SetParent(t);
			}
			return c;
		}
		private static RectTransform MaximizeRectTransform(Transform t) {
			return MaximizeRectTransform(t.GetComponent<RectTransform>());
		}
		private static RectTransform MaximizeRectTransform(RectTransform r) {
			r.anchorMax = Vector2.one;
			r.anchorMin = Vector2.zero;
			r.offsetMin = r.offsetMax = Vector2.zero;
			return r;
		}
		public static RectTransform GetOrCreateScrollableRect(Transform transform, out ScrollRect scrollView) {
			//Canvas c =
			//GetOrCreateRootCanvas(transform);
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
				// if (initialRectTransformSettings == null)
				// {
					MaximizeRectTransform(scrollView.transform);
				// }
				// else
				// {
				// 	RectTransform r = scrollView.GetComponent<RectTransform>();
				// 	r.anchorMin = initialRectTransformSettings.anchorMin;
				// 	r.anchorMax = initialRectTransformSettings.anchorMax;
				// 	r.offsetMin = initialRectTransformSettings.offsetMin;
				// 	r.offsetMax = initialRectTransformSettings.offsetMax;
				// }
			}
			if (scrollView.viewport == null) {
				GameObject viewport = new GameObject("viewport");
				viewport.transform.SetParent(scrollView.transform);
				Image img = viewport.AddComponent<Image>();
				img.color = new Color(0, 0, 0, 0.5f);
				Mask m = viewport.AddComponent<Mask>();
				m.showMaskGraphic = false;
				RectTransform r = MaximizeRectTransform(viewport.transform);
				r.offsetMax = new Vector2(0, 0);
				r.pivot = new Vector2(0, 1);
				r.anchorMax = Vector2.one;
				r.anchorMin = Vector2.zero;
				r.offsetMax = r.offsetMin = Vector2.zero;
				scrollView.viewport = r;
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
				rsb.offsetMin = new Vector2(-16,0);
				Scrollbar scrollbar = scrollbarObj.AddComponent<Scrollbar>();
				GameObject scrollHandle = new GameObject("handle");
				Image himg = scrollHandle.AddComponent<Image>();
				himg.color = Color.white;
				scrollHandle.transform.SetParent(scrollbarObj.transform);
				RectTransform rh = scrollHandle.GetComponent<RectTransform>();
				rh.offsetMin = new Vector2(4,0);
				rh.offsetMax = -rh.offsetMin;
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
				// ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
				// csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
				scrollView.content = r;
			}
			return scrollView.content;
		}
		public void RefreshColumnRules() {
			uiSignature = "";
			for(int i = 0; i < columnRules.Length; ++i){
				uiSignature += (columnRules[i].editable)?'e':' ';
			}
		}
		public void RefreshVisibleElements(Vector2 delta) {
			if(labeling == Labeling.alwasOnTop) {
				if(labelElement == null) {
					labelElement = new VisibleElement(this);
					labelElement.Refresh(null, -1);
					labelElement.rect.SetParent(root);
					labelElement.rect.anchoredPosition = Vector2.zero;
					labelElement.rect.transform.name = "label";
				}
				RectTransform lr = labelElement.rect;
				if(lr.sizeDelta.y != 0) {
					RectTransform sr = scrollView.GetComponent<RectTransform>();
					sr.offsetMax = new Vector2(0, -labelHeight);
				}
			}
			float width = 0, height = elementHeight*objects.Count;
			for(int i=0;i<columnRules.Length;++i) {
				width += columnRules[i].width;
			}
			if(labeling == Labeling.firstElement) { height += labelHeight; }
			rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
			if(visibleElements == null) {
				visibleElements = new List<VisibleElement>();
				rect.offsetMax = Vector2.zero;
			}
			int minShown = 0, maxShown = objects.Count-1;
			GetVisibleElementRange(out minShown, out maxShown);
			// Debug.Log("show "+minShown+" to "+maxShown);
			if(minimumObjectUse) {
				int totalShown = (maxShown-minShown)+1, ind;
				if(unusedElements == null) {
					unusedElements = new List<VisibleElement>();
				}
				if(elementsFound == null || elementsFound.Length < totalShown) {
					elementsFound = new int[totalShown];
				}
				for(int i = 0; i < totalShown; ++i) { elementsFound[i] = 0; }
				// check to see which elements are represented
				for(int i = visibleElements.Count-1; i >= 0; --i) {
					VisibleElement e = visibleElements[i];
					ind = e.Index;
					// if this visible element doesn't need to be represented, add it ot the unused list.
					// if this element is visible more than once, mark one of those visibilities as spare
					if(ind < minShown || ind > maxShown || elementsFound[ind-minShown] != 0) {
						unusedElements.Add(e); visibleElements.RemoveAt(i);
						if(e.IsSelected()) { e.Deselect(); }
					}
					// otherwise, mark that the element is correctly existing
					else { elementsFound[ind-minShown] = 1; }
				}
				for(int i = 0; i < totalShown; ++i) {
					// if there are unrepresented elements
					if(elementsFound[i] == 0) {
						VisibleElement e;
						// if there are free elements, repurpose a free element
						if(unusedElements.Count > 0) {
							int lastIndex = unusedElements.Count-1;
							e = unusedElements[lastIndex];
							unusedElements.RemoveAt(lastIndex);
						}
						// if there are no free elements, create a new one
						else {
							e = new VisibleElement(this);
						}
						int index = minShown+i;
						object o = null;
						if(index >= 0 && index < objects.Count) { o = objects[index]; }
						e.Refresh(o, minShown+i);
						visibleElements.Add(e);
					}
				}
			} else {
				if(maxShown > visibleElements.Count) {
					for(int i = visibleElements.Count; i <= maxShown; ++i) {
						VisibleElement e = new VisibleElement(this);
						e.Refresh(objects[i],i);
						visibleElements.Add(e);
					}
				}
			}
		}
		public ColumnRule[] columnRules;
		public float elementHeight, labelHeight;
		public GameObject text;
		public class VisibleElement {
			private int indexOfElement;
			public int Index{ get{return indexOfElement;} }
			UIListing manager;
			public object subject;
			public RectTransform rect;
			ICanvasElement[] columnElements;
			private string uiSignature;
			public VisibleElement(UIListing uilist) {
				manager = uilist;
				GameObject go = new GameObject("|");
				go.AddComponent<CanvasRenderer>();
				go.AddComponent<RectTransform>();
				go.transform.SetParent(manager.rect.transform);
				rect = go.GetComponent<RectTransform>();
				UPPERLEFT_ANCHOR(rect);
				//rect.sizeDelta = new Vector3(rect.sizeDelta.x, manager.elementHeight);
			}
			public bool IsSelected() {
				GameObject selected = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
				if(selected == null) return false;
				for(int i = 0; i < columnElements.Length; ++i) {
					if(columnElements[i].transform.gameObject == selected) { return true; }
				}
				return false;
			}
			public void Deselect() {
				UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
			}
			public static object PopTypeFrom(System.Type type, List<object> possibleSource) {
				object found = null;
				for(int i = 0; i < possibleSource.Count; ++i) {
					if(possibleSource[i].GetType() == type) {
						found = possibleSource[i];
						possibleSource.RemoveAt(i);
						break;
					}
				}
				return found;
			}
			/// <param>indexOfSubject</param>use -1 for the top label
			public void Refresh(object subject, int indexOfSubject) {
				this.indexOfElement = indexOfSubject;
				this.subject = subject;
				ColumnRule[] rules = manager.columnRules;
				if(columnElements == null || columnElements.Length != rules.Length
				|| uiSignature != manager.uiSignature) {
					List<object> extras = new List<object>();
					if(columnElements != null){
						extras.AddRange(columnElements);
					}
					columnElements = new ICanvasElement[rules.Length];
					float cursorX = 0;
					for(int i = 0;i<rules.Length;++i) {
						System.Type typeNeededHere = typeof(UnityEngine.UI.Text);
						if(rules[i].editable) {
							typeNeededHere = typeof(UnityEngine.UI.InputField);
						}
						columnElements[i] = PopTypeFrom(typeNeededHere, extras) as ICanvasElement;
						GameObject go;
						if(columnElements[i] == null) {
							go = new GameObject("-");
							UnityEngine.UI.Text t;
							if(typeNeededHere == typeof(UnityEngine.UI.InputField)) {
								UnityEngine.UI.InputField inf = go.AddComponent<UnityEngine.UI.InputField>();
								t = go.AddComponent<UnityEngine.UI.Text>();
								inf.textComponent = t;
								inf.onEndEdit.RemoveAllListeners();
								ColumnRule cr = rules[i];
								inf.onEndEdit.AddListener((s)=>{cr.Assign(this.subject, s);});
								columnElements[i] = inf;
							} else {
								t = go.AddComponent<UnityEngine.UI.Text>();
								columnElements[i] = t;
							}
							t.font = manager.font;
							t.color = manager.color;
							go.transform.SetParent(rect.transform);
						} else { go = columnElements[i].transform.gameObject; }
						RectTransform r = go.GetComponent<RectTransform>();
						UPPERLEFT_ANCHOR(r);
						r.anchoredPosition = new Vector2(cursorX, 0);
						r.sizeDelta = new Vector2(rules[i].width, manager.elementHeight);
						cursorX += rules[i].width;
					}
					uiSignature = manager.uiSignature;
					rect.sizeDelta = new Vector2(cursorX, manager.elementHeight);
				}
				float y = -indexOfElement * manager.elementHeight;
				if(manager.labeling == Labeling.firstElement) {
					y -= manager.labelHeight;
				}
				if(indexOfSubject >= 0) {
					if(rect.sizeDelta.y != manager.elementHeight) {
						rect.sizeDelta = new Vector2(rect.sizeDelta.x, manager.elementHeight);
						for(int i = 0; i < columnElements.Length; ++i) {
							RectTransform r = columnElements[i].transform.GetComponent<RectTransform>();
							r.sizeDelta = new Vector2(r.sizeDelta.x, manager.elementHeight);
						}
					}
					rect.anchoredPosition = new Vector2(0, y);
					for(int i = 0; i < rules.Length; ++i) {
						object label = rules[i].expr.Resolve(subject);
						InputField inf = columnElements[i].transform.GetComponent<UnityEngine.UI.InputField>();
						UnityEngine.UI.Text t = columnElements[i].transform.GetComponent<UnityEngine.UI.Text>();
						if(inf != null) {
							inf.interactable = true;
							inf.text = label.ToString();
						} else {
							t.text = label.ToString();
						}
						t.alignment = TextAnchor.UpperLeft;
					}
				} else {
					if(rect.sizeDelta.y != manager.labelHeight) {
						rect.sizeDelta = new Vector2(rect.sizeDelta.x, manager.labelHeight);
						for(int i = 0; i < columnElements.Length; ++i) {
							RectTransform r = columnElements[i].transform.GetComponent<RectTransform>();
							r.sizeDelta = new Vector2(r.sizeDelta.x, manager.labelHeight);
						}
					}
					y += manager.labelHeight-manager.elementHeight;
					rect.anchoredPosition = new Vector2(0, y);
					for(int i = 0; i < rules.Length; ++i) {
						object label = rules[i].label;
						UnityEngine.UI.Text t = columnElements[i].transform.GetComponent<UnityEngine.UI.Text>();
						t.alignment = TextAnchor.MiddleCenter;
						t.text = label.ToString();
						InputField inf = columnElements[i].transform.GetComponent<UnityEngine.UI.InputField>();
						if(inf != null) { inf.interactable = false; inf.text = t.text; }
					}
				}
			}
		}
	}

	public class TC {
		public string name, desc;
		public TC(string n, string d){name=n;desc=d;}
		public TC(){name=RandomString(3);desc=RandomString(10);}
		public static string RandomString(int length) {
			string s = "";
			for(int i = 0; i < length; ++i) {
				s += (char)(((int)'a')+Random.Range(0, 26));
			}
			return s;
		}
	}


	public RectTransform area;
	// Use this for initialization
	void Start () {
		List<object> exprname = new List<object>();
		exprname.Add("name");
		List<object> exprdesc = new List<object>();
		exprdesc.Add("desc");
		if(area == null){
			area = GetComponent<RectTransform>();
		}
		UIListing uilist = new UIListing(area);
		List<object> thingies = new List<object>();
		for(int i =0;i<200;++i) {
			thingies.Add(new TC());
		}
		uilist.SetData(thingies, 16, new UIListing.ColumnRule[]{
			new UIListing.ColumnRule("name", new OMU.Expression(OMU.Expression.OP_SCRIPTED_VALUE, "name"),50),
			new UIListing.ColumnRule("description", new OMU.Expression(OMU.Expression.OP_SCRIPTED_VALUE, "desc"),100, true)
		});
		uilist.RefreshVisibleElements(Vector2.zero);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
