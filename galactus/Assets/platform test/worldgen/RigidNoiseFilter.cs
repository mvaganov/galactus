﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RigidNoiseFilter : INoiseFilter
{
    NoiseSettings.RidgedNoiseSettings settings;
    SimplexNoise noise = new SimplexNoise();

    public RigidNoiseFilter(NoiseSettings.RidgedNoiseSettings settings)
    {
        this.settings = settings;
    }

    public float Evaluate(Vector3 point)
    {
        float noiseValue = 0;//(noise.Evaluate(point * settings.roughness + settings.center)+1) * .5f;
        float frequency = settings.baseRoughness;
        float amplitude = 1;
        float weight = 1;

        for (int i = 0; i < settings.numLayers; ++i)
        {
            float v = noise.Evaluate(point * frequency + settings.center);
            v = 1 - Mathf.Abs(v);
            v *= v;
            v *= weight;
            weight = Mathf.Clamp(v * settings.weightMultiplier, 0, 1);
            noiseValue += v * amplitude;
            frequency *= settings.roughness; // when roughness is greater than 1, frequency will increase with each layer
            amplitude *= settings.persistence; // if persistence is less than one, amplitude will decrease with each layer
        }
        noiseValue = Mathf.Max(0, noiseValue - settings.minValue);
        return noiseValue * settings.strength;
    }
}
