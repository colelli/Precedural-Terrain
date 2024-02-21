using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Logica per la generazione del terreno infinita suddivisa per chunk
/// </summary>
public class EndlessTerrain : MonoBehaviour {

    private static MapGenerator mapGenerator;

    private const float maxViewDistance = 450f;
    [SerializeField] private Transform player;
    [SerializeField] private Material mapMaterial;

    private static Vector2 playerPosition;
    private int chunkSize;
    private int chunksVisibleInDistance;

    private Dictionary<Vector2, TerrainChunk> terrainChunkDIctionary = new Dictionary<Vector2, TerrainChunk>();
    private List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    private void Start() {
        mapGenerator = FindObjectOfType<MapGenerator>();
        chunkSize = MapGenerator.GetMapChunkSize() - 1;
        chunksVisibleInDistance = Mathf.RoundToInt(maxViewDistance / chunkSize);
    }

    private void Update() {
        playerPosition = new Vector2(player.position.x, player.position.z);
        UpdateVisibleChunks();
    }

    private void UpdateVisibleChunks() {
        int currentChunkCoordX = Mathf.RoundToInt(playerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(playerPosition.y / chunkSize);

        //Itero tra i chunk visibili nell'ultimo Update per renderli invisibili
        for(int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++) {
            terrainChunksVisibleLastUpdate[i].SetVisible(false);
        }
        terrainChunksVisibleLastUpdate.Clear();

        for(int yOffset = -chunksVisibleInDistance; yOffset <= chunksVisibleInDistance; yOffset++) { 
            for(int xOffset = -chunksVisibleInDistance; xOffset <= chunksVisibleInDistance; xOffset++) {

                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
                
                if(terrainChunkDIctionary.ContainsKey(viewedChunkCoord)) {
                    //Contiene già il chunk -> lo rendo visibile
                    terrainChunkDIctionary[viewedChunkCoord].UpdateTerrainChunk();
                    if (terrainChunkDIctionary[viewedChunkCoord].IsVisible()) {
                        terrainChunksVisibleLastUpdate.Add(terrainChunkDIctionary[viewedChunkCoord]);
                    }
                } else {
                    //NON contiene il chunk -> lo istazio
                    terrainChunkDIctionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, transform, mapMaterial));
                }

            }
        }

    }

    /// <summary>
    /// Classe di supporto che contiene informazioni relative i vari chunk che vengono generati
    /// </summary>
    public class TerrainChunk {

        private GameObject meshObject;
        private Vector2 position;
        private Bounds bounds;

        MapData mapData;
        MeshRenderer meshRenderer;
        MeshFilter meshFilter;

        public TerrainChunk(Vector2 coordinate, int size, Transform parent, Material material) {
            position = coordinate * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0f, position.y);

            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshRenderer.material = material;
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshObject.transform.position = positionV3;
            meshObject.transform.parent = parent;

            SetVisible(false);

            mapGenerator.RequestMapData(OnMapDataReceived);
        }

        private void OnMapDataReceived(MapData mapData) {
            mapGenerator.RequestMeshData(mapData, OnMeshDataReceived);
        }

        private void OnMeshDataReceived(MeshData meshData) {
            meshFilter.mesh = meshData.CreateMesh();
        }

        /// <summary>
        /// Metodo che gestisce la visione del chunk basato su un <c>Bounds</c>.<br/>
        /// Se la distanza è <= della distanza massiva di rendering, i chunk vengono visualizzati.
        /// </summary>
        public void UpdateTerrainChunk() {
            float playerDistanceFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(playerPosition));
            bool visible = playerDistanceFromNearestEdge <= maxViewDistance;
            SetVisible(visible);
        }

        public void SetVisible(bool visible) {
            meshObject.SetActive(visible);
        }

        public bool IsVisible() {
            return meshObject.activeSelf;
        }
    }

}
