using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Classe responsabile della generazione delle <c>Texture2D</c>.
/// </summary>
public static class TextureGenerator {

    /// <summary>
    /// Genera una <c>Texture2D</c> data una mappa dei colori e le dimensioni della texture da creare (corrispondono con il piano).
    /// </summary>
    /// <param name="colourMap">Mappa dei colori</param>
    /// <param name="width">Larghezza texture</param>
    /// <param name="height">Altezza texture</param>
    /// <returns><c>Texture2D</c></returns>
    public static Texture2D TextureFromColourMap(Color[] colourMap, int width, int height) {
        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colourMap);
        texture.Apply();
        return texture;
    }

    /// <summary>
    /// Genera una <c>Texture2D</c> data una mappa del "rumore", ovvero una mappa delle altezze.<br/>
    /// Viene intesa tale la mappa, in scala di grigi, generata dal Perlin Noise.
    /// </summary>
    /// <param name="heightMap">Mappa del "rumore"</param>
    /// <returns><c>Texture2D</c></returns>
    public static Texture2D TextureFromHeightMap(float[,] heightMap) {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        Color[] colourMap = new Color[width * height];
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                /*
                 * Trasformo le coordinate di una matrice in un singolo vettore e faccio il Lerp (interpolazione) tra nero e 
                 * a seconda del valore generato in precedenza all'interno della noiseMap (riporta il valore tra 0 ed 1)
                 */
                colourMap[y * width + x] = Color.Lerp(Color.black, Color.white, heightMap[x, y]);
            }
        }

        return TextureFromColourMap(colourMap, width, height);

    }

}