using UnityEngine;
using System.Collections.Generic;

public class Prediction : MonoBehaviour {

	public ParticleSystem predictionParticle;

	ParticleSystem.Particle[] particles = new ParticleSystem.Particle[50];

    // TODO make this a PlayerForce, not a Transform
    //public Transform toPredict;
    private List<PlayerForce> bodies;

    public void SetBodies(List<PlayerForce> bodies) { this.bodies = bodies; }

    private struct CalcSpace {
        public int maxPredictions;
        public PlayerForce pf;
        public ResourceEater re;
        public Rigidbody rb;
        public float speed;
        public float tMod;
        public Vector3 v;
        public Vector3 accelForce;
        public Vector3 predictedLocation;

        public void StartCalc(PlayerForce pf, int maxParticles) {
            this.pf = pf;
            re = pf.GetResourceEater();
            rb = pf.GetComponent<Rigidbody>();
            predictedLocation = pf.transform.position;
            float speed = rb.velocity.magnitude;
            maxPredictions = (int)(maxParticles * speed / 50);//(int)(100 / thisRe.mass);
            if (maxPredictions < 1) maxPredictions = 1;
            if (maxPredictions > maxParticles)
                maxPredictions = maxParticles;
            v = rb.velocity;
            tMod = (float)(re.mass / 2f);
            predictedLocation += v * Time.deltaTime;
            accelForce = pf.accelDirection * pf.maxAcceleration;
        }

        public void Iterate(ref ParticleSystem.Particle particle) {
            particle.position = predictedLocation;
            particle.startSize = re.effectsRadius;
            particle.lifetime = 1;
            particle.startLifetime = 2;
            particle.startColor = re.GetCurrentColor();
            //particle.rotation3D = gameObject.transform.rotation.eulerAngles;
            //predictionParticle.Emit (predictedLocation, v, thisRe.effectsRadius, Time.deltaTime * 2, thisRe.color);
            //points [i] = predictedLocation;
            // TODO make the acceleration change as the velocity changes, to better predict if acceleration stays constant.
            v += (accelForce * (tMod / re.mass));
            float d = v.magnitude;
            if (d > pf.maxSpeed / re.mass) {
                v = v.normalized * (pf.maxSpeed / re.mass);
            }
            predictedLocation += v * tMod;
            accelForce = Steering.SeekDirection(pf.GetMoveDirection() * pf.maxSpeed, rb.velocity, pf.maxAcceleration, tMod);
        }
    }

    private CalcSpace[] calc;

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
            while (predictionsTotal < particles.Length && iterations < maxPrediction) {
                for(int i = 0; i < bodies.Count; ++i)
                {
                    if(iterations < calc[i].maxPredictions) {
                        calc[i].Iterate(ref particles[predictionsTotal]);
                        predictionsTotal++;
                    }
                }
                iterations++;
            }
            /*
			PlayerForce pf = p.GetComponent<PlayerForce> ();
			ResourceEater thisRe = pf.GetResourceEater ();
			Rigidbody rb = pf.GetComponent<Rigidbody> ();
			Vector3 predictedLocation = p.position;
			float speed = rb.velocity.magnitude;
			int numPredictions = (int)(particles.Length  * speed / 50);//(int)(100 / thisRe.mass);
			if(numPredictions < 1) numPredictions = 1;
			if (numPredictions > particles.Length)
				numPredictions = particles.Length;
			//Vector3[] points = new Vector3[numPredictions];
			//float t = 0;
			Vector3 v = rb.velocity;
			float d;
			float tMod = (float)(thisRe.mass / 2f);
            predictedLocation += v * Time.deltaTime;
			Vector3 accelForce = pf.accelDirection * pf.maxAcceleration;
            for (int i = 0; i < numPredictions; ++i) {
				particles [i].position = predictedLocation;
				particles [i].startSize = thisRe.effectsRadius;
				particles [i].lifetime = 1;
				particles [i].startLifetime = 2;
				particles [i].startColor = thisRe.GetCurrentColor();
				particles [i].rotation3D = gameObject.transform.rotation.eulerAngles;
                //predictionParticle.Emit (predictedLocation, v, thisRe.effectsRadius, Time.deltaTime * 2, thisRe.color);
                //points [i] = predictedLocation;
                // TODO make the acceleration change as the velocity changes, to better predict if acceleration stays constant.
                v += (accelForce * (tMod / thisRe.mass));
                d = v.magnitude;
				if (d > pf.maxSpeed/thisRe.mass) {
					v = v.normalized * (pf.maxSpeed/thisRe.mass);
				}
				predictedLocation += v * tMod;
                accelForce = Steering.SeekDirection(pf.GetIntendedDirection() * pf.maxSpeed, rb.velocity, pf.maxAcceleration, tMod);
            }
            */
            for (int i = predictionsTotal; i < particles.Length; ++i) {
				particles [i].startSize = 0;
			}
			predictionParticle.SetParticles (particles, particles.Length);
		}
	}
}
