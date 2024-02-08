using System.Collections;
using UnityEngine;

/// <summary>
/// La classe <c>NoiseGenerator</c> è responsabile delle funzioni di creazione e generazione del "rumore" per ottenere una mappa organica e casuale.<br/>
/// </summary>
public static class NoiseGenerator {

    /// <summary>
    /// Il metodo <c>GenerateNoiseMap</c> si occupa della generazione della mappa utilizzando il Perlin Noise.<br/>
    /// Questo approccio permette di generare mappe casuali (mediante un seed) che possano riportare aspetti quanto più organici.
    /// </summary>
    /// <param name="mapWidth">La larghezza della mappa da generare</param>
    /// <param name="mapHeight">L'altezza (lunghezza) della mappa da generare</param>
    /// <param name="seed">Il seme per generare mappe pseudo-casuali</param>
    /// <param name="scale">Un valore di scala della mappa</param>
    /// <param name="octaves">Il numero di "ottave" che formano il risultato della noiseMap.<br/>La noiseMap è una composizione delle varie ottave sulla base della <c>persistance</c> e <c>lacunarity</c></param>
    /// <param name="persistance">Influenza l'amplitudine (influenza) della feature (su un piano 2D viene visualizzato come la y)</param>
    /// <param name="lacunarity">Influenza la frequenza della feature (su un piano 2D viene visualizzato come la x)</param>
    /// <param name="offset">Offset per spostare la mappa (default Vector2.zero)</param>
    /// <returns></returns>
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistance = 0.5f, float lacunarity = 2f, Vector2 offset = default(Vector2)) {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        //Creo un nuovo sample per le ottave da generare pseudo-casualmente (basate sul seed)
        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        for(int i = 0; i < octaves; i++) {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) + offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        //Uso queste variabili per "scalare" verso il centro della mappa invece che l'angolo in alto a destra
        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;

        for (int y=0; y < mapHeight; y++) {
            for(int x=0; x < mapWidth; x++) {

                float amplitude = 1f;
                float frequency = 1f;
                float noiseHeight = 0f;

                for(int i=0; i<octaves; i++) {
                    float sampleX = (x-halfWidth) / scale * frequency + octaveOffsets[i].x;
                    float sampleY = (y-halfHeight) / scale * frequency + octaveOffsets[i].y;

                    //Preso il PerlinNoise lo moltiplico per 2 e sottraggo 1 per poter portare i valori nel range [-1, 1]
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;

                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance; //range [0,1] -> decrementa le ottave
                    frequency *= lacunarity; //range [1,inf] -> incrementa le ottave
                }

                //Aggiorno i valori minimi e massimi per poter normalizzare la mappa del rumore
                if(noiseHeight > maxNoiseHeight) {
                    maxNoiseHeight = noiseHeight;
                }else if(noiseHeight < minNoiseHeight) {
                    minNoiseHeight = noiseHeight;
                }

                noiseMap[x, y] = noiseHeight;

            }
        }

        noiseMap = NormalizeNoiseMap(minNoiseHeight, maxNoiseHeight, noiseMap);

        return noiseMap;

    }

    private static float[,] NormalizeNoiseMap(float min, float max, float[,] noiseMap) {

        int width = noiseMap.GetLength(0);
        int height = noiseMap.GetLength(1);

        float[,] normalizedMap = new float[width, height];

        for (int y = 0; y < width; y++) {
            for (int x = 0; x < height; x++) {
                normalizedMap[x, y] = Mathf.InverseLerp(min, max, noiseMap[x, y]);
            }
        }

        return normalizedMap;

    }

}