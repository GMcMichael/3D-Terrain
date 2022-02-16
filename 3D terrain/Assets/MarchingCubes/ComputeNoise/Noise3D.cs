using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Noise3D : BaseNoise
{
    [HideInInspector]
    public float frequency = 1;
    [HideInInspector]
    public float amplitude = 1;
    [HideInInspector]
    public int octaves = 1;
    public override ComputeBuffer Generate(ComputeBuffer pointsBuffer, int numPoints, int numPointsPerAxis, float spacing, Vector3 noiseOffset, Vector3 worldCoords, Vector3 worldSize)
    {
        computeNoise.SetFloat("frequency", frequency);
        computeNoise.SetFloat("amplitude", amplitude);
        computeNoise.SetInt("octaves", octaves);
        return base.Generate(pointsBuffer, numPoints, numPointsPerAxis, spacing, noiseOffset, worldCoords, worldSize);
    }

    public void SetValues(float _frequency, float _amplitude, int _octaves) {
        amplitude = _amplitude;
        frequency = _frequency;
        octaves = _octaves;
    }
}
