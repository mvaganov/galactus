using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODO visualizations...

public abstract class AI_Task {
	/// <summary>what Agent should this task be calculated for?</summary>
	protected Agent_TargetFinder self;
	protected float score;
	public AI_Task(Agent_TargetFinder self) { Set(self,null); }
	public AI_Task(Agent_TargetFinder self, float score) { Set(self,null,score); }
	public virtual void SetSelf(Agent_TargetFinder self) { this.self = self; }
	public virtual Agent_TargetFinder GetSelf() { return self; }
	public virtual int GetScore() { return (int)(score*128); }
	public virtual void SetScore(float score) { this.score = score; }
	public virtual void SetScore() { SetScore(CalculateFitnessScore()); }
	public virtual Transform GetTarget() { return null; }
	public virtual void SetTarget(Transform t) { }
	public virtual void Set(Agent_TargetFinder self, Transform target, float score) {
		SetSelf(self); SetTarget (target); SetScore (score);
	}
	public virtual void Set(Agent_TargetFinder self, Transform target) {
		Set(self, target, 0);
		if(IsValid()){
			SetScore (); // calculate the score after self and target are set
		}
	}
	public enum Targetability { self, yes, manyOthers }
	public virtual Targetability GetTargetability() { return Targetability.self; }
	public virtual bool IsValid(ref string whyNot) { whyNot = "incomplete AI"; return false; }
	public virtual bool IsValid() { string s = ""; return IsValid(ref s); }
	/// <summary></summary>
	/// <returns>The fitness score. Very negative number by default. Not float.MIN, because that causes weird issues when scores are compared</returns>
	public virtual float CalculateFitnessScore() { return -32768; }
	public virtual bool IsSteering() { return false; }
	public virtual void GetSteering(ref Vector3 move, ref Vector3 look) { }
	public AI_Task Clone(){ return this.MemberwiseClone () as AI_Task; }

	public override string ToString () {
		return "["
			+"("+((int)(GetScore()))+")"
			+(GetTarget()!=null?GetTarget().name:"")
			+"<"+GetDescription()+">"
			+"]";
	}
	public abstract void Execute ();
	public virtual void Enter (){ }
	public virtual void Exit (){
		if (drawnElements != null) { ClearLineElementsBeyond (0); }
	}
	public abstract string GetDescription ();
	public virtual void DrawLogic(ref List<GameObject> drawnElements) { }

	private List<GameObject> drawnElements = null;
	// TODO create a proper memory pool system for DrawLogic code
	//public GameObject LineElement(int index) {
	//	if (drawnElements == null) { drawnElements = new List<GameObject> (); }
	//	GameObject o = null;
	//	if (index == drawnElements.Count) {
	//		drawnElements.Add (Effects.Line (ref o));
	//	} else { o = drawnElements [index]; }
	//	return o;
	//}
	public void SetLineElement(int index, GameObject g){
		if (drawnElements [index] == null || g != drawnElements[index]) {
			throw new UnityException ("["+g+"] seems bad bro. why didn't the draw object initialize here? ?"+drawnElements[index]+"?");
		}
		drawnElements [index] = g;
	}
	protected void ClearLineElementsBeyond(int index) {
		for (int i = drawnElements.Count - 1; i >= index; --i) {
			MemoryPoolItem.Destroy (drawnElements [i]);
			drawnElements.RemoveAt (i);
		}
	}
	public virtual void Draw() {
		if (drawnElements == null) { drawnElements = new List<GameObject> (); }
		DrawLogic (ref drawnElements);
//		for(int a=0;a<drawnCount;++a){drawnElements[a].SetActive(true);}
//		for(int a=drawnCount;a<drawnElements.Length;++a){drawnElements[a].SetActive(false);}
//		return drawnCount;
	}
};

public class AgentImagination {
	protected Dictionary<System.Type,AI_Task> imaginary = new Dictionary<System.Type, AI_Task>();
	public AgentImagination(Agent_TargetFinder self) { Initialize (self); }
	public void Initialize(Agent_TargetFinder self) {
//		Debug.Log ("create AI for ("+self+")");
		AI_Task[] ai = {
			new AI_Harvest(self,null),
			new AI_Flee(self,null),
			new AI_Attack(self,null),
			new AI_Nothing (self),
			new AI_Searching(self),
			new AI_Plan (self),
		};
		// TODO write datastructure that figures out which personality traits map to which events.
		// example: greed->ai_harvest, thread->ai_flee,ai_attack
		for(int i=0;i<ai.Length;++i) {
			imaginary [ai[i].GetType()] = ai[i];
		}
	}
	public float ImagineScoreFor<TYPE>(Agent_TargetFinder self, Transform target) {
		AI_Task ai = imaginary[typeof(TYPE)];
		string whyNot = "";
		if (ai.GetTargetability () == AI_Task.Targetability.yes) {
			ai.Set (self, target);
			if (ai.IsValid (ref whyNot)) {
				return ai.CalculateFitnessScore ();
			}
		}
		throw new UnityException (self+" can't imagine a "+typeof(TYPE)+" score using "+target+
			((whyNot.Length>0)?": "+whyNot:"."));
	}
	public AI_Task GetBestDecision(Agent_TargetFinder self, Transform target, List<string> excuses = null) {
		string whyNot;
		AI_Task best = null;
		foreach (var kvp in imaginary) {
			AI_Task ai = kvp.Value;
			if (ai.GetTargetability () == AI_Task.Targetability.yes) {
				ai.Set (self, target, 0);
				whyNot = "";
				if (ai.IsValid (ref whyNot)) {
					ai.SetScore ();
//					Debug.Log (ai);
					if (best == null || ai.GetScore () > best.GetScore ()) {
						best = ai;
					}
				} else if (excuses != null) {
					excuses.Add ("FAILED: "+ai+": "+whyNot);
				}
			}
		}
		return (best != null)?best.Clone ():null;
	}
}

public class AI_TargetTask : AI_Task {
	public override AI_Task.Targetability GetTargetability() { return AI_Task.Targetability.yes; }
	public AI_TargetTask(Agent_TargetFinder self, Transform target, float score):base(self,score){
		this.target = target;
	}
	public AI_TargetTask(Agent_TargetFinder self, Transform target):base(self){ this.target = target; }
	protected Transform target;
	protected int targetIncarnation;
	public override void SetTarget (Transform t) { target = t; targetIncarnation = t?MemoryPoolItem.GetIncarnation (t.gameObject):-1; }
	public virtual bool IsTargetConsistent() {
		if (target && MemoryPoolItem.GetIncarnation (target.gameObject) != targetIncarnation) {
			target = null;
		}
		return target != null;
	}
	public override bool IsValid(ref string whyNot) {
		if (target == null) { whyNot = "no target"; return false; }
		if (target == self.transform) { whyNot = "target is self"; return false; }
		if (!IsTargetConsistent ()) { whyNot = "inconsistency with target"; return false; }
		return true;
	}
	public override Transform GetTarget () { return target; }
	public override bool IsSteering() { return true; }
	public override void GetSteering(ref Vector3 move, ref Vector3 look) { 
		Vector3 d = (target.position - self.transform.position);
		move = look = d.normalized;
	}
	public override void Execute(){}
	public override string GetDescription () { return "target"; }
	public override void DrawLogic(ref List<GameObject> drawnElements) {
		//Lines.MakeCircle_With (LineElement (0), target.position, Camera.main.transform.forward, Color.white, 
		//	target.transform.lossyScale.z).name="<T>";
	}
}

public class AI_Nothing : AI_Task {
	public AI_Nothing(Agent_TargetFinder self):base(self){}
	public override string GetDescription() { return "nothing"; }
	public override float CalculateFitnessScore() { return 0 + self["idleness"]; }
	public override void Execute(){ }
}

// ?greed, ?harvest, ?laziness, ?wanderlust
public class AI_Harvest : AI_TargetTask {
	public AI_Harvest(Agent_TargetFinder self, Transform target):base(self, target){}
	public override float CalculateFitnessScore() {
		dist = self.GetMob ().DistanceTo (target);
		float d = self ["vision"] - dist;
		idealDist = IdealDistanceToHarvest ();
		return (d*d-dist*self["laziness"]) + self["wanderlust"] + self["greed"] + self["harvest"];
	}
	public override void SetTarget (Transform t) { 
		if (t) {
			props = t.GetComponent<Agent_Properties> ();
			idealDist = IdealDistanceToHarvest ();
		}
		base.SetTarget (t);
	}
	public override void SetSelf (Agent_TargetFinder self) {
		if (self) { selfProps = self.GetComponent<Agent_Properties> (); }
		base.SetSelf (self);
	}
	public override string GetDescription() { return "harvest"; }
	public override bool IsValid(ref string whyNot) {
		bool valid;
		if (valid = base.IsValid (ref whyNot)) {
			if (!props) {
				whyNot = "can only harvest from Agent_Properties, not " + target; 
				Debug.Log (whyNot);
				valid = false;
			} else if (props.AI_IsDangerous ()) {
				whyNot = "too dangerous to \"harvest\"";
				valid = false;
			}
		}
		return valid;
	}
	public override void GetSteering(ref Vector3 move, ref Vector3 look) { 
		self.GetMob().CalculateArrive (target.position - self.transform.forward*idealDist, out move, ref look);
	}
	public override void Execute() {
		self.GetMob().Arrive (target.position - self.transform.forward*idealDist);
	}
	public float IdealDistanceToHarvest() {
		return props.GetRadius () + selfProps ["eatRange"]*self.transform.lossyScale.z;				
	}
	public float dist, idealDist;
	public Agent_Properties props, selfProps;
	public override void DrawLogic(ref List<GameObject> drawnElements) {
//		Vector3 targ = target.position - self.transform.forward * idealDist;
//		Lines.MakeCircle_With (LineElement (0), targ, Camera.main.transform.forward, Color.green,
//			self.GetRadius()).name="<H>";
//		Lines.Make_With (LineElement (1), 
//			target.position,
//			target.position - self.transform.forward * target.transform.lossyScale.z/2,
////			self.transform.position + self.transform.forward * self.GetRadius (), 
////			targ - self.transform.forward * self.GetRadius(), 
//			Color.green, self.GetProperties ().GetEatSphere ().GetRadius (), 0).name="<H.>";
	}
}

// ?cowardice, ?threat(target)
public class AI_Flee : AI_TargetTask {
	public AI_Flee(Agent_TargetFinder self, Transform target):base(self, target){}
	public override float CalculateFitnessScore() {
		Vector3 delta = target.position - self.transform.position;
		float rawDist = delta.magnitude;
		dist = rawDist - (targetProps.GetRadius () + self.GetRadius ());
		Vector3 aScaryDir = delta / -rawDist;
		float comingAtMe = Vector3.Dot(aScaryDir, targetProps.GetMOB().Velocity);
		float d = (idealDist - dist);
		// fleeing is a higher priority if the agent is heading toward us!
		return d * d * (1+self["cowardice"]) + idealDist * comingAtMe;
	}
	public override void SetTarget (Transform t) { 
		if (t) {
			targetProps = t.GetComponent<Agent_Properties> ();
			idealDist = IdealDistance ();
		}
		base.SetTarget (t);
	}
	public override void SetSelf (Agent_TargetFinder self) {
		if (self) { selfProps = self.GetComponent<Agent_Properties> (); }
		base.SetSelf (self);
	}
	public override string GetDescription() { return "flee"; }
	public override bool IsValid(ref string whyNot) {
		bool valid;
		if (valid = base.IsValid (ref whyNot)) {
			if (!targetProps) {
				whyNot = "can only fear Agent_Properties, not " + target; 
				Debug.Log (whyNot);
				valid = false;
			} else if (!targetProps.AI_IsDangerous () || !targetProps.GetEatSphere().IsActive()) {
				whyNot = "won't flee from benign objects ("+targetProps.name+")";
				valid = false;
			}
			if (valid) {
				float d = self.GetMob ().DistanceTo (target);
				if(d > idealDist) {
					whyNot = "not afraid since "+target.name+" is far enough away ("+d+" > "+idealDist+")";
					valid = false;
//					Debug.Log (whyNot);
				}
			}
		}
		return valid;
	}
//	public static GameObject fleeViz;
	public override void GetSteering(ref Vector3 move, ref Vector3 look) { 
		self.GetMob().CalculateSeek (target.position, out move, ref look);
		move *= -1;
	}
	public override void Execute() {
//		Lines.Make (ref fleeViz, self.transform.position, target.position, Color.yellow);
		self.GetMob().Flee (target.position);
	}
	public float IdealDistance() {
		return self.GetRadius() + targetProps.GetRadius () + targetProps ["eatRange"] + targetProps["eatSize"] + 
			(targetProps["speed"]*(1+self["cowardice"]));				
	}
	public float dist, idealDist;
	public Agent_Properties targetProps, selfProps;
	public override void DrawLogic(ref List<GameObject> drawnElements) {
		//float r = target.transform.lossyScale.z / 2;
		//Vector3 d = (target.position - self.transform.position).normalized;
		//Lines.Make_With (LineElement(0), target.transform.position - d * r, target.position - d * (idealDist - self.GetRadius()),
		//	Color.yellow, r*2, 0).name="<F.t>";
		//Lines.MakeCircle_With (LineElement(1), target.position - d * idealDist, Camera.main.transform.forward, 
		//	Color.yellow, self.GetRadius ()).name="<F.e>";
	}
}

// ?aggression, ?threat(target), ?desparation
public class AI_Attack : AI_TargetTask {
	public AI_Attack(Agent_TargetFinder self, Transform target):base(self, target){}
	public override float CalculateFitnessScore() {
		dist = self.GetMob ().DistanceTo (target);
		float d = (threatRange - dist);
		return d*d*winability*(1+self["aggession"]) + self["desperation"] + self["attack"];
	}
	public override void SetTarget (Transform t) { 
		if (t) {
			props = t.GetComponent<Agent_Properties> ();
			if (props) {
				threatRange = ThreatRange ();
				winability = selfProps.AI_CalculateHowIWouldDoInAFightAgainst (props);
			}
		}
		base.SetTarget (t);
	}
	public override void SetSelf (Agent_TargetFinder self) {
		if (self) { selfProps = self.GetComponent<Agent_Properties> (); }
		base.SetSelf (self);
	}
	public override string GetDescription() { return "harvest"; }
	public override bool IsValid(ref string whyNot) {
		bool valid;
		if (valid = base.IsValid (ref whyNot)) {
			if (!props) {
				whyNot = "can only fear Agent_Properties, not " + target; 
				Debug.Log (whyNot);
				valid = false;
			} else if (!props.AI_IsDangerous ()) {
				whyNot = "won't attack benign objects ("+props.name+")";
				valid = false;
			}
			if (valid) {
				dist = self.GetMob ().DistanceTo (target);
				if(dist > threatRange) {
					whyNot = "not aggressive since "+target.name+" is far enough away ("+dist+" > "+threatRange+")";
					valid = false;
//					Debug.Log (whyNot);
				}
			}
		}
		return valid;
	}
	public float ThreatRange() {
		return self.GetRadius() + (selfProps["speed"]*(1+self["aggression"]+winability));
	}

	public override void Enter () {
		selfProps.GetEatSphere().autoActivateAgent = true;
		selfProps.GetEatSphere ().maintainActivation = true; base.Enter ();
	}
	public override void Execute() {
		self.GetMob().Seek (target.position - self.transform.forward*selfProps["eatRange"]);
	}
	public override void Exit () {
		selfProps.GetEatSphere ().maintainActivation = false; base.Exit ();
	}
	public override void DrawLogic(ref List<GameObject> drawnElements) {
//		float r = target.transform.lossyScale.z / 2;
//		Vector3 d = (target.position - self.transform.position).normalized;
//		Lines.Make_With (LineElement(0), self.transform.position+d*self.GetRadius(), target.position-d*r, 
//			Color.red, self.transform.lossyScale.z, 0).name="<A.t>";
//		Lines.Make_With (LineElement(1), self.transform.position + d * threatRange, target.transform.position + d*r,
//			Color.red, target.transform.lossyScale.z, 0).name="<A.e>";
////		Lines.MakeCircle (DrawElement(2), self.transform.position, Camera.main.transform.forward, Color.red, threatRange).name="<A.r>";
	}

	public float dist, threatRange, winability;
	public Agent_Properties props, selfProps;
}

public class AI_Searching : AI_Task {
	public AI_Searching(Agent_TargetFinder self):base(self){}
	public override string GetDescription() { return "searching"; }
	public override bool IsValid(ref string whyNot) { return true; }
	public override void Execute () {
		self.GetMob().RandomWalk (self.transform.position.magnitude / 1024f,
			Singleton.Get<GameRules>().transform.position
		);
		self.GetSensor().enabled = true;// make sure we can see!
		Agent_Sensor.SensorSnapshot s = self.GetSensor().GetSnapshot ();
		// if we found more than just ourselves
		if(s.sensed.Length <= 1 && s.timestamp < Time.time-self.GetSensor().sensorUpdateTime) {
			self.SwitchActivityTo (new AI_Plan (self));
		}
	}
	public override void DrawLogic(ref List<GameObject> drawnElements) {
		//Agent_Sensor.SensorSnapshot s = self.GetSensor().GetSnapshot ();
		//Lines.Make_With (LineElement(0), s.origin, s.origin + s.direction * s.range, Color.gray).name="<S.l>";
		//Lines.MakeCircle_With (LineElement(1), s.origin, Camera.main.transform.forward, Color.gray).name="<S.s>";
		//Lines.MakeCircle_With (LineElement(2), s.origin + s.direction * s.range, Camera.main.transform.forward, Color.gray).name="<S.e>";
	}
}

public class AI_Plan : AI_Task {
	public AI_Plan(Agent_TargetFinder self):base(self){}
	public override string GetDescription() { return "planning"; }
	public static float FitnessScore(Agent_TargetFinder self, Transform target) { return float.MinValue; }
	public override void Execute (){
		self.SwitchActivityTo(BestThingToDoInFrontOfMe (self));
	}
	public static AI_Task BestThingToDoInFrontOfMe(Agent_TargetFinder self) {
		RaycastHit[] hits = self.GetSensor().GetSnapshot ().sensed;
		// if we saw something...
		if (hits != null && hits.Length > 0) {
			//return CalculateFittest<RaycastHit> (self, hits, FitnessFromRaycast);
			List<string> excuses = new List<string> ();
			AI_Task best = ImagineBestChoiceFrom(self, hits, excuses);
	//		if (excuses.Count > 0) {
	//			Debug.Log ("Failed:\n[" + string.Join (",\n", excuses.ToArray ()) + "]");
	//		}
//			if (best != null) {
//				Debug.Log ("Best: "+best);
//			}
			return best;
		}
		return null;
	}
	public delegate AI_Task FitnessFunction<T>(Agent_TargetFinder self, T obj);

	private static AgentImagination imagination = null;
	public static AI_Task ImagineBestChoiceFrom(Agent_TargetFinder self, RaycastHit[] list, List<string> excuses = null) {
		if (imagination == null) {
			imagination = new AgentImagination (self);
		}
		AI_Task fittest = null, forConsideration;
		foreach (RaycastHit iter in list) {
			if (iter.transform != self.transform) {
				forConsideration = imagination.GetBestDecision (self, iter.transform, excuses);//fitness (self, iter);
				self.ConsiderAddingToDo(forConsideration);
				if (forConsideration != null && (fittest == null 
				|| (forConsideration.GetScore () > fittest.GetScore ()))) {
					fittest = forConsideration;
				}
			}
		}
		return fittest;
	}
	public override void DrawLogic(ref List<GameObject> drawnElements) {
		//Agent_Sensor.SensorSnapshot s = self.GetSensor().GetSnapshot ();
		//RaycastHit[] hits = s.sensed;
		//for (int i = 0; i < hits.Length; ++i) {
		//	Lines.Make_With (LineElement(i), hits [i].transform.position, self.transform.position, Color.gray, hits[i].transform.lossyScale.z, 0).name="<P.l>";
		//}
		//ClearLineElementsBeyond (hits.Length);
	}
}

public class AI_CompositeSteering : AI_Task {
	public AI_CompositeSteering(Agent_TargetFinder self):base(self){}
	private List<AI_Task> steering = new List<AI_Task>();
	private float minPriority, priorityDelta;
	public void Clear() { steering.Clear (); }
	bool isActive = false;
	public override void Enter() {
		isActive = true;
		base.Enter ();
	}
	public override void Exit (){
		isActive = false;
		steering.ForEach (i => i.Exit ());
		base.Exit ();
	}

	public void AddBehavior(AI_Task b) {
		if (!b.IsSteering ()) {
			throw new UnityException ("should not accept non-steering behavior");
		}
		steering.Add (b);
		if (isActive) {
			b.Enter ();
		}
	}
	public void CalcPriorities(){
		minPriority = float.MaxValue;
		float maxPriority = float.MinValue, s;
		for (int i = steering.Count-1; i >= 0; --i) {
			AI_Task t = steering[i];
			if(t.IsValid()) {
				t.SetScore ();
				s = t.GetScore ();
				if (s > maxPriority) { maxPriority = s; }
				if (s < minPriority) { minPriority = s; }
			} else {
				steering [i].Exit ();
				steering.RemoveAt (i);
			}
		}
		priorityDelta = maxPriority - minPriority;
	}

	private float GetWeightOf(AI_Task t) { return (t.GetScore () - minPriority) / priorityDelta; }

	/// <summary>keeps track of calculated steering forces from each steering behavior</summary>
	private Vector3[,] steerCalc;

	public override void GetSteering(ref Vector3 move, ref Vector3 look) {
		if (steerCalc == null || steerCalc.GetLength (0) < steering.Count) { steerCalc = new Vector3[steering.Count, 2]; }
		Vector3 m, l;
		for(int i=0;i<steering.Count;++i) {
			AI_Task t = steering[i];
			m = Vector3.zero;
			l = Vector3.zero;
			t.GetSteering (ref m, ref l);
			float p = GetWeightOf (t);//(t.GetScore () - minPriority) / priorityDelta;
			if (m != Vector3.zero) { move += m * p; }
			if (l != Vector3.zero) { look += l * p; }
			steerCalc [i, 0] = m;
			steerCalc [i, 1] = l;
		}
		if (move != Vector3.zero) { move.Normalize (); }
		if (look != Vector3.zero) { look.Normalize (); }
	}
	public override void Execute () {
		Agent_MOB m = self.GetMob ();
		Vector3 move = Vector3.zero, look = Vector3.zero;
		CalcPriorities ();
		GetSteering (ref move, ref look);
		if(look != Vector3.zero) { m.InputLookDirection = look; }//m.UpdateLookDirection (look); }
		if(move != Vector3.zero) { m.InputMoveDirection = move; }//m.ApplyForceToward (move); }
	}
	public override string GetDescription () { return "steering behavior"; }
	public override bool IsValid(ref string whyNot) {
		if (steering.Count == 0) {
			whyNot = "no valid steering behaviors";
			return false;
		}
		return true;
	}

	// TODO draw line and arrow heads pointing out of the target move and look, the thickness indicates priority.
	public override void DrawLogic(ref List<GameObject> drawnElements) {
		//for (int i = 0; i < steering.Count; ++i) {
		//	steering [i].Draw ();
		//}
		//Vector3[] p = new Vector3[3];
		//for (int i = 0; i < steerCalc.GetLength(0) && i < steering.Count; ++i) {
		//	// TODO draw an arrow, with a width based on the weight.
		//	p [0] = self.transform.position + steerCalc[i,0] * (self.transform.lossyScale.z/2);
		//	p [1] = p [0] + steerCalc [i, 0];
		//	p [2] = p [1] + steerCalc [i, 1];
		//	float w = GetWeightOf (steering [i]);
		//	Lines.Make_With(LineElement(i), p, p.Length, steering[i].LineElement(0).GetComponent<Renderer>().material.color, w, 0).name = "<?."+steering[i].GetDescription()+">";
		//}
	}
}

// TODO merged-steering-behavior positions, to allow mergers of seek/flee/evade/hide/flock behaviors
// TODO AI_Flock - move with allies
// TODO AI_Flank - move around target with allies, positioning at different directions
// TODO AI_Swarm - attack with allies, while flanking
// TODO AI_Lure - attempt to aggro and lead agent into ambush
// TODO AI_Ambush - wait, unaggressively, till a target wanders into the ambush area
// TODO AI_Evade - if an agent is seeking me, run away, but find energy rich targets nearby, especially allies
// TODO AI_Hide - if an agent is scaring me, try to keep friendly targets between me and him
// TODO make UI for point allocaiton
// TODO use OBS to make a video showing gameplay