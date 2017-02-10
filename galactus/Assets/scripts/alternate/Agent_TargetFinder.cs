using UnityEngine;
using System.Collections;

public class Agent_TargetFinder : MonoBehaviour {

	[SerializeField]
	private AITask currentTask;

	public float maxSightRange = 100f;
	public float sightRadius = 20f;
	public float secondsBetweenThinking = 2f;
	private float timer;
	private Agent_MOB mob;

	public enum ThingToDo {nothing, seek, arrive, flee, harvest};

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
		return transform.localScale.x;
	}

	// Use this for initialization
	void Start () {
		mob = GetComponent<Agent_MOB> ();
	}
	
	public delegate AITask FitnessFunction<T, SELF>(SELF self, T obj);

	// TODO make this acynchronous
	public static FitnessFunction<RaycastHit, Agent_TargetFinder> fitnessFromRaycast = delegate(Agent_TargetFinder self, RaycastHit obj) {
		Transform t = obj.collider.transform;
		return fitnessFromTransform(self, t);
	};

	// TODO make this acynchronous
	public static FitnessFunction<Transform, Agent_TargetFinder> fitnessFromTransform = delegate(Agent_TargetFinder self, Transform t) {
		if(t == self.transform) { return AITask.none; }
		EnergyAgent energy = t.GetComponent<EnergyAgent>();
		if(!energy) { return AITask.none; }
		AITask think = AITask.none;
		think.score = -(Vector3.Distance(self.transform.position, t.transform.position) - (self.GetRadius() + t.transform.lossyScale.x) / 2);
		bool invalid = false;
		// if the target is a resource collectable
		if(energy) {
			Agent_MOB mob = t.GetComponent<Agent_MOB>();
			if(!mob || mob.GetEatSphere() == null) {
				// add the resource collectable's radius to the fitness score, with a multiplier for how much the agent is a harvester
				think.target = energy.gameObject;
				think.score += energy.GetRadius() * self.generalHarvestingMultiplier;
				think.code = ThingToDo.harvest;
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

	GameObject testRay;

	void PlanSomethingNewToDo() {
		// TODO task queue. if the task queue is empty, THEN go looking for things.
		// pick a random direction to look in, and look there
		Ray r = new Ray (transform.position, Random.onUnitSphere);
		float sightRad = GetRadius () + sightRadius;
//		Lines.Make (ref this.testRay, transform.position, transform.position + r.direction * maxSightRange, Color.white, sightRad, sightRad);
		RaycastHit[] hits = Physics.SphereCastAll (r, sightRad, maxSightRange);
		timer = secondsBetweenThinking;
		// if we saw something...
		if (hits != null && hits.Length > 0) {
			AITask newChoice = FindFittest<RaycastHit,Agent_TargetFinder> (this, hits, fitnessFromRaycast);
			if (newChoice.score > float.MinValue) {
				currentTask = newChoice;
				// TODO compare the newChoice to what is currently being done
			}
		}
		// TODO add an additional delay to the reconsideration timer based on how much this AI Decision is liked
	}

	bool CanKeepDoing(AITask thing) {
		switch (currentTask.code) {
		case ThingToDo.nothing: return false;
		case ThingToDo.flee: return (mob.target - transform.position).magnitude > maxSightRange;
		case ThingToDo.seek:
		case ThingToDo.arrive: return (mob.target - transform.position).magnitude <= GetRadius();
		case ThingToDo.harvest:
			EatSphere eat = mob.GetEatSphere ();
			Agent_Properties meal = eat.GetMeal ();
			return (meal && Vector3.Distance (
				 meal.transform.position,      eat.transform.position) < 
				(meal.transform.localScale.x + eat.transform.localScale.x));
		}
		throw new UnityException ("don't know how to do " + thing.code);
	}

	void StartDoing (AITask thing) {
		switch (currentTask.code) {
		case ThingToDo.nothing:
			mob.targetBehavior = Agent_MOB.TargetBehavior.stop;
			break;
		case ThingToDo.seek:
			mob.target = currentTask.target.transform.position;
			mob.targetBehavior = Agent_MOB.TargetBehavior.seek;
			break;
		case ThingToDo.arrive:
		case ThingToDo.harvest:
			mob.target = currentTask.target.transform.position;
			mob.targetBehavior = Agent_MOB.TargetBehavior.arrive;
			break;
		case ThingToDo.flee:
			mob.target = currentTask.target.transform.position;
			mob.targetBehavior = Agent_MOB.TargetBehavior.flee;
			break;
		}
	}

	void KeepDoing (AITask thing) {
	}
	void StopDoing (AITask thing) {
		mob.targetBehavior = Agent_MOB.TargetBehavior.none;
	}

	// TODO make this acynchronous
	void FixedUpdate() {
		timer -= Time.deltaTime;
		// if it's time to do AI logic
		if (timer <= 0) {
			if (currentTask.IsSomething() && CanKeepDoing(currentTask)) {
				KeepDoing (currentTask);
			} else {
				StopDoing (currentTask);
				PlanSomethingNewToDo ();
				StartDoing (currentTask);
			}
			timer = secondsBetweenThinking;
		}
	}
}
