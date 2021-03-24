using UnityEngine;

[CreateAssetMenu(fileName = "ErosionSetting", menuName = "ScriptableObjects/ErosionSetting", order = 1)]
public class ErosionSetting : ScriptableObject
{
    [Header("Terrain")]
    [Range(0, 500)]
    public int SmoothingIterationsOnStart = 0;

    [Header("Flow settings")]
    [Range(0.1f, 10f)]
    public float PipeArea = 1f;
    [Range(0.1f, 20f)]
    public float Gravity = 3f;
    [Range(0.1f, 1f)]
    public float PipeLength = 1f;
    [Range(0.1f, 1f)]
    public float CellSize = 1f;

    [Header("Erosion and deposition settings")]
    [Range(0.1f, 100f)]
    public float SedimentCapacity = 0.9f;
    [Range(0.1f, 1f)]
    public float SuspensionRate = 1f;
    [Range(0.1f, 1f)]
    public float DepositionRate = 1f; 
     
    [Header("Rain and evaporation")]
    public bool AddWater = true;


    [Range(0, 1f)]
    public float Evaporation = 0.1f;

    [Range(0, 1f)]
    public float MaxRainIntensity = 0.5f;
    [Range(0, 1f)]
    public float MinRainIntensity = 0f;
    [Range(2, 50f)]
    public float MaxRainSize = 32f;
    [Range(2, 50f)]
    public float MinRainSize = 2f;

    [Range(0f, 1f)]
    public float RainFrequency = 0.01f;

    [Range(0f, 10000f)]
    public float StopRainAfterTime = 1000f;

    public bool RandomizedRain = true;

    [Header("Time and step size")]

    [Range(0.001f, 0.5f)]
    public float TimeDelta = 0.01f;
    [Range(1, 5000)]
    public float IterationsEachStep = 500f;

    [Header("Iterations on different sizes")]
    [Range(0, 5000)] 
    public int iterationsOn257 = 500;
    [Range(0, 2000)] 
    public int iterationsOn513 = 500;
    [Range(0, 1000)] 
    public int iterationsOn1025 = 500;
    [Range(0, 500)] 
    public int iterationsOn2049 = 10;

}
