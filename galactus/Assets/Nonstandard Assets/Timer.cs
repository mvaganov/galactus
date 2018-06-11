using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// author: mvaganov@hotmail.com
// license: Copyfree, public domain. This is free code! Great artists, steal this code!
namespace NS {
	/* // example code:
	NS.Timer.setTimeout (() => {
		Debug.Log("This printed 3 seconds after setTimeout was called!");
	}, 3000);
	*/
	public class Timer : Chrono {
		[Tooltip("When to trigger")]
		public float seconds = 1;
		[Tooltip("Transform to teleport to\nSceneAsset to load a new scene\nAudioClip to play audio\nGameObject to SetActivate(true)")]
		public EditorGUIObjectReference whatToActivate = new EditorGUIObjectReference();
		[Tooltip("If true, restart a timer after triggering")]
		public bool repeat = false;

		private void DoTimer() {
			if (repeat) {
				SetTimeout (DoTimer, (long)(seconds * 1000));
			}
			SetTimeout (whatToActivate.data, (long)(seconds * 1000));
		}

		void Start() {
			base.Init ();
			if(whatToActivate.data != null) { DoTimer(); }
		}
	}

	public class Chrono : MonoBehaviour {
		[Tooltip("keeps track of how long each update takes. It longer than this, stop executing events and do them later. less than 0 for no limit.")]
		public int maxMillisecondsPerUpdate = 100;

		[System.Serializable]
		public class ToDo {
			public string description;
			/// <summary>Unix Epoch Time Milliseconds</summary>
			public long when;
			public object what;
			/// <summary>whether or not to Do or UNdo</summary>
			public bool activate = true;
			/// <summary>what could be a delegate, or an executable object, as executed by a Trigger</summary>
			public ToDo(long when, object what, string description = null) {
				if (description == null) {
					if (typeof(System.Action).IsAssignableFrom(what.GetType())) {
						System.Action a = what as System.Action;
						description = a.Method.Name;
					} else {
						description = what.ToString();
					}
				}
				this.description = description; this.when = when; this.what = what;
			}
		}
		/// <summary>using a List, which is contiguous memory, because it's faster than a liked list MOST of time, because of cache misses, and reasonable data loads</summary>
		public List<ToDo> queue = new List<ToDo>();
		/// <summary>The singleton</summary>
		private static NS.Chrono s_instance = null;
		public static Chrono Instance() {
			if (s_instance == null) {
				Object[] objs = FindObjectsOfType(typeof(NS.Chrono));  // find the instance
				for (int i = 0; i < objs.Length; ++i) {
					if (objs[i].GetType () == typeof(NS.Chrono)) {
						s_instance = objs [i] as NS.Chrono; break;
					}
				}
				if(s_instance == null) { // if it doesn't exist
					GameObject g = new GameObject("<" + typeof(NS.Chrono).Name + ">");
					s_instance = g.AddComponent<NS.Chrono>(); // create one
				}
			}
			return s_instance;
		}
		/// <summary>While this is zero, use system time. As soon as time becomes perturbed, by pause or time scale, keep track of game-time. To reset time back to realtime, use SynchToRealtime()</summary>
		private long alternativeTime = 0;
		/// <summary>The timer counts in milliseconds, Unity measures in fractions of a second. This value reconciles fractional milliseconds.</summary>
		private float leftOverTime = 0;

		public static long NowRealtime() { return System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond; }

		public long Now() { return alternativeTime == 0 ? NowRealtime() : alternativeTime; }

		public static long now() { return Instance ().Now (); }

		public void SyncToRealtime() { alternativeTime = 0; }

		private int BestIndexFor(long soon) {
			int index = 0;
			for (index = 0; index < queue.Count; ++index) {
				if (queue [index].when > soon) break;
			}
			return index;
		}

		/// <summary>as the JavaScript function</summary>
		/// <param name="action">Action. an object to trigger, expected to be a delegate or System.Action</param>
		/// <param name="delayMilliseconds">Delay milliseconds.</param>
		public void SetTimeout(System.Action action, long delayMilliseconds) {
			SetTimeout ((object)action, delayMilliseconds);
		}
		/// <summary>as the JavaScript function</summary>
		/// <param name="action">Action. an object to trigger, expected to be a delegate or System.Action</param>
		/// <param name="delayMilliseconds">Delay milliseconds.</param>
		public void SetTimeout(object action, long delayMilliseconds) {
			long soon = Now () + delayMilliseconds;
			queue.Insert(BestIndexFor (soon), new ToDo(soon, action));
		}

		/// <param name="action">Action. what to do</param>
		/// <param name="delayMilliseconds">Delay milliseconds. in how-many-milliseconds to do it</param>
		public static void setTimeout(object action, long delayMilliseconds) {
			setTimeout ((object)action, delayMilliseconds);
		}
		/// <summary>Allows implicit conversion of lambda expressions and delegates</summary>
		/// <param name="action">Action. what to do</param>
		/// <param name="delayMilliseconds">Delay milliseconds. in how-many-milliseconds to do it</param>
		public static void setTimeout(System.Action action, long delayMilliseconds) {
			Instance ().SetTimeout (action, delayMilliseconds);
		}

		void OnApplicationPause(bool paused) { if (alternativeTime == 0) { alternativeTime = Now (); } }
		void OnDisable() { OnApplicationPause (true); }
		void OnEnable() { OnApplicationPause (false); }

		protected void Init() {
			NS.F.EquateUnityEditorPauseWithApplicationPause (OnApplicationPause);
		}

		void Start () {
			Init ();
			if (s_instance != null && s_instance != this) { throw new System.Exception ("there should only be one timer!"); }
			s_instance = this;
		}

		void Update () {
			long now;
			if (alternativeTime == 0) {
				now = Now ();
				if (UnityEngine.Time.timeScale != 1) { alternativeTime = now; }
			} else {
				float deltaTimeMs = (UnityEngine.Time.deltaTime * 1000);
				long deltaTimeMsLong = (long)(deltaTimeMs + leftOverTime);
				alternativeTime += deltaTimeMsLong;
				leftOverTime = deltaTimeMs - deltaTimeMsLong;
				now = alternativeTime;
			}
			if (queue.Count > 0 && queue [0].when <= now) {
				// the things to do in the toDoRightNow might add to the queue, so to prevent infinite looping...
				// separate out the elements to do right now
				List<ToDo> toDoRightNow = new List<ToDo> ();
				for (int i = 0; i < queue.Count; ++i) {
					if (queue [i].when > now) { break; }
					toDoRightNow.Add (queue [i]);
				}
				queue.RemoveRange (0, toDoRightNow.Count);
				long nowReally = NowRealtime ();
				// do what is scheduled to do right now
				for (int i = 0; i < toDoRightNow.Count; ++i) {
					ToDo todo = toDoRightNow [i];
					// if todo.what adds to the queue, it won't get executed this cycle
					NS.F.DoActivate(todo.what, gameObject, gameObject, todo.activate);
					// if it took too long to do that thing, stop and hold the rest of the things to do till later.
					if (maxMillisecondsPerUpdate >= 0 && ((NowRealtime() - nowReally) > maxMillisecondsPerUpdate)) {
						toDoRightNow.RemoveRange (0, i+1);
						queue.InsertRange (0, toDoRightNow);
						// toDoRightNow.Clear (); // not strictly necessary, but could be useful if more code is added to this method.
						break;
					}
				}
			}
		}
	}
}
