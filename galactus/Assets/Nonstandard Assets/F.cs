using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NS {
/// This class serves as a store for static Utility functions until a more suitable class/namespace can be developed
public class F {
	public static void DoActivate(
		object whatToActivate,
		object causedActivate,
		object doingActivate,
		bool activate,
		float delayInSeconds
	) {
		if (delayInSeconds <= 0) {
			DoActivate (whatToActivate, causedActivate, doingActivate, activate);
		} else {
			NS.Timer.setTimeout (() => {
				DoActivate (whatToActivate, causedActivate, doingActivate, activate);
			}, (long)(delayInSeconds * 1000));
		}
	}

	//public static void DoActivate(object whatToActivate) { DoActivate(whatToActivate, null, null, true); }
	public static void DoActivate(
		object whatToActivate,	// what needs to be activated
		object causedActivate,	// what triggered the activation
		object doingActivate,	// what is doing the activation
		bool activate			// whether this is an activate or deactivate request
	) {
		if (whatToActivate == null) { Debug.LogError ("Don't know how to activate null"); return; }
		if (whatToActivate is EditorGUIObjectReference) {
			whatToActivate = ((EditorGUIObjectReference)whatToActivate).data;
		}
		System.Type type = whatToActivate.GetType ();
		if (typeof(System.Action).IsAssignableFrom (type)) {
			System.Action a = whatToActivate as System.Action;
			a.Invoke ();
		} else if (typeof(UnityEngine.Events.UnityEvent).IsAssignableFrom (type)) {
			UnityEngine.Events.UnityEvent a = whatToActivate as UnityEngine.Events.UnityEvent;
			a.Invoke ();
		} else if (type == typeof(Transform)) {
			Transform targetLocation = ConvertToTransform(whatToActivate);
			Transform toMove = ConvertToTransform(causedActivate);
			if (toMove != null) {
				toMove.position = targetLocation.position;
			}
		// } else if (type == typeof(NS.SceneField)) {
		// 	NS.SceneField scene = whatToActivate as NS.SceneField;
		// 	scene.LoadLevel (activate);
		} else if (type == typeof(AudioClip) || type == typeof(AudioSource)) {
			AudioSource asource = null;
			if (type == typeof(AudioSource)) {
				asource = whatToActivate as AudioSource;
			}
			if (asource == null) {
				GameObject go = ConvertToGameObject(doingActivate);
				if(go != null) {
					asource = go.AddComponent<AudioSource> ();
				} else {
					throw new System.Exception("can't create audio without a game object to put it on.");
				}
			}
			if (type == typeof(AudioClip)) {
				asource.clip = whatToActivate as AudioClip;
			}
			if (activate) {
				asource.Play ();
			} else {
				asource.Stop ();
			}
		} else if (type == typeof(ParticleSystem)) {
			ParticleSystem ps = whatToActivate as ParticleSystem;
			if (activate) {
				Transform t = ps.transform;
				GameObject go = ConvertToGameObject(doingActivate);
				t.position = go.transform.position;
				t.rotation = go.transform.rotation;
				ParticleSystem.ShapeModule sm = ps.shape;
				if (sm.shapeType == ParticleSystemShapeType.Mesh) {
					sm.mesh = go.GetComponent<MeshFilter> ().mesh;
					sm.scale = go.transform.lossyScale;
				} else if (sm.shapeType == ParticleSystemShapeType.MeshRenderer) {
					sm.meshRenderer = go.GetComponent<MeshRenderer> ();
				}
				ps.Play ();
			} else {
				ps.Stop ();
			}
		} else if (type == typeof(GameObject)) {
			(whatToActivate as GameObject).SetActive (activate);
		} else if (type == typeof(UnityEngine.Material)) {
			GameObject go = ConvertToGameObject(doingActivate);
			RememberedOriginalMaterial r = go.GetComponent<RememberedOriginalMaterial> ();
			if (activate) {
				Material m = whatToActivate as Material;
				if (r == null) {
					r = go.AddComponent<RememberedOriginalMaterial> ();
					r.oldMaterial = go.GetComponent<Renderer> ().material;
				}
				go.GetComponent<Renderer> ().material = m;
			} else {
				if (r != null) {
					go.GetComponent<Renderer> ().material = r.oldMaterial;
					GameObject.Destroy (r);
				}
			}
		} else if (typeof(IEnumerable).IsAssignableFrom (type)) {
			IEnumerable ienum = whatToActivate as IEnumerable;
			IEnumerator iter = ienum.GetEnumerator ();
			while (iter.MoveNext ()) {
				DoActivate (iter.Current, causedActivate, doingActivate, activate);
			}
		} else if (type == typeof(Animation)) {
			if (activate) {
				(whatToActivate as Animation).Play ();
			} else {
				(whatToActivate as Animation).Stop();
			}
		} else {
			System.Reflection.MethodInfo[] m = type.GetMethods ();
			bool invoked = false;
			for (int i = 0; i < m.Length; ++i) {
				System.Reflection.MethodInfo method = m[i];
				if ((activate && method.Name == "DoActivateTrigger")
				|| (!activate && method.Name == "DoDeactivateTrigger")) {
					if(method.GetParameters().Length == 0) {
						method.Invoke (whatToActivate, new object[]{ });
						invoked = true;
					} else if(method.GetParameters().Length == 1) {
						method.Invoke (whatToActivate, new object[]{ causedActivate });
						invoked = true;
					} else if(method.GetParameters().Length == 2) {
						method.Invoke (whatToActivate, new object[]{ causedActivate, doingActivate });
						invoked = true;
					}
					break;
				}
			}
			if(!invoked) {
				Debug.LogError ("Don't know how to "+((activate)?"DoActivateTrigger":"DoDeactivateTrigger")+" a \'" + type + "\'");
			}
		}
	}
	public class RememberedOriginalMaterial : MonoBehaviour { public Material oldMaterial; }

	/// used to handle pause/unpause behavior
	public delegate void BooleanAction(bool b);
	public static void EquateUnityEditorPauseWithApplicationPause(BooleanAction b) {
		#if UNITY_EDITOR
		// This method is run whenever the playmode state is changed.
		UnityEditor.EditorApplication.pauseStateChanged += (UnityEditor.PauseState ps) => {
			b(ps == UnityEditor.PauseState.Paused);
		};
		#endif
	}

	public static GameObject ConvertToGameObject(object obj) {
		if(obj is GameObject) { return obj as GameObject; }
		if(obj is Component) { return (obj as Component).gameObject; }
		if(obj is Collision) { return (obj as Collision).gameObject; }
		if(obj is Collision2D) { return (obj as Collision2D).gameObject; }
		return null;
	}

	public static Transform ConvertToTransform(object obj) {
		if(obj is Transform) { return obj as Transform; }
		GameObject go = ConvertToGameObject(obj);
		if(go != null) { return go.transform; }
		return null;
	}
}
}