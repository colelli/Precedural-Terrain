using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Classe per la generazione della mappa del "rumore" che guida la generazione del terreno casuale (basato sul Perlin Noise).
/// </summary>
public class MapGenerator : MonoBehaviour {

    [Header("Map Configs")]
    [SerializeField] [Min(1)] private int mapHeight;
    [SerializeField] [Min(1)] private int mapWidth;
    [SerializeField] [Range(0.01f, 99.99f)] private float noiseScale;

    [SerializeField] [Tooltip("Numero di ottave")] [Min(1)] private int octaves;
    [SerializeField] [Tooltip("Influenza delle ottave")] [Range(0f,1f)] private float persistance;
    [SerializeField] [Tooltip("Frequenza delle ottave")] [Min(1f)] private float lacunarity;

    [SerializeField] private int seed;
    [SerializeField] private Vector2 offset;

    [SerializeField] private bool autoUpdate;

    /// <summary>
    /// Il metodo <c>GenerateMap</c> permette di generare la noiseMap da renderizzare sul piano.
    /// </summary>
    public void GenerateMap() {
        float[,] noiseMap = NoiseGenerator.GenerateNoiseMap(mapWidth, mapHeight, seed, noiseScale, octaves, persistance, lacunarity, offset);

        MapDisplay display = FindObjectOfType<MapDisplay>();
        display.DrawNoiseMap(noiseMap);

    }

    public bool CanAutoUpdate() {
        return autoUpdate;
    }

}