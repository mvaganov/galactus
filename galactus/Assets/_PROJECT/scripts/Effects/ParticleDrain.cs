using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleDrain : MonoBehaviour {

	private int partsActive = 0;
	public Transform target;
	public float speed = 20;
	private float distance;
	private ParticleSystem ps;
	private ParticleSystem.Particle[] particles;

	public void Setup(Transform target, int particleCount, Color color, float speed) {
		this.target = target;
		partsActive = particleCount;
		ps = GetComponent<ParticleSystem> ();
		ps.SetColor (color);
		ps.Emit (particleCount);
		particles = new ParticleSystem.Particle[particleCount];
		ps.GetParticles (particles);
		this.speed = speed;
		distance = 0;
		System.Array.ForEach (particles, (p) => {
			float d = Vector3.Distance(transform.position, p.position);
			if(d > distance) { distance = d; }
		});
	}

	void Update () {
		if (partsActive <= 0) {
			MemoryPoolItem.Destroy (gameObject);
			return;
		}
		float rad = target.lossyScale.z/2;
		ps.GetParticles (particles);
		for (int i = partsActive-1; i >= 0; --i) {
			ParticleSystem.Particle p = particles [i];
			if (p.remainingLifetime < p.startLifetime / 2) {
				Vector3 delta = target.position - p.position;
				float d = delta.magnitude;
				if (d <= rad || p.remainingLifetime <= 0) {
					particles [i] = particles [--partsActive];
				} else {
					Vector3 dir = delta / d;
					p.velocity = dir * speed;
//					p.position += dir * speed * Time.deltaTime;
//					p.remainingLifetime = p.startLifetime / 2;
					//p.remainingLifetime = (d < distance) ? d * p.startLifetime / distance : p.startLifetime;
					particles [i] = p;
				}
			}
		}
		ps.SetParticles (particles, partsActive);
	}
}
