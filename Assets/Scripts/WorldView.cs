using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class WorldView : MonoBehaviour
{
    private const int _viewStateCount = 3;

    public float SlopeMin;
    public float SlopeMax;
    public float TerrainScale;

    public GameObject TerrainMesh;
    public GameObject WaterMesh;
    public GameObject WaterBackfaceMesh;
    public GameObject OverlayMesh;

    public Transform Foliage;
    public Transform Planet;
    public Transform Sun;
    public Transform Moon;


    public PlanetView PlanetView = new PlanetView();

    ViewState[] _viewStates = new ViewState[_viewStateCount];
    int _lastViewState = 0;
    int _curViewState = 1;
    int _nextViewState = 2;
    float _lerpProgress = 1;

    // Start is called before the first frame update
    void Start()
    {
        GameManager.Active.NewGameEvent += OnNewGame;
        GameManager.Active.SimTickEvent += OnSimTick;


    }

	private void OnDestroy()
	{
        PlanetView?.Dispose();
        foreach (var i in _viewStates)
		{
            i?.Dispose();
		}
	}


	void OnNewGame(SimState simState)
	{
        for (int i = 0; i < _viewStateCount; i++)
        {
            _viewStates[i] = new ViewState();
            _viewStates[i].Init(GameManager.Active.StaticState.Count, 3);
        }

        var terrainMesh = new Mesh();
        terrainMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        TerrainMesh.GetComponent<MeshFilter>().sharedMesh = terrainMesh;
        TerrainMesh.GetComponent<MeshCollider>().sharedMesh = GameManager.Active.Icosphere.Mesh;

        var waterMesh = new Mesh();
        waterMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        WaterMesh.GetComponent<MeshFilter>().sharedMesh = waterMesh;

        var waterBackfaceMesh = new Mesh();
        waterBackfaceMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        WaterBackfaceMesh.GetComponent<MeshFilter>().sharedMesh = waterBackfaceMesh;

        var overlayMesh = new Mesh();
        overlayMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        OverlayMesh.GetComponent<MeshFilter>().sharedMesh = overlayMesh;


        PlanetView.Init(GameManager.Active.Icosphere, GameManager.Active.StaticState.Count, SlopeMin, SlopeMax);
        var jobHandle = PlanetView.BuildRenderState(simState, _viewStates[_nextViewState], GameManager.Active.WorldData, GameManager.Active.StaticState, TerrainScale, default);
        PlanetView.Update(TerrainMesh.GetComponent<MeshFilter>().sharedMesh, WaterMesh.GetComponent<MeshFilter>().sharedMesh, WaterBackfaceMesh.GetComponent<MeshFilter>().sharedMesh, OverlayMesh.GetComponent<MeshFilter>().sharedMesh, _viewStates[_nextViewState], jobHandle);
        TerrainMesh.GetComponent<MeshCollider>().sharedMesh = null;
        TerrainMesh.GetComponent<MeshCollider>().sharedMesh = TerrainMesh.GetComponent<MeshFilter>().sharedMesh;
    }

    // Update is called once per frame
    void Update()
    {
        _lerpProgress = math.saturate(_lerpProgress + Time.deltaTime);
        var jobHandle = PlanetView.Lerp(GameManager.Active.StaticState.Count, _viewStates[_lastViewState], _viewStates[_nextViewState], _viewStates[_curViewState], _lerpProgress);
        PlanetView.Update(TerrainMesh.GetComponent<MeshFilter>().sharedMesh, WaterMesh.GetComponent<MeshFilter>().sharedMesh, WaterBackfaceMesh.GetComponent<MeshFilter>().sharedMesh, OverlayMesh.GetComponent<MeshFilter>().sharedMesh, _viewStates[_curViewState], jobHandle);
    }

    void OnSimTick(SimState simState)
	{
        _lastViewState = _curViewState;
        _nextViewState = (_nextViewState + 1) % 3;
        _curViewState = (_curViewState + 1) % 3;
        var jobHandle = PlanetView.BuildRenderState(simState, _viewStates[_nextViewState], GameManager.Active.WorldData, GameManager.Active.StaticState, TerrainScale, default);
        jobHandle.Complete();
    }

}
