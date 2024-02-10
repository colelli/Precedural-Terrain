using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Classe responsabile per la generazione della Mesh della mappa.
/// </summary>
public static class MeshGenerator {

    /// <summary>
    /// Metodo per la generazione della mesh del terreno data una mappa del "rumore".<br/>
    /// Viene intesa tale la mappa, in scala di grigi, generata dal Perlin Noise.<br/>
    /// Tale mappa contiene i valori necessari relativi le quote dei singoli vertici.
    /// </summary>
    /// <param name="heightMap">Mappa del "rumore"</param>
    /// <returns><c>MeshData</c></returns>
    public static MeshData GenerateTerrainMesh(float[,] heightMap) {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        float topLeftX = (width - 1) / -2f; //negative value
        float topLeftY = (height - 1) / 2f; //positive value

        MeshData meshData = new MeshData(width, height);
        int vertexIndex = 0; //Serve per tenere traccia del punto in cui ci troviamo nell'array "vertices" di meshData

        for(int y = 0; y < height; y++) {
            for(int x = 0; x < width; x++) {
                meshData.SetVertexAtIndex(vertexIndex, new Vector3(topLeftX + x, heightMap[x,y], topLeftY - y));
                meshData.SetUvsAtIndex(vertexIndex, new Vector2(x / (float)width, y / (float)height));

                //Aggiungo i triangoli ignorando i "bordi" destro ed inferiore
                //Vengono aggiunti in senso orario
                if(x < (width - 1) && y < (height - 1)) {
                    meshData.AddTriangle(vertexIndex, vertexIndex + width + 1, vertexIndex + width);
                    meshData.AddTriangle(vertexIndex + width + 1, vertexIndex, vertexIndex + 1);
                }

                vertexIndex++;
            }
        }

        return meshData;

    }

}

/// <summary>
/// Classe responsabile della gestione dei dati relativi alla mesh da generare.
/// </summary>
public class MeshData {

    private Vector3[] vertices;
    private Vector2[] uvs;
    private int[] triangles;

    private int triangleIndex;

    public MeshData(int meshWidth, int meshHeight) {
        vertices = new Vector3[meshWidth * meshHeight];
        uvs = new Vector2[meshWidth * meshHeight];
        triangles = new int[(meshWidth-1) * (meshHeight-1) * 6];
    }

    public void AddTriangle(int a, int b, int c) {
        triangles[triangleIndex] = a;
        triangles[triangleIndex+1] = b;
        triangles[triangleIndex+2] = c;

        triangleIndex += 3;
    }

    public void SetVertexAtIndex(int index, Vector3 vector) {
        vertices[index] = vector;
    }

    public void SetUvsAtIndex(int index, Vector2 uv) {
        uvs[index] = uv;
    }

    public Mesh CreateMesh() {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        return mesh;
    }

}