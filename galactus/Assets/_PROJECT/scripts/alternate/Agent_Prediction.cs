using UnityEngine;
using System.Collections.Generic;

public class Agent_Prediction : MonoBehaviour {
	// TODO if an object is in the path defined by Prediction, put some things around it's name. like "name" vs ">name<"
	public ParticleSystem predictionParticle;
	public Agent_InputControl controls;

	ParticleSystem.Particle[] particles = new ParticleSystem.Particle[50];

	private List<Agent_MOB> bodies;

	public void SetBodies(List<Agent_MOB> bodies) { this.bodies = bodies; }

    private struct CalcSpace {
        public int maxPredictions; // TODO do maxDistancePredicted, where the distance is the stop distance.
		public Agent_MOB pf;
		public Agent_SizeAndEffects re;
        public Rigidbody rb;
        public float speed;
        public float tMod;
        public Vector3 v;
        public Vector3 accelForce;
        public Vector3 predictedLocation;

		public void StartCalc(Agent_MOB pf, int maxParticles) {
            this.pf = pf;
			re = pf.GetComponent<Agent_SizeAndEffects>();
            rb = pf.GetComponent<Rigidbody>();
            predictedLocation = pf.transform.position;
            float speed = rb.velocity.magnitude;
            maxPredictions = (int)(maxParticles * speed / 50);//(int)(100 / thisRe.mass);
            if (maxPredictions < 1) maxPredictions = 1;
            if (maxPredictions > maxParticles)
                maxPredictions = maxParticles;
            v = rb.velocity;
            tMod = (float)(rb.mass / 2f);
            predictedLocation += v * Time.deltaTime;
			//accelForce = pf.accelDirection * pf.maxAcceleration;
			Vector3 dir = pf.InputMoveDirection;//pf.GetAccelerationDirection();
            if (dir == Vector3.zero) dir = rb.transform.forward;
			accelForce = Steering.SeekDirection(dir * pf.MoveSpeed//pf.maxSpeed
				, rb.velocity, pf.Acceleration, tMod);
        }

        public void Iterate(ref ParticleSystem.Particle particle) {
            particle.position = predictedLocation;
			particle.startSize = re.GetRadius();
            particle.remainingLifetime = 1;
            particle.startLifetime = 2;
			particle.startColor = re.GetColor();
            //particle.rotation3D = gameObject.transform.rotation.eulerAngles;
            //predictionParticle.Emit (predictedLocation, v, thisRe.effectsRadius, Time.deltaTime * 2, thisRe.color);
            //points [i] = predictedLocation;
            // TODO make the acceleration change as the velocity changes, to better predict if acceleration stays constant.
            v += (accelForce * (tMod / rb.mass));
            float d = v.magnitude;
            if (d > pf.MoveSpeed / rb.mass) {
				v = v.normalized * (pf.MoveSpeed / rb.mass);
            }
            predictedLocation += v * tMod;
			Vector3 dir = pf.InputMoveDirection;// pf.GetAccelerationDirection();
            if(dir == Vector3.zero) dir = rb.transform.forward;
			accelForce = Steering.SeekDirection(dir * pf.MoveSpeed, rb.velocity, pf.Acceleration, tMod);
        }

		public void StopLocation(ref ParticleSystem.Particle particle) {
			particle.position = pf.transform.position + rb.velocity.normalized * pf.BrakeDistance;//GetBrakeDistance();
			particle.startSize = re.GetSize();
			particle.remainingLifetime = 1;
			particle.startLifetime = 2;
			particle.startColor = re.GetEffectColor();
		}
    }

    private CalcSpace[] calc;

	void Start() {
		this.controls = GetComponent<Agent_InputControl> ();
	}

    void FixedUpdate () {
        //Transform p = toPredict;
		if (bodies != null && bodies.Count > 0) {
            int predictionsTotal = 0;
            int iterations = 0;
            int maxPrediction = 0;
            if(calc == null || calc.Length < bodies.Count) {
                calc = new CalcSpace[bodies.Count];
            }
            // start calculations based on the current bodies
            for(int i = 0; i < bodies.Count; ++i) {
                calc[i].StartCalc(bodies[i], particles.Length);
                if (calc[i].maxPredictions > maxPrediction) maxPrediction = calc[i].maxPredictions;
            }
			for (int i = 0; i < bodies.Count; ++i) {
				calc[i].StopLocation(ref particles[predictionsTotal]);
				predictionsTotal++;
			}
			if (!controls.IsBraking ()) {
				// calculate the stop location
				while (predictionsTotal < particles.Length && iterations < maxPrediction) {
					for (int i = 0; i < bodies.Count; ++i) {
						if (iterations < calc [i].maxPredictions && !calc [i].pf.IsBraking () && calc[i].pf.rb.velocity != Vector3.zero) {
							calc [i].Iterate (ref particles [predictionsTotal]);
							predictionsTotal++;
						}
					}
					iterations++;
				}
			}
            for (int i = predictionsTotal; i < particles.Length; ++i) {
				particles [i].startSize = 0;
			}
			predictionParticle.SetParticles (particles, particles.Length);
		}
	}
}
