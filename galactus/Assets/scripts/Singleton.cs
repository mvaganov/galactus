using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Singleton {

	private static Dictionary<System.Type, MonoBehaviour> instances = new Dictionary<System.Type, MonoBehaviour>();

	public static T Get<T>() where T : MonoBehaviour {
		MonoBehaviour instance;
		if(!instances.TryGetValue(typeof(T), out instance)) {
			if((instance = GameObject.FindObjectOfType<T>() as T) == null) {
				GameObject g = new GameObject();
				instance = g.AddComponent<T>();
				g.name = "<" + instance.GetType().Name + ">";
				instances [typeof(T)] = instance;
			}
		}
		return instance as T;
	}
}
