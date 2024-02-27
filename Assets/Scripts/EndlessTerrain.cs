using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Logica per la generazione del terreno infinita suddivisa per chunk
/// </summary>
public class EndlessTerrain : MonoBehaviour {

    private static MapGenerator mapGenerator;

    //usato per scalare la mappa
    private const float scale = 1f;

    private const float playerMoveThresholdForChunkUpdate = 25f;
    private const float sqrPlayerMoveThresholdForChunkUpdate = playerMoveThresholdForChunkUpdate * playerMoveThresholdForChunkUpdate;

    [SerializeField] private LODInfo[] detailLevels;
    private static float maxViewDistance;

    [SerializeField] private Transform player;
    [SerializeField] private Material mapMaterial;

    private static Vector2 playerPosition;
    private Vector2 playerPositionOld;
    private int chunkSize;
    private int chunksVisibleInDistance;

    private Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    private static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    private void Start() {
        mapGenerator = FindObjectOfType<MapGenerator>();

        maxViewDistance = detailLevels[detailLevels.Length - 1].visibleDistanceThreashold;
        chunkSize = MapGenerator.GetMapChunkSize() - 1;
        chunksVisibleInDistance = Mathf.RoundToInt(maxViewDistance / chunkSize);

        UpdateVisibleChunks();
    }

    private void Update() {
        playerPosition = new Vector2(player.position.x, player.position.z) / scale;

        if((playerPositionOld - playerPosition).sqrMagnitude > sqrPlayerMoveThresholdForChunkUpdate) {
            playerPositionOld = playerPosition;
            UpdateVisibleChunks();
        }

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
                
                if(terrainChunkDictionary.ContainsKey(viewedChunkCoord)) {
                    //Contiene già il chunk -> lo rendo visibile
                    terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                    if (terrainChunkDictionary[viewedChunkCoord].IsVisible()) {
                        terrainChunksVisibleLastUpdate.Add(terrainChunkDictionary[viewedChunkCoord]);
                    }
                } else {
                    //NON contiene il chunk -> lo istazio
                    terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, detailLevels, transform, mapMaterial));
                }

            }
        }

    }

    /// <summary>
    /// Classe di supporto che contiene informazioni relative i vari chunk che vengono generati
    /// </summary>
    private class TerrainChunk {

        private GameObject meshObject;
        private Vector2 position;
        private Bounds bounds;

        private MapData mapData;
        private bool mapDataReceived;
        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;
        private MeshCollider meshCollider;

        private LODInfo[] detailLevels;
        private LODMesh[] lodMeshes;
        private LODMesh collisionLODMesh;

        private int previousLODIndex = -1;

        public TerrainChunk(Vector2 coordinate, int size, LODInfo[] detailLevels, Transform parent, Material material) {
            this.detailLevels = detailLevels;
            
            position = coordinate * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0f, position.y);

            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshRenderer.material = material;
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshCollider = meshObject.AddComponent<MeshCollider>();
            meshObject.transform.position = positionV3 * scale;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one * scale;

            lodMeshes = new LODMesh[detailLevels.Length];
            for(int i = 0; i < detailLevels.Length; i++) {
                lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk);
                if (detailLevels[i].useForCollider) {
                    collisionLODMesh = lodMeshes[i];
                }
            }

            SetVisible(false);

            mapGenerator.RequestMapData(position, OnMapDataReceived);
        }

        private void OnMapDataReceived(MapData mapData) {
            this.mapData = mapData;
            mapDataReceived = true;

            Texture2D texture = TextureGenerator.TextureFromColourMap(mapData.colourMap, MapGenerator.GetMapChunkSize(), MapGenerator.GetMapChunkSize());
            meshRenderer.material.mainTexture = texture;

            UpdateTerrainChunk();
        }

        /// <summary>
        /// Metodo che gestisce la visione del chunk basato su un <c>Bounds</c>.<br/>
        /// Se la distanza è <= della distanza massiva di rendering, i chunk vengono visualizzati.
        /// </summary>
        public void UpdateTerrainChunk() {
            if (!mapDataReceived) return;

            float playerDistanceFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(playerPosition));
            bool visible = playerDistanceFromNearestEdge <= maxViewDistance;

            if (visible) {
                int lodIndex = 0;
                for(int i = 0; i < detailLevels.Length - 1; i++) {
                    if(playerDistanceFromNearestEdge > detailLevels[i].visibleDistanceThreashold) {
                        lodIndex = i + 1;
                    } else {
                        break;
                    }
                }

                if(lodIndex != previousLODIndex) {
                    LODMesh lodMesh = lodMeshes[lodIndex];
                    if (lodMesh.HasMesh()) {
                        previousLODIndex = lodIndex;
                        meshFilter.mesh = lodMesh.GetMesh();
                    }else if (!lodMesh.HasRequestedMesh()) {
                        lodMesh.RequestMesh(mapData);
                    }
                }

                if(lodIndex == 0) {
                    //Il player è abbastanza vicino per la max resolution
                    if (collisionLODMesh.HasMesh()) {
                        meshCollider.sharedMesh = collisionLODMesh.GetMesh();
                    } else if (!collisionLODMesh.HasRequestedMesh()) {
                        collisionLODMesh.RequestMesh(mapData);
                    }
                }

                terrainChunksVisibleLastUpdate.Add(this);
            }

            SetVisible(visible);
        }

        public void SetVisible(bool visible) {
            meshObject.SetActive(visible);
        }

        public bool IsVisible() {
            return meshObject.activeSelf;
        }

        public bool HasReceivedMapData() {
            return mapDataReceived;
        }

    }

    private class LODMesh {

        private Mesh mesh;
        private bool hasRequestedMesh;
        private bool hasMesh;
        private int lod;
        private System.Action updateCallback;

        public LODMesh(int lod, System.Action updateCallback) {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }

        private void OnMeshDataReceived(MeshData meshData) {
            mesh = meshData.CreateMesh();
            hasMesh = true;

            updateCallback();
        }

        public void RequestMesh(MapData mapData) {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);
        }

        public Mesh GetMesh() {
            return mesh;
        }

        public bool HasRequestedMesh() {
            return hasRequestedMesh;
        }

        public bool HasMesh() {
            return hasMesh;
        }

    }

    [System.Serializable]
    public struct LODInfo {
        public int lod;
        public float visibleDistanceThreashold;
        public bool useForCollider;
    }

}
