using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Recycle, Reduce, Reuse
public class R3 {
	public interface Reusable{void Recycle();} 
	private static List<object> oPool = new List<object>();
	private static GameObject recyclebin = null;

	public static object Get(System.Type type) {
		object o=null;for(int i=0;i<oPool.Count;++i){if(oPool[i].GetType()==type){o=oPool[i];oPool.RemoveAt(i);break;}}
		if(o != null && o is Component){Component c=o as Component;c.gameObject.SetActive(true);}
		return o;
        // above is presumably faster than: Get((obj) => { obj.GetType() == type});
	}
    public delegate bool Condition(object obj);
    public static object Get(Condition howToTellIfThisIsThisGood) {
        object o = null; for (int i = 0; i < oPool.Count; ++i) {
            if (howToTellIfThisIsThisGood(oPool[i])) { o = oPool[i]; oPool.RemoveAt(i); break; }
        }
        if (o != null && o is Component) { Component c = o as Component; c.gameObject.SetActive(true); }
        return o;
    }
    /// <param name="o">automatcally calls Recycle() if it's a Reusable object.</param>
	public static void Add(object o) {
		if(oPool.IndexOf(o) != -1) {throw new System.Exception("DOUBLE FREE!?!");}
		if(o is R3.Reusable){(o as R3.Reusable).Recycle();}
		if(o is Component){
			Component c=o as Component;UGUI.ZERO_SIZE(c.gameObject);c.gameObject.SetActive(false);
			if(recyclebin==null){recyclebin=new GameObject("<recyclebin>");}
			if(c.transform.parent != null){throw new System.Exception("why was "+c.GetType()+" still connected to a parent?");}
			c.transform.SetParent(recyclebin.transform);
		}
		oPool.Add(o);
	}
    /// <param name="list">List. automatcally calls Recycle() on Reusable objects.</param>
	public static void AddRange(IList list){if(list==null)return;for(int i=0;i<list.Count;++i){Add(list[i]);}}
}
