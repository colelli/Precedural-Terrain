using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Classe responsabile della visualizzazione della Texture 2D.<br/>
/// </summary>
public class MapDisplay : MonoBehaviour {

    [SerializeField] private Renderer textureRenderer;
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshRenderer meshRenderer;

    /// <summary>
    /// Il metodo <c>DrawTexture</c> permette di disegnare la texture passata come parametro.
    /// </summary>
    public void DrawTexture(Texture2D texture) {
        textureRenderer.sharedMaterial.mainTexture = texture;
        textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height);
    }

    /// <summary>
    /// Imposta la mesh creata sul corrente GameObject.
    /// </summary>
    /// <param name="meshData"><c>MeshData</c> generata</param>
    /// <param name="texture"><c>Texture2D</c> da applicare alla mesh</param>
    public void DrawMesh(MeshData meshData, Texture2D texture) {
        meshFilter.sharedMesh = meshData.CreateMesh();
        meshRenderer.sharedMaterial.mainTexture = texture;
    }

}
