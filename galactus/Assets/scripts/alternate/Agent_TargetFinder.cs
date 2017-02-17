using UnityEngine;
using System.Collections;

// TODO state machines!
public class Agent_TargetFinder : MonoBehaviour {

	[SerializeField]
	private AITask currentTask;
	private Agent_Sensor sensor;
//	public float maxSightRange = 100f;
//	public float sightRadius = 20f;
	public float secondsBetweenThinking = 2f;
	private float timer;
	private Agent_MOB mob;
	// TODO rename energy->sizeAndEffects
	private Agent_SizeAndEffects energy;

	// TODO implement these in state machines
	public enum ThingToDo {nothing, searching, planning, seek, arrive, flee, harvest};

	[System.Serializable]
	public struct AITask {
		public float score; // how highly to weight this decision
		public ThingToDo code; // what the decision is
		public GameObject target; // where to target this decision
		public AITask(float score, GameObject target, ThingToDo code) {this.score=score;this.target=target;this.code=code;}
		public Vector3 NextLocation() {
			return target.transform.position;
		}
		public static AITask none = new AITask(float.MinValue, null, ThingToDo.nothing);
		public bool IsSomething() { return code != ThingToDo.nothing; }
	};
	public float generalAggressionMultiplier = .5f;
	public float generalHarvestingMultiplier = 1;

	public float GetRadius() {
		return transform.localScale.z;
	}

	// Use this for initialization
	void Start () {
		mob = GetComponent<Agent_MOB> ();
		energy = GetComponent<Agent_SizeAndEffects> ();
		sensor = GetComponent<Agent_Sensor> ();
		sensor.EnsureOwnerIsKnown ();
		sensor.sensorUpdateTime = secondsBetweenThinking;
	}
	
	public delegate AITask FitnessFunction<T, SELF>(SELF self, T obj);

	// TODO make this acynchronous
	public static FitnessFunction<RaycastHit, Agent_TargetFinder> fitnessFromRaycast = delegate(Agent_TargetFinder self, RaycastHit obj) {
		Transform t = obj.collider.transform;
		return fitnessFromTransform(self, t);
	};

	// TODO make this acynchronous
	public static FitnessFunction<Transform, Agent_TargetFinder> fitnessFromTransform = delegate(Agent_TargetFinder self, Transform them) {
		if(them == self.transform) { return AITask.none; }
		// TODO rename theirEnergy->theirSizeAndEffects
		Agent_SizeAndEffects theirEnergy = them.GetComponent<Agent_SizeAndEffects>();
		if(!theirEnergy) { return AITask.none; }
		AITask think = AITask.none;
		think.score = -self.mob.DistanceTo(them);
			//-(Vector3.Distance(self.transform.position, them.transform.position) - (self.GetRadius() + them.transform.lossyScale.x) / 2);
		bool invalid = false;
		// if the target is a resource collectable
		if(theirEnergy) {
			//Agent_MOB mob = t.GetComponent<Agent_MOB>();
			if(theirEnergy.GetEatSphere() == null) {
				// add the resource collectable's radius to the fitness score, with a multiplier for how much the agent is a harvester
				think.target = theirEnergy.gameObject;
				think.score += theirEnergy.GetSize() * self.generalHarvestingMultiplier;
				think.code = ThingToDo.arrive; // TODO if close enough, harvest!
			}
			// if target is another agent
			else {
				// find out what group the target is in
				// if the target is the same group
					// invalidate - TODO check if this ally needs help, and self can help.
						// types of help: "flock", "giveEnergy", "take", "push", "joinHarvest", "joinAttack"
						// add to the score based on the kind of AI, set the AIDecision code
				// if target is from another group
					// compare target strength to self strength (compare = self.radius - target.radius)
					// if the eat-sphere is pointed away, add target.eat.radius, otherwise subtract target.eat.radius
					// if it's a stronger target (compare <= 0)
						// decide if it seems aggressive to self. if so, run. if so, add compare*self.radius to the score. multiply score again by some kind of fear variable
						// set the code to "flee"
					// if it's a weaker target (compare > 0)
						// go for it!
						// set the code to "seek"
			}
		}
		if(invalid) {
			think.score = float.MinValue;
		}
		return think;
	};

	// TODO make this acynchronous
	public static AITask FindFittest<T,SELF>(SELF self, System.Collections.Generic.ICollection<T> list, FitnessFunction<T,SELF> fitness) {
		bool needCandidate = true;
		AITask fittest = AITask.none, forConsideration;
		foreach (T iter in list) {
			forConsideration = fitness (self, iter);
			if (needCandidate || forConsideration.score > fittest.score) {
				fittest = forConsideration;
				needCandidate = false;
			}
		}
		return fittest;
	}

	AITask PlanSomethingNewToDo() {
		timer = sensor.sensorUpdateTime;
		RaycastHit[] hits = sensor.GetSnapshot ().sensed;
		// if we saw something...
		if (hits != null && hits.Length > 0) {
			AITask newChoice = FindFittest<RaycastHit,Agent_TargetFinder> (this, hits, fitnessFromRaycast);
			if (newChoice.score > float.MinValue) {
				// re-evaluate current task, if the current task is valid
				if (currentTask.target != null) {
					currentTask = fitnessFromTransform (this, currentTask.target.transform);
				}
				if (newChoice.score > currentTask.score) {
					return newChoice;
				}
				return currentTask;
			}
		}
		// if we saw nothing, keep doing what we're doing
		return currentTask;
	}

	bool ShouldKeepDoing(AITask thing) {
		switch (currentTask.code) {
		case ThingToDo.nothing: return false;
		case ThingToDo.searching:
			Agent_Sensor.SensorSnapshot s = sensor.GetSnapshot ();
			return s.sensed.Length == 0 || s.timestamp < Time.time-sensor.sensorUpdateTime;
		case ThingToDo.flee: 
			return mob.DistanceTo (thing.target.transform) < sensor.range;
		case ThingToDo.seek:
		case ThingToDo.arrive:
			return Vector3.Distance(mob.transform.position, thing.target.transform.position) >= 0;
		case ThingToDo.harvest:
			EatSphere eat = energy.GetEatSphere ();
			Agent_SizeAndEffects meal = eat.GetMeal ();
			return (meal && mob.DistanceTo(thing.target.transform) < sensor.range);
		}
		throw new UnityException ("don't know how to do " + thing.code);
	}

	void StartDoing (AITask thing) {
//		switch (currentTask.code) {
//		case ThingToDo.nothing:	break;
//		case ThingToDo.seek:	break;
//		case ThingToDo.arrive:	break;
//		case ThingToDo.harvest:	break;
//		case ThingToDo.flee:	break;
//		}
	}

	void KeepDoing (AITask thing) {
		switch (currentTask.code) {
		case ThingToDo.nothing:
		case ThingToDo.searching:
			// stop
			mob.Brake ();
			// look around, pretty much randomly, but weight toward the center of the world
			mob.RandomLook();
			// make sure we can see!
			sensor.enabled = true;
			break;
		case ThingToDo.planning:
			currentTask = PlanSomethingNewToDo ();
			break;
		case ThingToDo.seek:
			mob.Seek (currentTask.target.transform.position);
			break;
		case ThingToDo.arrive:
		case ThingToDo.harvest:
			mob.Arrive (currentTask.target.transform.position);
			break;
		case ThingToDo.flee:
			mob.Flee(currentTask.target.transform.position);
			break;
		default:
			print ("What am I doing? " + currentTask.code);
			break;
		}
	}
	void StopDoing (AITask thing) {
//		mob.targetBehavior = Agent_MOB.TargetBehavior.none;
	}

	// TODO make this acynchronous
	void FixedUpdate() {
		timer -= Time.deltaTime;
		// if it's time to do AI logic
		if (timer <= 0) {
			StopDoing (currentTask);
			currentTask = PlanSomethingNewToDo ();
			StartDoing (currentTask);
			timer = secondsBetweenThinking;
		}
		//print (currentTask.code);
		if (currentTask.IsSomething () && ShouldKeepDoing (currentTask)) {
			KeepDoing (currentTask);
		} else {
			currentTask = AITask.none;
		}
	}
}
