using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Classe per la generazione della mappa del "rumore" che guida la generazione del terreno casuale (basato sul Perlin Noise).
/// </summary>
public class MapGenerator : MonoBehaviour {

    private enum DrawMode {HEIGHT_MAP, COLOR_MAP, DRAW_MESH}

    [Header("Render Configs")]
    [SerializeField] private DrawMode drawMode;

    [Header("Map Configs")]
    //241 perchè < 255 (possiamo massimo avere 255^2 vertici per mesh)
    //e 241-1=240 che è fattorizzabile come (1,2,4,6,8,12)
    private const int MAP_CHUNK_SIZE = 241;
    [SerializeField] [Range(0, 6)] [Tooltip("Livello di dettaglio della mesh")] private int levelOfDetail;
    [SerializeField] [Range(0.01f, 99.99f)] private float noiseScale;

    [SerializeField] [Tooltip("Numero di ottave")] [Min(1)] private int octaves;
    [SerializeField] [Tooltip("Influenza delle ottave")] [Range(0f,1f)] private float persistance;
    [SerializeField] [Tooltip("Frequenza delle ottave")] [Min(1f)] private float lacunarity;

    [SerializeField] private int seed;
    [SerializeField] private Vector2 offset;

    [SerializeField] private bool autoUpdate;

    [SerializeField] private TerrainType[] mapRegions;
    [SerializeField] private float meshHeightMultiplier;
    [SerializeField] private AnimationCurve meshHeightCurve;


    /// <summary>
    /// Il metodo <c>GenerateMap</c> genera la noiseMap da renderizzare sul piano.<br/>
    /// A seconda della corrente <c>DrawMode</c> selezionata viene renderizzata:<br/>
    /// - Mappa delle altezze (scala di grigi/raw Perlin Noise);<br/>
    /// - Mappa dei colori (suddivisa sulla base dei <c>TerrainType</c>);<br/>
    /// - Rendering della Mesh (generazione & rendering della mesh, compresa texture).
    /// </summary>
    public void GenerateMap() {
        float[,] noiseMap = NoiseGenerator.GenerateNoiseMap(MAP_CHUNK_SIZE, MAP_CHUNK_SIZE, seed, noiseScale, octaves, persistance, lacunarity, offset);

        Color[] colourMap = GenerateColourMap(noiseMap);

        MapDisplay display = FindObjectOfType<MapDisplay>();
        switch (drawMode) {
            case DrawMode.HEIGHT_MAP:
                display.DrawTexture(TextureGenerator.TextureFromHeightMap(noiseMap));
                break;
            case DrawMode.COLOR_MAP:
                display.DrawTexture(TextureGenerator.TextureFromColourMap(colourMap, MAP_CHUNK_SIZE, MAP_CHUNK_SIZE));
                break;
            case DrawMode.DRAW_MESH:
                display.DrawMesh(MeshGenerator.GenerateTerrainMesh(noiseMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail), TextureGenerator.TextureFromColourMap(colourMap, MAP_CHUNK_SIZE, MAP_CHUNK_SIZE));
                break;
            default:
                break;
        }

    }

    //Metodo privato per la generazione della colour map (DrawMode.COLOR_MAP)
    private Color[] GenerateColourMap(float[,] noiseMap) {

        Color[] colourMap = new Color[MAP_CHUNK_SIZE * MAP_CHUNK_SIZE];

        for(int y = 0; y < MAP_CHUNK_SIZE; y++) {
            for(int x = 0; x < MAP_CHUNK_SIZE; x++) {

                float currentHeight = noiseMap[x, y];

                for(int r = 0; r < mapRegions.Length; r++) {

                    if(currentHeight <= mapRegions[r].height) {
                        colourMap[y * MAP_CHUNK_SIZE + x] = mapRegions[r].color;
                        break;
                    }

                }

            }
        }

        return colourMap;

    }

    public bool CanAutoUpdate() {
        return autoUpdate;
    }

    public static int GetMapChunkSize() {
        return MAP_CHUNK_SIZE;
    }

}

[System.Serializable]
public struct TerrainType {
    public string terrainLabel;
    public float height;
    public Color color;
}