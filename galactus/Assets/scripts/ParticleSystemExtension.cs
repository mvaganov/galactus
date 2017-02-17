using UnityEngine;
using System.Collections;

public static class ParticleSystemExtension
{
    public static void EnableEmission(this ParticleSystem particleSystem, bool enabled)
    {
        var emission = particleSystem.emission;
        emission.enabled = enabled;
    }

    public static float GetEmissionRate(this ParticleSystem particleSystem)
    {
		return particleSystem.emission.rateOverTimeMultiplier;
    }

    public static void SetEmissionRate(this ParticleSystem particleSystem, float emissionRate)
    {
        var emission = particleSystem.emission;
		var rate = emission.rateOverTime;
        rate.constantMax = emissionRate;
        emission.rateOverTime = rate;
    }

	public static void SetColor(this ParticleSystem ps, Color c) {
		ParticleSystem.MainModule m = ps.main;
		ParticleSystem.MinMaxGradient mmg = m.startColor;
		mmg.color = c;
		m.startColor = mmg;
	}

	public static void SetParticleSpeed(this ParticleSystem ps, float speed) {
		ParticleSystem.MainModule m = ps.main;
		m.startSpeedMultiplier = speed;
	}

	public static void SetParticleSize(this ParticleSystem ps, float size) {
		ParticleSystem.MainModule m = ps.main;
		m.startSizeMultiplier = size;
	}

	public static void SetParticleLifetime(this ParticleSystem ps, float lifetime) {
		ParticleSystem.MainModule m = ps.main;
		m.startLifetimeMultiplier = lifetime;
	}
}
