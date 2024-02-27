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
    /// <param name="meshHeightMultiplier">Moltiplicatore altezza mappa</param>
    /// <param name="_meshHeightCurve">Curva per la gestione del moltiplicatore</param>
    /// <param name="levelOfDetail">Livello di dettaglio della mesh</param>
    /// <param name="useFlatShading">Renderizzare con flat shading (default: false)</param>
    /// <returns><c>MeshData</c></returns>
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float meshHeightMultiplier, AnimationCurve _meshHeightCurve, int levelOfDetail, bool useFlatShading = false) {
        AnimationCurve meshHeightCurve = new AnimationCurve(_meshHeightCurve.keys);

        int meshSemplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;
        int borderedSize = heightMap.GetLength(0);
        int meshSize = borderedSize - 2 * meshSemplificationIncrement;
        //usato per valori che non devono dipendere dalla densità della mesh
        int meshSizeUnsimplified = borderedSize - 2;

        float topLeftX = (meshSizeUnsimplified - 1) / -2f; //negative value
        float topLeftY = (meshSizeUnsimplified - 1) / 2f; //positive value

        int verticesPerLine = (meshSize - 1) / meshSemplificationIncrement + 1;

        MeshData meshData = new MeshData(verticesPerLine, useFlatShading);

        int[,] vertexIndicesMap = new int[borderedSize, borderedSize];
        int meshVertexIndex = 0;
        int borderVertexIndex = -1;

        for (int y = 0; y < borderedSize; y += meshSemplificationIncrement) {
            for (int x = 0; x < borderedSize; x += meshSemplificationIncrement) {
                bool isBorderVertex = y == 0 || y == borderedSize - 1 || x == 0 || x == borderedSize - 1;
                if (isBorderVertex) {
                    //Se bordo lo aggiungo con indice negativo e decremento
                    vertexIndicesMap[x, y] = borderVertexIndex;
                    borderVertexIndex--;
                } else {
                    //Altrimenti aggiungo come indice positivo (mesh da renderizzare)
                    vertexIndicesMap[x, y] = meshVertexIndex;
                    meshVertexIndex++;
                }

            }
        }

        for (int y = 0; y < borderedSize; y += meshSemplificationIncrement) {
            for(int x = 0; x < borderedSize; x += meshSemplificationIncrement) {
                int vertexIndex = vertexIndicesMap[x, y];

                Vector2 percent = new Vector2((x - meshSemplificationIncrement) / (float)meshSize, (y - meshSemplificationIncrement) / (float)meshSize);
                float height = meshHeightCurve.Evaluate(heightMap[x, y]) * meshHeightMultiplier;
                Vector3 vertexPosition = new Vector3(topLeftX + percent.x * meshSizeUnsimplified, height, topLeftY - percent.y * meshSizeUnsimplified);

                meshData.AddVertex(vertexPosition, percent, vertexIndex);

                //Aggiungo i triangoli ignorando i "bordi" destro ed inferiore
                //Vengono aggiunti in senso orario
                if(x < (borderedSize - 1) && y < (borderedSize - 1)) {
                    int a = vertexIndicesMap[x, y];
                    int b = vertexIndicesMap[x + meshSemplificationIncrement, y];
                    int c = vertexIndicesMap[x, y + meshSemplificationIncrement];
                    int d = vertexIndicesMap[x + meshSemplificationIncrement, y + meshSemplificationIncrement];

                    meshData.AddTriangle(a,d,c);
                    meshData.AddTriangle(d,a,b);
                }

                vertexIndex++;
            }
        }

        meshData.ProcessMesh();

        return meshData;

    }

}

/// <summary>
/// Classe responsabile della gestione dei dati relativi alla mesh da generare.
/// </summary>
public class MeshData {

    private Vector3[] vertices;
    private Vector2[] uvs;
    private Vector3[] bakedNormals;
    private int[] triangles;

    private Vector3[] borderVertices;
    private int[] borderTrinagles;

    private int triangleIndex;
    private int borderTriangleIndex;

    private bool useFlatShading;

    public MeshData(int verticesPerLine, bool useFlatShading) {
        this.useFlatShading = useFlatShading;

        vertices = new Vector3[verticesPerLine * verticesPerLine];
        uvs = new Vector2[verticesPerLine * verticesPerLine];
        triangles = new int[(verticesPerLine-1) * (verticesPerLine-1) * 6];

        borderVertices = new Vector3[verticesPerLine * 4 + 4];
        borderTrinagles = new int[24 * verticesPerLine];
    }

    public void AddTriangle(int a, int b, int c) {
        if(a < 0 || b < 0 || c < 0) {
            //Border triangle
            borderTrinagles[borderTriangleIndex] = a;
            borderTrinagles[borderTriangleIndex + 1] = b;
            borderTrinagles[borderTriangleIndex + 2] = c;

            borderTriangleIndex += 3;
        } else {
            //Mesh triangle
            triangles[triangleIndex] = a;
            triangles[triangleIndex + 1] = b;
            triangles[triangleIndex + 2] = c;

            triangleIndex += 3;
        }

    }

    public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex) {
        if(vertexIndex < 0) {
            //Border vertex
            borderVertices[-vertexIndex - 1] = vertexPosition;
        } else {
            //Mesh vertex
            vertices[vertexIndex] = vertexPosition;
            uvs[vertexIndex] = uv;
        }
    }

    private Vector3[] CalculateNormals() {
        Vector3[] vertexNormals = new Vector3[vertices.Length];
        //Ottengo il numero di trinagoli prendendo la lunghezza dei vertici e dividendo per 3
        int triangleCount = triangles.Length / 3;

        //Ciclo attraverso i triangoli della Mesh da visualizzare
        for(int i = 0; i < triangleCount; i++) {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = triangles[normalTriangleIndex];
            int vertexIndexB = triangles[normalTriangleIndex + 1];
            int vertexIndexC = triangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            vertexNormals[vertexIndexA] += triangleNormal;
            vertexNormals[vertexIndexB] += triangleNormal;
            vertexNormals[vertexIndexC] += triangleNormal;
        }

        //Ciclo attraverso i triangoli della Mesh del bordo (solo ad uso UVs)
        int borderTriangleCount = borderTrinagles.Length / 3;
        for (int i = 0; i < borderTriangleCount; i++) {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = borderTrinagles[normalTriangleIndex];
            int vertexIndexB = borderTrinagles[normalTriangleIndex + 1];
            int vertexIndexC = borderTrinagles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            if (vertexIndexA >= 0) {
                vertexNormals[vertexIndexA] += triangleNormal;
            }
            if (vertexIndexB >= 0) {
                vertexNormals[vertexIndexB] += triangleNormal;
            }
            if (vertexIndexC >= 0) {
                vertexNormals[vertexIndexC] += triangleNormal;
            }
        }


        for (int i = 0; i < vertexNormals.Length; i++) {
            vertexNormals[i].Normalize();
        }

        return vertexNormals;
    }

    private Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC) {
        Vector3 pointA = (indexA < 0) ? borderVertices[-indexA - 1] : vertices[indexA];
        Vector3 pointB = (indexB < 0) ? borderVertices[-indexB - 1] : vertices[indexB];
        Vector3 pointC = (indexC < 0) ? borderVertices[-indexC - 1] : vertices[indexC];

        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;

        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    public void ProcessMesh() {
        if (useFlatShading) {
            FlatShading();
        } else {
            BakeNormals();
        }
    }

    private void BakeNormals() {
        bakedNormals = CalculateNormals();
    }

    private void FlatShading() {
        Vector3[] flatShadedVertices = new Vector3[triangles.Length];
        Vector2[] flatShadedUVs = new Vector2[triangles.Length];

        for(int i = 0;i < triangles.Length; i++) {
            flatShadedVertices[i] = vertices[triangles[i]];
            flatShadedUVs[i] = uvs[triangles[i]];
            triangles[i] = i;
        }

        //Sovrascrivo i vertici e UV con quelli flat shaded
        vertices = flatShadedVertices;
        uvs = flatShadedUVs;
    }

    public Mesh CreateMesh() {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        if (useFlatShading) {
            mesh.RecalculateNormals();
        } else {
            mesh.normals = bakedNormals;
        }

        return mesh;
    }

}