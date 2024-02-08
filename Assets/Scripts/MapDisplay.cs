using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Classe responsabile della generazione della Texture 2D contenente la mappa del "rumore".<br/>
/// La classe è responsabile anche della visualizzazione della texture su un piano presente nell'Editor.
/// </summary>
public class MapDisplay : MonoBehaviour {

    [SerializeField] private Renderer textureRenderer;

    /// <summary>
    /// Il metodo <c>DrawNoiseMap</c> permette di andare a generare e disegnare la texture 
    /// contenente i dati del PerlinNoise su un piano presente nell'Editor.
    /// </summary>
    /// <param name="noiseMap">La mappa del "rumore" generata</param>
    public void DrawNoiseMap(float[,] noiseMap) {
        int width = noiseMap.GetLength(0);
        int height = noiseMap.GetLength(1);

        Texture2D texture = GenerateTexture(noiseMap, width, height);

        textureRenderer.sharedMaterial.mainTexture = texture;
        textureRenderer.transform.localScale = new Vector3(width, 1, height);
    }

    /// <summary>
    /// Metodo privato per la generazione della Texture 2D
    /// </summary>
    /// <param name="noiseMap">La mappa del "rumore" generata</param>
    /// <param name="width">Larghezza della mappa/texture</param>
    /// <param name="height">Altezza della mappa/texture</param>
    /// <returns></returns>
    private Texture2D GenerateTexture(float[,] noiseMap, int width, int height) {
        Texture2D texture = new Texture2D(width, height);

        Color[] colourMap = new Color[width * height];
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                /*
                 * Trasformo le coordinate di una matrice in un singolo vettore e faccio il Lerp (interpolazione) tra nero e 
                 * a seconda del valore generato in precedenza all'interno della noiseMap (riporta il valore tra 0 ed 1)
                 */
                colourMap[y * width + x] = Color.Lerp(Color.black, Color.white, noiseMap[x, y]);
            }
        }
        texture.SetPixels(colourMap);
        texture.Apply();

        return texture;
    }

}
