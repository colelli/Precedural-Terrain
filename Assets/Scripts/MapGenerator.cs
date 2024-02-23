using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

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
    [SerializeField] [Range(0, 6)] [Tooltip("Livello di dettaglio della mesh")] private int editorPreviewLOD;
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

    private Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    private Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    public void DrawMapInEditor() {
        MapData mapdata = GenerateMapData(Vector2.zero);
        MapDisplay display = FindObjectOfType<MapDisplay>();
        switch (drawMode) {
            case DrawMode.HEIGHT_MAP:
                display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapdata.heightMap));
                break;
            case DrawMode.COLOR_MAP:
                display.DrawTexture(TextureGenerator.TextureFromColourMap(mapdata.colourMap, MAP_CHUNK_SIZE, MAP_CHUNK_SIZE));
                break;
            case DrawMode.DRAW_MESH:
                display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapdata.heightMap, meshHeightMultiplier, meshHeightCurve, editorPreviewLOD), TextureGenerator.TextureFromColourMap(mapdata.colourMap, MAP_CHUNK_SIZE, MAP_CHUNK_SIZE));
                break;
            default:
                break;
        }
    }

    //Creo una chiamata con Callback per gestire la generazione del terreno in un thread diverso
    //Il metodo di generazione è attivo nel thread in cui viene chiamato
    public void RequestMapData(Vector2 centre,  Action<MapData> callback) {
        ThreadStart threadStart = delegate {
            MapDataThread(centre, callback);
        };

        new Thread(threadStart).Start();
    }

    private void MapDataThread(Vector2 centre, Action<MapData> callback) {
        MapData mapData = GenerateMapData(centre);
        //lock server per evitare problemi di accesso concorrenziale da più thread alla stessa variabile
        lock (mapDataThreadInfoQueue){
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    public void RequestMeshData(MapData mapdata, int lod, Action<MeshData> callback) {
        ThreadStart threadStart = delegate {
            MeshDataThread(mapdata, lod, callback);
        };

        new Thread(threadStart).Start();
    }

    private void MeshDataThread(MapData mapdata, int lod, Action<MeshData> callback) {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapdata.heightMap, meshHeightMultiplier, meshHeightCurve, lod);
        //lock server per evitare problemi di accesso concorrenziale da più thread alla stessa variabile
        lock (meshDataThreadInfoQueue) {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    private void Update() {
        //Verifico se ci sono chiamate di callback nella coda della mapData
        if(mapDataThreadInfoQueue.Count > 0) {
            for(int i = 0; i < mapDataThreadInfoQueue.Count; i++) {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }

        //Verifico se ci sono chiamate di callback nella coda della meshData
        if(meshDataThreadInfoQueue.Count > 0) {
            for(int i = 0; i < meshDataThreadInfoQueue.Count; i++) {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    /// <summary>
    /// Il metodo <c>GenerateMap</c> genera la noiseMap da renderizzare sul piano.<br/>
    /// A seconda della corrente <c>DrawMode</c> selezionata viene renderizzata:<br/>
    /// - Mappa delle altezze (scala di grigi/raw Perlin Noise);<br/>
    /// - Mappa dei colori (suddivisa sulla base dei <c>TerrainType</c>);<br/>
    /// - Rendering della Mesh (generazione & rendering della mesh, compresa texture).
    /// </summary>
    private MapData GenerateMapData(Vector2 centre) {
        float[,] noiseMap = NoiseGenerator.GenerateNoiseMap(MAP_CHUNK_SIZE, MAP_CHUNK_SIZE, seed, noiseScale, octaves, persistance, lacunarity, centre + offset);
        Color[] colourMap = GenerateColourMap(noiseMap);

        return new MapData(noiseMap, colourMap);
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

    private struct MapThreadInfo<T> {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter) {
            this.callback = callback;
            this.parameter = parameter;
        }
    }

}

[System.Serializable]
public struct TerrainType {
    public string terrainLabel;
    public float height;
    public Color color;
}

public struct MapData {
    public readonly float[,] heightMap;
    public readonly Color[] colourMap;

    public MapData(float[,] heightMap, Color[] colourMap) {
        this.heightMap = heightMap;
        this.colourMap = colourMap;
    }
}