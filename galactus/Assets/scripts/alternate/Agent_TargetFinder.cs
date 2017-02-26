using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Agent_TargetFinder : MonoBehaviour {

	[SerializeField]
	private AI_Task currentTask;
	private Agent_Sensor sensor;
	// TODO "anxiety" stat decreases time... secondsBetweenThinking = 2/(2^prop["anxiety"])?
	public float secondsBetweenThinking = 2f;
	private float timer;
	private Agent_MOB mob;
	private Agent_Properties props;
	private Agent_SizeAndEffects sizeAndEffects;
	private List<AI_Task> todo = null;

	public bool ConsiderAddingToDo(AI_Task t) {
		if (t == null) return false;
		bool changed = false;
		// if this new task is more important than what we're oding right now
		if (currentTask == null) {
			SwitchActivityTo (t);
			return true;
		} else if (t.GetScore () > currentTask.GetScore ()) {
			AI_Task oldTask = currentTask;
			SwitchActivityTo (t);
			t = oldTask;
			changed = true;
		}
		// if we have task memory
		float mem = this ["taskMemory"];
		if (mem > 0) {
			if (todo == null) {
				todo = new List<AI_Task> ();
			}
			// check if this task is already in here
			int existingIndex = todo.FindIndex (delegate(AI_Task obj) {
				return t.GetType() == obj.GetType() && obj.GetTarget() == t.GetTarget();
			});
			// if the task isn't already known, add this task to task memory, in order of importance
			if(existingIndex < 0) {
				if (mem > todo.Count) {
					todo.Add (t);
					changed = true;
				} else {
					float s = t.GetScore ();
					if (s >= todo [todo.Count - 1].GetScore ()) {
						for (int i = 0; i < todo.Count; ++i) {
							if (todo [i].GetScore () < s) {
								todo.Insert (i, t);
								changed = true;
								break;
							}
						}
					}
	//				SortTasks ();
					ReEvaluateTODO ();
					RemoveExtraTasks ();
	//				string str = "";
	//				todo.ForEach (obj => { str += ((str.Length>0)?",\n":"")+obj.ToString(); });
//					print ("TODO:[" + PrintTasks("\n") + "\n]");
				}
			}
		}
		return changed;
	}

	public string PrintTasks(string separator) {
		string str = "";
		todo.ForEach (obj => { str += ((str.Length>0)?separator:"")+obj.ToString(); });
		return str;
	}
	public void ClearInvalidTasks() { todo.RemoveAll (delegate(AI_Task obj) { return !obj.IsValid(); }); }
	public void RecalculateTasks() {
//		print ("PRECALC:[\n" + PrintTasks("\n") + "\n]");
		todo.ForEach (delegate(AI_Task obj) { obj.SetScore(); });
//		print ("POSTCALC:[\n" + PrintTasks("\n") + "\n]");
	}
	public void SortTasks() { 
//		print ("PRESORT:[\n" + PrintTasks("\n") + "\n]");
		todo.Sort (delegate(AI_Task a, AI_Task b) { return b.GetScore() - a.GetScore(); });
//		print ("POSTSORT:[\n" + PrintTasks("\n") + "\n]");
	}
	public void RemoveExtraTasks() {
		float mem = this ["taskMemory"];
		while (todo.Count > mem) {
			todo.RemoveAt (todo.Count-1);
		}
	}


	public void ReEvaluateTODO() {
		ClearInvalidTasks ();
		RecalculateTasks ();
		SortTasks ();
//		print ("????:\n[\n" + PrintTasks("\n") + "\n]");
	}

	public AI_Task CalculateMostImportantTask() {
		AI_Task t = null;
		if (todo != null) {
			// TODO add to the currentTask priority weight based on ["focus"] stat
			if (currentTask != null) {
				todo.Add (currentTask);
//				print ("----:\n[\n" + PrintTasks("\n") + "\n]");
			}
			ReEvaluateTODO ();
			if (todo.Count > 0) {
				t = todo [0];
				todo.RemoveAt (0);
			} else {
				t = null;
			}
		}
		currentTask = t;
		return t;
	}

	public float GetRadius() {
		return transform.localScale.z;
	}

	public void Reset() { currentTask = null; }

	// Use this for initialization
	void Start () {
		mob = GetComponent<Agent_MOB> ();
		props = GetComponent<Agent_Properties> ();
		sizeAndEffects = GetComponent<Agent_SizeAndEffects> ();
		sensor = GetComponent<Agent_Sensor> ();
		sensor.EnsureOwnerIsKnown ();
		sensor.sensorUpdateTime = secondsBetweenThinking;
	}
	
	public Agent_MOB GetMob() { return mob; }
	public Agent_Properties GetProperties() { return props; }
	public Agent_Sensor GetSensor() { return sensor; }
	public Agent_SizeAndEffects GetSizeAndEffects() { return sizeAndEffects; }

	public void SwitchActivityTo(AI_Task nextThingToDo) {
		if (currentTask!=null) { currentTask.Exit (); }
		currentTask = nextThingToDo;
		if (currentTask!=null) { currentTask.Enter (); }
	}

	// TODO make this acynchronous
	void FixedUpdate() {
		// every few seconds, look around, and evaluate if something you see is more desireable to do than what is being done
		timer -= Time.deltaTime;
		if (timer <= 0) {
			timer = secondsBetweenThinking;
			AI_Task whatIsInFrontOfMe = AI_Plan.BestThingToDoInFrontOfMe (this);
//			print (whatIsInFrontOfMe+"?");
			if(currentTask == null || 
				whatIsInFrontOfMe != null && whatIsInFrontOfMe.GetScore() > currentTask.GetScore()) {
				ConsiderAddingToDo (whatIsInFrontOfMe);
				//SwitchActivityTo (whatIsInFrontOfMe);
			}
		}
		// if we're doing something, keep doing it
		if (currentTask != null && currentTask.IsValid()) {
			currentTask.Execute ();
		} else {
			if (currentTask != null) { SwitchActivityTo (null); }
			// otherwise, do the most important thing we know we have to do.
			SwitchActivityTo(CalculateMostImportantTask ());
			// and if there is no important task to do, look for something to do!
			if(currentTask == null) {
				SwitchActivityTo (new AI_Searching(this));//AI_Task.plan);
			}
			//Debug.Log ("best task to do?: "+currentTask);
		}
	}

	public float this[string i] {
		get{ return props[i]; }
		set{ props [i] = value; }
	}
}
