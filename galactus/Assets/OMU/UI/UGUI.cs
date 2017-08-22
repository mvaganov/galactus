using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UGUI {
	public static RectTransform UPPERLEFT_ANCHOR(RectTransform r) {
		r.pivot = new Vector2(0,1);
		r.anchorMin = new Vector2(0,1);
		r.anchorMax = r.anchorMin;
		return r;
	}
	public static RectTransform ZERO_SIZE(GameObject g){RectTransform r=g.GetComponent<RectTransform>();r.sizeDelta=Vector2.zero;return r;}
	public static RectTransform MaximizeRectTransform(Transform t) {
		return MaximizeRectTransform(t.GetComponent<RectTransform>());
	}
	public static RectTransform MaximizeRectTransform(RectTransform r) {
		r.anchorMax = Vector2.one; r.anchorMin = Vector2.zero; r.offsetMin = r.offsetMax = Vector2.zero;
		return r;
	}

}
