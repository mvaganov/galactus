using UnityEngine;
using System.Collections;

public class Prediction : MonoBehaviour {

	public ParticleSystem predictionParticle;

	ParticleSystem.Particle[] particles = new ParticleSystem.Particle[50];

	public Transform toPredict;

	void FixedUpdate () {
		Transform p = toPredict;
		if (p) {
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
            for (int i = numPredictions; i < particles.Length; ++i) {
				particles [i].startSize = 0;
			}
			predictionParticle.SetParticles (particles, particles.Length);
		}
	}
}
