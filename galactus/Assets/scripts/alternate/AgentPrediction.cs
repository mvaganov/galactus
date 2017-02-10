//using UnityEngine;
//using System.Collections.Generic;
//
//public class AgentPrediction : MonoBehaviour {
//
//	public ParticleSystem predictionParticle;
//
//	ParticleSystem.Particle[] particles = new ParticleSystem.Particle[50];
//
//    private List<AgentForce> bodies;
//
//	public void SetBodies(List<AgentForce> bodies) { this.bodies = bodies; }
//
//    private struct CalcSpace {
//        public int maxPredictions;
//		public AgentForce pf;
//		public AgentForce re;
//        public Rigidbody rb;
//        public float speed;
//        public float tMod;
//        public Vector3 v;
//        public Vector3 accelForce;
//        public Vector3 predictedLocation;
//
//        public void StartCalc(AgentForce pf, int maxParticles) {
//            this.pf = pf;
//            re = pf;
//            rb = pf.GetComponent<Rigidbody>();
//            predictedLocation = pf.transform.position;
//            float speed = rb.velocity.magnitude;
//            maxPredictions = (int)(maxParticles * speed / 50);//(int)(100 / thisRe.mass);
//            if (maxPredictions < 1) maxPredictions = 1;
//            if (maxPredictions > maxParticles)
//                maxPredictions = maxParticles;
//            v = rb.velocity;
//            tMod = (float)(re.mass / 2f);
//            predictedLocation += v * Time.deltaTime;
//            //accelForce = pf.accelDirection * pf.maxAcceleration;
//            Vector3 dir = pf.movement.GetMoveDirection();
//            if (dir == Vector3.zero) dir = rb.transform.forward;
//			accelForce = Steering.SeekDirection(dir * pf.movement.maxSpeed, rb.velocity, pf.movement.maxAcceleration, tMod);
//        }
//
//        public void Iterate(ref ParticleSystem.Particle particle) {
//            particle.position = predictedLocation;
//            particle.startSize = re.effectsSize;
//            particle.lifetime = 1;
//            particle.startLifetime = 2;
//            particle.startColor = re.GetCurrentColor();
//            //particle.rotation3D = gameObject.transform.rotation.eulerAngles;
//            //predictionParticle.Emit (predictedLocation, v, thisRe.effectsRadius, Time.deltaTime * 2, thisRe.color);
//            //points [i] = predictedLocation;
//            // TODO make the acceleration change as the velocity changes, to better predict if acceleration stays constant.
//            v += (accelForce * (tMod / re.mass));
//            float d = v.magnitude;
//            if (d > pf.movement.maxSpeed / re.mass) {
//				v = v.normalized * (pf.movement.maxSpeed / re.mass);
//            }
//            predictedLocation += v * tMod;
//			Vector3 dir = pf.movement.GetMoveDirection();
//            if(dir == Vector3.zero) dir = rb.transform.forward;
//			accelForce = Steering.SeekDirection(dir * pf.movement.maxSpeed, rb.velocity, pf.movement.maxAcceleration, tMod);
//        }
//    }
//
//    private CalcSpace[] calc;
//
//    void FixedUpdate () {
//        //Transform p = toPredict;
//		if (bodies != null && bodies.Count > 0) {
//            int predictionsTotal = 0;
//            int iterations = 0;
//            int maxPrediction = 0;
//            if(calc == null || calc.Length < bodies.Count) {
//                calc = new CalcSpace[bodies.Count];
//            }
//            // start calculations based on the current bodies
//            for(int i = 0; i < bodies.Count; ++i) {
//                calc[i].StartCalc(bodies[i], particles.Length);
//                if (calc[i].maxPredictions > maxPrediction) maxPrediction = calc[i].maxPredictions;
//            }
//            while (predictionsTotal < particles.Length && iterations < maxPrediction) {
//                for(int i = 0; i < bodies.Count; ++i)
//                {
//                    if(iterations < calc[i].maxPredictions) {
//                        calc[i].Iterate(ref particles[predictionsTotal]);
//                        predictionsTotal++;
//                    }
//                }
//                iterations++;
//            }
//            for (int i = predictionsTotal; i < particles.Length; ++i) {
//				particles [i].startSize = 0;
//			}
//			predictionParticle.SetParticles (particles, particles.Length);
//		}
//	}
//}
