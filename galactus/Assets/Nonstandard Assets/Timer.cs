using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// author: mvaganov@hotmail.com
// license: Copyfree, public domain. This is free code! Great artists, steal this code!
namespace NS {
	/* // example code:
	NS.Timer.setTimeout (() => {
		Debug.Log("This will print 3 seconds after setTimeout was called!");
	}, 3000);
	*/
	public class Timer : Chrono {
		[Tooltip("When to trigger")]
		public float seconds = 1;
		[Tooltip("Transform to teleport to\nSceneAsset to load a new scene\nAudioClip to play audio\nGameObject to SetActivate(true)")]
		public ObjectPtr whatToActivate = new ObjectPtr();
		[Tooltip("If true, restart a timer after triggering")]
		public bool repeat = false;

		private void DoTimer() {
			if (repeat) {
				SetTimeout (DoTimer, (long)(seconds * 1000));
			}
			SetTimeout (whatToActivate.Data, (long)(seconds * 1000));
		}

		void Start() {
			base.Init ();
			if(whatToActivate.Data != null) { DoTimer(); }
		}

		/// <summary>Allows implicit conversion of lambda expressions and delegates. Same as Chrono.setTimeout, this method is here to prevent warnings.</summary>
		/// <param name="action">Action. what to do</param>
		/// <param name="delayMilliseconds">Delay milliseconds. in how-many-milliseconds to do it</param>
		new public static void setTimeout(System.Action action, long delayMilliseconds) {
			Chrono.setTimeout(action, delayMilliseconds);
		}
	}

	public class Chrono : MonoBehaviour {
		/// <summary>The singleton</summary>
		private static NS.Chrono s_instance = null;
		[Tooltip("keeps track of how long each update takes. If a timer-update takes longer than this, stop executing events and do them later. less than 0 for no limit.")]
		public int maxMillisecondsPerUpdate = 100;
		/// <summary>using a List, which is contiguous memory, because it's faster than a liked list MOST of time, because of cache misses, and reasonable data loads</summary>
		public List<ToDo> queue = new List<ToDo>();
		public List<ToDo> queueRealtime = new List<ToDo>();
		/// <summary>While this is zero, use system time. As soon as time becomes perturbed, by pause or time scale, keep track of game-time. To reset time back to realtime, use SynchToRealtime()</summary>
		private long alternativeTime = 0;
		/// <summary>The timer counts in milliseconds, Unity measures in fractions of a second. This value reconciles fractional milliseconds.</summary>
		private float leftOverTime = 0;
		/// <summary>if actions are interrupted, probably by a deadline, this keeps track of what was being done</summary>
		private List<ToDo> _currentlyDoing = new List<ToDo>();
		private int currentlyDoneThingIndex = 0;

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

		/// <returns>The realtime, as milliseconds since Jan 1 1970.</returns>
		public static long NowRealtime() { return System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond; }

		/// <returns>game time right now (modified by pausing or Time.timeScale</returns>
		public long Now() { return (alternativeTime == 0) ? NowRealtime() : alternativeTime; }

		/// <returns>game time right now (modified by pausing or Time.timeScale</returns>
		public static long now() { return Instance ().Now (); }

		/// <summary>clears the difference between game time and real time</summary>
		public void SyncToRealtime() { alternativeTime = 0; }

		private int BestIndexFor(long soon, List<ToDo> a_queue) {
			int index = 0;
			for (index = 0; index < a_queue.Count; ++index) {
				if (a_queue[index].when > soon) break;
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
			queue.Insert(BestIndexFor (soon, queue), new ToDo(soon, action));
		}

		/// <summary>as the JavaScript function</summary>
		/// <param name="action">Action. an object to trigger, expected to be a delegate or System.Action</param>
		/// <param name="delayMilliseconds">Delay milliseconds.</param>
		public void SetTimeoutRealtime(object action, long delayMilliseconds) {
			long soon = NowRealtime() + delayMilliseconds;
			queueRealtime.Insert(BestIndexFor(soon, queueRealtime), new ToDo(soon, action));
		}

		/// <param name="action">Action. what to do</param>
		/// <param name="delayMilliseconds">Delay milliseconds. in how-many-milliseconds to do it</param>
		public static void setTimeout(object action, long delayMilliseconds) {
			Instance ().SetTimeout (action, delayMilliseconds);
		}

		/// <param name="action">Action. what to do</param>
		/// <param name="delayMilliseconds">Delay milliseconds. in how-many-milliseconds to do it</param>
		public static void setTimeoutRealtime(object action, long delayMilliseconds) {
			Instance().SetTimeoutRealtime(action, delayMilliseconds);
		}

		/// <summary>Allows implicit conversion of lambda expressions and delegates</summary>
		/// <param name="action">Action. what to do</param>
		/// <param name="delayMilliseconds">Delay milliseconds. in how-many-milliseconds to do it</param>
		public static void setTimeout(System.Action action, long delayMilliseconds) {
			Instance ().SetTimeout (action, delayMilliseconds);
		}

		/// <summary>Allows implicit conversion of lambda expressions and delegates</summary>
		/// <param name="action">Action. what to do</param>
		/// <param name="delayMilliseconds">Delay milliseconds. in how-many-milliseconds to do it</param>
		public static void setTimeoutRealtime(System.Action action, long delayMilliseconds) {
			Instance().SetTimeoutRealtime(action, delayMilliseconds);
		}

		void OnApplicationPause(bool paused) { if (alternativeTime == 0) { alternativeTime = Now (); } }
		void OnDisable() { OnApplicationPause (true); }
		void OnEnable() { OnApplicationPause (false); }

		protected void Init() {
			NS.ActivateAnything.EquateUnityEditorPauseWithApplicationPause (OnApplicationPause);
		}

		void Start () {
			Init ();
			if (s_instance != null && s_instance != this) { throw new System.Exception ("there should only be one timer!"); }
			s_instance = this;
		}

		void Update () {
			long now_t, nowForReals = NowRealtime();
			long deadline = nowForReals + maxMillisecondsPerUpdate;
			if(queueRealtime.Count > 0) {
				DoWhatIsNeededNow(queueRealtime, nowForReals, deadline);
			}
			if(queue.Count > 0) {
				if(alternativeTime == 0) {
					now_t = nowForReals;
					if(UnityEngine.Time.timeScale != 1) { alternativeTime = now_t; }
				} else {
					float deltaTimeMs = (UnityEngine.Time.deltaTime * 1000);
					long deltaTimeMsLong = (long)(deltaTimeMs + leftOverTime);
					alternativeTime += deltaTimeMsLong;
					leftOverTime = deltaTimeMs - deltaTimeMsLong;
					now_t = alternativeTime;
				}
				DoWhatIsNeededNow(queue, now_t, deadline);
			}
		}
		void DoWhatIsNeededNow(List<ToDo> a_queue, long now_t, long deadline) {
			bool tryToDoMore;
			do {
				tryToDoMore = false;
				if(a_queue.Count > 0 && a_queue[0].when <= now_t) {
					if(_currentlyDoing.Count == 0) {
						// the things to do in the toDoRightNow might add to the queue, so to prevent infinite looping...
						// separate out the elements to do right now
						for(int i = 0; i < a_queue.Count; ++i) {
							if(a_queue[i].when > now_t) { break; }
							_currentlyDoing.Add(a_queue[i]);
						}
						// if there's nothing to do, get out of this potential loop
						if(_currentlyDoing.Count == 0) { break; }
						a_queue.RemoveRange(0, _currentlyDoing.Count);
						tryToDoMore = false;
					}
					// do what is scheduled to do right now
					while(currentlyDoneThingIndex < _currentlyDoing.Count) {
						ToDo todo = _currentlyDoing[currentlyDoneThingIndex++];
						// if todo.what adds to the queue, it won't get executed this cycle
						NS.ActivateAnything.DoActivate(todo.what, gameObject, gameObject, todo.activate);
						// if it took too long to do that thing, stop and hold the rest of the things to do till later.
						if(maxMillisecondsPerUpdate >= 0 && NowRealtime() > deadline) {
							break;
						}
					}
					if(currentlyDoneThingIndex >= _currentlyDoing.Count) {
						_currentlyDoing.Clear();
						currentlyDoneThingIndex = 0;
						tryToDoMore = NowRealtime() < deadline && a_queue.Count > 0;
					}
				}
			} while(tryToDoMore);
		}
	}
}
