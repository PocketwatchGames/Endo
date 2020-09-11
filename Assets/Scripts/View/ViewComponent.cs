using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using System;

namespace Endo
{
    public enum MeshOverlay
    {
        None,
        AirTemperature
    }


    public enum LegendType
    {
        None,
        Temperature,
        PPM,
        Percent,
        Pressure,
        Volume,
        Mass,
        Watts,
    }

    [Serializable]
    public struct MeshOverlayColors
    {
        public string Title;
        public float Min;
        public float Max;
        public LegendType LegendType;
        public int DecimalPlaces;
        public NativeArray<CVP> ColorValuePairs;
    }

    public struct MeshOverlayData
    {
        public MeshOverlayData(MeshOverlayColors colors, NativeSlice<float> values)
        {
            Values = values;
            Colors = colors;
            InverseRange = 1.0f / (colors.Max - colors.Min);
        }
        public MeshOverlayColors Colors;
        public NativeSlice<float> Values { get; private set; }
        public float InverseRange { get; private set; }
    }

    public struct WindOverlayData
    {
        public WindOverlayData(float maxVelocity, bool maskLand, NativeSlice<float3> values)
        {
            MaxVelocity = maxVelocity;
            MaskLand = maskLand;
            Values = values;
        }
        public float MaxVelocity { get; private set; }
        public bool MaskLand { get; private set; }
        public NativeSlice<float3> Values { get; private set; }
    }



    public class ViewComponent : MonoBehaviour
    {
        private const int _viewStateCount = 3;

        public float SlopeMin;
        public float SlopeMax;
        public float TerrainScale;

        public GameObject TerrainMesh;
        public GameObject WaterMesh;
        public GameObject WaterBackfaceMesh;
        public GameObject OverlayMesh;

        public Transform Planet;
        public Transform Sun;
        public Transform Moon;
        public SimComponent Sim;
        public FoliageManager Foliage;

        public const int VertsPerCell = 25;
        public const int VertsPerCloud = 25;
        public const int MaxNeighbors = 6;

        public MeshOverlay ActiveOverlay;
        public Dictionary<MeshOverlay, MeshOverlayColors> OverlayColors;

        [Header("Display")]
        public float DisplayFloraWeight = 1;
        public float DisplaySandWeight = 1;
        public float DisplaySoilWeight = 1;
        public float DisplayDustMax = 100;
        public float DisplayLavaTemperatureMax = 1200;
        public float DisplaySoilFertilityMax = 5.0f;
        public float DisplayPlanktonMax = 10;
        public float DisplayPlanktonPower = 0.5f;
        public int DisplayPlanktonLevels = 5;
        public int DisplayIceLevels = 5;
        public int DisplayIcePower = 5;
        public float DisplayWaterTemperatureMax = 50;
        public int DisplayWaterTemperatureLevels = 10;

        [Header("Wind Overlay")]
        public float DisplayWindSpeedLowerAirMax = 50;
        public float DisplayWindSpeedUpperAirMax = 250;
        public float DisplayPressureGradientForceMax = 0.01f;
        public float DisplayWindSpeedSurfaceWaterMax = 5;
        public float DisplayWindSpeedDeepWaterMax = 0.5f;
        public float DisplayVerticalWindSpeedMax = 0.5f;
        public float DisplayVerticalWindSpeedMin = 0.05f;

        [Header("Overlays")]
        public float DisplayRainfallMax = 5.0f;
        public float DisplaySalinityMin = 0;
        public float DisplaySalinityMax = 50;
        public float DisplayEvaporationMax = 5.0f;
        public float DisplayTemperatureMin = 223;
        public float DisplayTemperatureMax = 323;
        public float DisplayWaterCarbonMax = 0.001f;
        public float DisplayAbsoluteHumidityMax = 0.05f;
        public float DisplayAirPressureMin = 97000;
        public float DisplayAirPressureMax = 110000;
        public float DisplayHeatAbsorbedMax = 1000;
        public float DisplayCrustDepthMax = 10000;
        public float DisplayMagmaMassMax = 1000000;
        public float DisplayCarbonDioxideMax = 0.002f;
        public float DisplayOxygenMax = 0.35f;
        public float DisplayDivergenceMax = 1000;


        private List<int> _terrainIndices;
        private NativeArray<float3> _terrainVertices;
        private NativeArray<float4> _terrainColors;
        private NativeArray<float3> _terrainNormals;
        private NativeArray<float4> _terrainUVs;

        private List<int> _waterBackfaceIndices;
        private NativeArray<float3> _waterVertices;
        private NativeArray<float4> _waterColors;
        private NativeArray<float3> _waterNormals;

        private NativeArray<float3> _overlayVertices;
        private NativeArray<Color32> _overlayColors;
        private NativeArray<float> _selectionCells;

        private NativeArray<float3> _standardVerts;

        private JobHelper _perCellJobHelper;
        private JobHelper _perVertexJobHelper;

        private NativeArray<CVP> _normalizedRainbow;
        private NativeArray<CVP> _normalizedBlueBlackRed;


        private bool _indicesInitialized;


        ViewState[] _viewStates = new ViewState[_viewStateCount];
        int _lastViewState = 0;
        int _curViewState = 1;
        int _nextViewState = 2;
        float _lerpProgress = 1;

        // Start is called before the first frame update
        void Start()
        {
            Sim.NewGameEvent += OnNewGame;
            Sim.SimTickEvent += OnSimTick;
        }

		public void Init()
		{
            for (int i = 0; i < _viewStateCount; i++)
            {
                _viewStates[i] = new ViewState();
                _viewStates[i].Init(Sim.StaticState.Count, 3);
            }

            var terrainMesh = new Mesh();
            terrainMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            TerrainMesh.GetComponent<MeshFilter>().sharedMesh = terrainMesh;
            TerrainMesh.GetComponent<MeshCollider>().sharedMesh = Sim.Icosphere.Mesh;

            var waterMesh = new Mesh();
            waterMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            WaterMesh.GetComponent<MeshFilter>().sharedMesh = waterMesh;

            var waterBackfaceMesh = new Mesh();
            waterBackfaceMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            WaterBackfaceMesh.GetComponent<MeshFilter>().sharedMesh = waterBackfaceMesh;

            var overlayMesh = new Mesh();
            overlayMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            OverlayMesh.GetComponent<MeshFilter>().sharedMesh = overlayMesh;

            Foliage.Init(Sim.StaticState.Count, Sim.StaticState);

            _perCellJobHelper = new JobHelper(Sim.StaticState.Count);
            _perVertexJobHelper = new JobHelper(Sim.StaticState.Count * VertsPerCell);

            _terrainVertices = new NativeArray<float3>(Sim.StaticState.Count * VertsPerCell, Allocator.Persistent);
            _terrainNormals = new NativeArray<float3>(Sim.StaticState.Count * VertsPerCell, Allocator.Persistent);
            _terrainColors = new NativeArray<float4>(Sim.StaticState.Count * VertsPerCell, Allocator.Persistent);
            _terrainUVs = new NativeArray<float4>(Sim.StaticState.Count * VertsPerCell, Allocator.Persistent);
            _terrainIndices = new List<int>();

            _waterVertices = new NativeArray<float3>(Sim.StaticState.Count * VertsPerCell, Allocator.Persistent);
            _waterNormals = new NativeArray<float3>(Sim.StaticState.Count * VertsPerCell, Allocator.Persistent);
            _waterColors = new NativeArray<float4>(Sim.StaticState.Count * VertsPerCell, Allocator.Persistent);
            _waterBackfaceIndices = new List<int>();

            _overlayVertices = new NativeArray<float3>(Sim.StaticState.Count * VertsPerCell, Allocator.Persistent);
            _overlayColors = new NativeArray<Color32>(Sim.StaticState.Count * VertsPerCell, Allocator.Persistent);
            _selectionCells = new NativeArray<float>(Sim.StaticState.Count, Allocator.Persistent);

            _standardVerts = new NativeArray<float3>(Sim.StaticState.Count * VertsPerCell, Allocator.Persistent);

            Unity.Mathematics.Random random = new Unity.Mathematics.Random(1);

            _normalizedRainbow = new NativeArray<CVP>(new CVP[] {
                                            new CVP(Color.black, 0),
                                            new CVP(new Color(0.25f,0,0.5f,1), 1.0f / 7),
                                            new CVP(Color.blue, 2.0f / 7),
                                            new CVP(Color.green, 3.0f / 7),
                                            new CVP(Color.yellow, 4.0f / 7),
                                            new CVP(Color.red, 5.0f / 7),
                                            new CVP(Color.magenta, 6.0f / 7),
                                            new CVP(Color.white, 1),
                                            },
                                                Allocator.Persistent);
            _normalizedBlueBlackRed = new NativeArray<CVP>(new CVP[] {
                                            new CVP(Color.blue, 0),
                                            new CVP(Color.black, 0.5f),
                                            new CVP(Color.red, 1) },
                                                Allocator.Persistent);

            OverlayColors = new Dictionary<MeshOverlay, MeshOverlayColors>
            {
                { MeshOverlay.AirTemperature, new MeshOverlayColors{ Title="Air Temperature", Min=DisplayTemperatureMin,Max=DisplayTemperatureMax,ColorValuePairs=_normalizedRainbow, LegendType=LegendType.Temperature, DecimalPlaces=0 } },
            };

            const float maxSlope = 3;
            for (int i = 0; i < Sim.Icosphere.Vertices.Length; i++)
            {
                float3 pos = Sim.Icosphere.Vertices[i];
                _standardVerts[i * VertsPerCell] = pos;
                int neighborCount = (Sim.Icosphere.Neighbors[(i + 1) * MaxNeighbors - 1] >= 0) ? MaxNeighbors : (MaxNeighbors - 1);
                for (int j = 0; j < neighborCount; j++)
                {
                    int neighborIndex1 = Sim.Icosphere.Neighbors[i * MaxNeighbors + j];
                    int neighborIndex2 = Sim.Icosphere.Neighbors[i * MaxNeighbors + (j + 1) % neighborCount];

                    //_terrainIndices.Add(i);
                    //_terrainIndices.Add(neighborIndex2);
                    //_terrainIndices.Add(neighborIndex1);

                    //_waterBackfaceIndices.Add(i);
                    //_waterBackfaceIndices.Add(neighborIndex1);
                    //_waterBackfaceIndices.Add(neighborIndex2);


                    {
                        float slope = random.NextFloat() * (SlopeMax - SlopeMin) + SlopeMin;
                        float3 slopePoint = (Sim.Icosphere.Vertices[neighborIndex1] + Sim.Icosphere.Vertices[neighborIndex2] + pos * (1 + slope)) / (maxSlope + slope);
                        float slopePointLength = math.length(slopePoint);
                        float3 extendedSlopePoint = slopePoint / (slopePointLength * slopePointLength);


                        _standardVerts[i * VertsPerCell + 1 + j] = extendedSlopePoint; // surface
                        _standardVerts[i * VertsPerCell + 1 + j + MaxNeighbors] = extendedSlopePoint; // wall
                        _standardVerts[i * VertsPerCell + 1 + j + MaxNeighbors * 2] = extendedSlopePoint; // wall
                        _standardVerts[i * VertsPerCell + 1 + j + MaxNeighbors * 3] = extendedSlopePoint; // corner

                    }

                    _terrainIndices.Add(i * VertsPerCell);
                    _terrainIndices.Add(i * VertsPerCell + 1 + ((j + 1) % neighborCount));
                    _terrainIndices.Add(i * VertsPerCell + 1 + j);

                    _waterBackfaceIndices.Add(i * VertsPerCell + 1 + j);
                    _waterBackfaceIndices.Add(i * VertsPerCell + 1 + ((j + 1) % neighborCount));
                    _waterBackfaceIndices.Add(i * VertsPerCell);

                    int neighbor1 = -1;
                    {
                        int neighborNeighborCount = (Sim.Icosphere.Neighbors[(neighborIndex1 + 1) * MaxNeighbors - 1] >= 0) ? MaxNeighbors : (MaxNeighbors - 1);
                        for (int k = 0; k < neighborNeighborCount; k++)
                        {
                            if (Sim.Icosphere.Neighbors[neighborIndex1 * MaxNeighbors + k] == i)
                            {
                                neighbor1 = (k - 1 + neighborNeighborCount) % neighborNeighborCount;
                                _terrainIndices.Add(i * VertsPerCell + 1 + 2 * MaxNeighbors + ((j - 1 + neighborCount) % neighborCount));
                                _terrainIndices.Add(i * VertsPerCell + 1 + MaxNeighbors + j);
                                _terrainIndices.Add(neighborIndex1 * VertsPerCell + 1 + MaxNeighbors + k);

                                _waterBackfaceIndices.Add(neighborIndex1 * VertsPerCell + 1 + MaxNeighbors + k);
                                _waterBackfaceIndices.Add(i * VertsPerCell + 1 + MaxNeighbors + j);
                                _waterBackfaceIndices.Add(i * VertsPerCell + 1 + 2 * MaxNeighbors + ((j - 1 + neighborCount) % neighborCount));

                                break;
                            }
                        }
                    }
                    if (neighbor1 >= 0 && i < neighborIndex1 && i < neighborIndex2)
                    {
                        int neighborNeighborCount = (Sim.Icosphere.Neighbors[(neighborIndex2 + 1) * MaxNeighbors - 1] >= 0) ? MaxNeighbors : (MaxNeighbors - 1);
                        for (int k = 0; k < neighborNeighborCount; k++)
                        {
                            if (Sim.Icosphere.Neighbors[neighborIndex2 * MaxNeighbors + k] == i)
                            {
                                _terrainIndices.Add(i * VertsPerCell + 1 + 3 * MaxNeighbors + j);
                                _terrainIndices.Add(neighborIndex2 * VertsPerCell + 1 + 3 * MaxNeighbors + k);
                                _terrainIndices.Add(neighborIndex1 * VertsPerCell + 1 + 3 * MaxNeighbors + neighbor1);

                                _waterBackfaceIndices.Add(neighborIndex1 * VertsPerCell + 1 + 3 * MaxNeighbors + neighbor1);
                                _waterBackfaceIndices.Add(neighborIndex2 * VertsPerCell + 1 + 3 * MaxNeighbors + k);
                                _waterBackfaceIndices.Add(i * VertsPerCell + 1 + 3 * MaxNeighbors + j);

                                break;
                            }
                        }
                    }

                }
            }

        }

        private void OnDestroy()
        {
            Foliage.Dispose();
            foreach (var i in _viewStates)
            {
                i?.Dispose();
            }
            _terrainVertices.Dispose();
            _terrainNormals.Dispose();
            _terrainColors.Dispose();
            _terrainUVs.Dispose();

            _waterVertices.Dispose();
            _waterNormals.Dispose();
            _waterColors.Dispose();

            _overlayVertices.Dispose();
            _overlayColors.Dispose();

            _standardVerts.Dispose();

            _selectionCells.Dispose();

            _normalizedBlueBlackRed.Dispose();
            _normalizedRainbow.Dispose();
        }

        public int GetClosestVert(int triangleIndex, int vIndex)
        {
            return _terrainIndices[triangleIndex * 3 + vIndex] / VertsPerCell;
        }


        void OnNewGame(SimState simState)
        {

            Init();
            var jobHandle = CreateViewStateFromSimState(simState, _viewStates[_nextViewState], Sim.WorldData, Sim.StaticState, TerrainScale, default);
            UpdateMeshes(TerrainMesh.GetComponent<MeshFilter>().sharedMesh, WaterMesh.GetComponent<MeshFilter>().sharedMesh, WaterBackfaceMesh.GetComponent<MeshFilter>().sharedMesh, OverlayMesh.GetComponent<MeshFilter>().sharedMesh, _viewStates[_nextViewState], jobHandle);
            TerrainMesh.GetComponent<MeshCollider>().sharedMesh = null;
            TerrainMesh.GetComponent<MeshCollider>().sharedMesh = TerrainMesh.GetComponent<MeshFilter>().sharedMesh;
        }

        // Update is called once per frame
        void Update()
        {
            _lerpProgress = math.saturate(_lerpProgress + Time.deltaTime);
            var jobHandle = LerpViewState(Sim.StaticState.Count, _viewStates[_lastViewState], _viewStates[_nextViewState], _viewStates[_curViewState], _lerpProgress);
            UpdateMeshes(TerrainMesh.GetComponent<MeshFilter>().sharedMesh, WaterMesh.GetComponent<MeshFilter>().sharedMesh, WaterBackfaceMesh.GetComponent<MeshFilter>().sharedMesh, OverlayMesh.GetComponent<MeshFilter>().sharedMesh, _viewStates[_curViewState], jobHandle);

            OverlayMesh.SetActive(ActiveOverlay != MeshOverlay.None);
            TerrainMesh.SetActive(ActiveOverlay == MeshOverlay.None);
            WaterMesh.SetActive(ActiveOverlay == MeshOverlay.None);
            Foliage.FoliageParent.SetActive(ActiveOverlay == MeshOverlay.None);
        } 

        void OnSimTick(SimState simState)
        {
            _lastViewState = _curViewState;
            _nextViewState = (_nextViewState + 1) % 3;
            _curViewState = (_curViewState + 1) % 3;
            var jobHandle = CreateViewStateFromSimState(simState, _viewStates[_nextViewState], Sim.WorldData, Sim.StaticState, TerrainScale, default);
            jobHandle.Complete();
        }

        public JobHandle CreateViewStateFromSimState(SimState from, ViewState to, WorldData worldData, StaticState staticState, float terrainScale, JobHandle dependency)
        {
            //to.Ticks = from.PlanetState.Ticks;
            //to.Position = from.PlanetState.Position;
            //to.Rotation = math.degrees(from.PlanetState.Rotation);

            MeshOverlayData meshOverlay;
            bool useMeshOverlay = GetMeshOverlayData(ActiveOverlay, from, staticState, worldData, out meshOverlay);

            var buildRenderStateJobHandle = _perCellJobHelper.Schedule(
                true, 1, dependency,
                new CreateViewStateJob()
                {
                    TerrainColor = to.TerrainColor,
                    TerrainElevation = to.TerrainElevation,
                    WaterColor = to.WaterColor,
                    WaterElevation = to.WaterElevation,
                    OverlayColor = to.OverlayColor,
                    TerrainState = to.TerrainState,

                    Elevation = from.Elevation,
                    WaterDepth = from.WaterDepth,
                    Dirt = from.Dirt,
                    Sand = from.Sand,
                    Vegetation = from.Vegetation,
                    Ice = from.IceMass,
                    Explored = from.Explored,
                    MeshOverlayMin = meshOverlay.Colors.Min,
                    MeshOverlayInverseRange = meshOverlay.InverseRange,
                    MeshOverlayData = meshOverlay.Values,
                    MeshOverlayColors = meshOverlay.Colors.ColorValuePairs,
                    MeshOverlayActive = ActiveOverlay != MeshOverlay.None,
                    PlanetRadius = staticState.PlanetRadius,
                    TerrainScale = terrainScale,
                });

            return buildRenderStateJobHandle;
        }

        public JobHandle LerpViewState(int cellCount, ViewState lastState, ViewState nextState, ViewState state, float t)
        {
            int batchCount = 1;
            NativeList<JobHandle> dependencies = new NativeList<JobHandle>(Allocator.TempJob);
            dependencies.Add((new LerpJobfloat { Progress = t, Out = state.TerrainElevation, Start = lastState.TerrainElevation, End = nextState.TerrainElevation }).Schedule(cellCount, batchCount));
            dependencies.Add((new LerpJobfloat4 { Progress = t, Out = state.TerrainColor, Start = lastState.TerrainColor, End = nextState.TerrainColor }).Schedule(cellCount, batchCount));
            dependencies.Add((new LerpJobfloat { Progress = t, Out = state.WaterElevation, Start = lastState.WaterElevation, End = nextState.WaterElevation }).Schedule(cellCount, batchCount));
            dependencies.Add((new LerpJobfloat4 { Progress = t, Out = state.WaterColor, Start = lastState.WaterColor, End = nextState.WaterColor }).Schedule(cellCount, batchCount));
            dependencies.Add((new LerpJobfloat4 { Progress = t, Out = state.TerrainState, Start = lastState.TerrainState, End = nextState.TerrainState }).Schedule(cellCount, batchCount));

            if (ActiveOverlay != MeshOverlay.None)
            {
                dependencies.Add((new LerpJobColor32 { Progress = t, Out = state.OverlayColor, Start = lastState.OverlayColor, End = nextState.OverlayColor }).Schedule(cellCount, batchCount));
            }

            var jobHandle = JobHandle.CombineDependencies(dependencies);
            dependencies.Dispose();
            return jobHandle;
        }

        public void UpdateMeshes(Mesh terrainMesh, Mesh waterMesh, Mesh waterBackfaceMesh, Mesh overlayMesh, ViewState viewState, JobHandle dependencies)
        {
            var getVertsHandle = _perVertexJobHelper.Schedule(
                JobType.Schedule, 64, dependencies,
                new BuildTerrainVertsJob()
                {
                    VTerrainPosition = _terrainVertices,
                    VTerrainColor = _terrainColors,
                    VWaterPosition = _waterVertices,
                    VWaterNormal = _waterNormals,
                    VWaterColor = _waterColors,
                    VOverlayColor = _overlayColors,
                    VOverlayPosition = _overlayVertices,
                    VTerrainUVs = _terrainUVs,

                    TerrainElevation = viewState.TerrainElevation,
                    TerrainColor = viewState.TerrainColor,
                    WaterElevation = viewState.WaterElevation,
                    WaterColor = viewState.WaterColor,
                    OverlayColor = viewState.OverlayColor,
                    TerrainState = viewState.TerrainState,
                    Selection = _selectionCells,
                    StandardVerts = _standardVerts,
                });


            getVertsHandle.Complete();

            terrainMesh.SetVertices(_terrainVertices);
            //terrainMesh.SetNormals(_terrainNormals);
            terrainMesh.SetColors(_terrainColors);
            terrainMesh.SetUVs(0, _terrainUVs);

            if (ActiveOverlay != MeshOverlay.None || !_indicesInitialized)
            {
                overlayMesh.SetVertices(_overlayVertices);
                overlayMesh.SetColors(_overlayColors);
            }

            waterMesh.SetVertices(_waterVertices);
            waterMesh.SetNormals(_waterNormals);
            waterMesh.SetColors(_waterColors);
            waterMesh.SetUVs(0, _terrainUVs);
            waterBackfaceMesh.SetVertices(_waterVertices);

            if (!_indicesInitialized)
            {
                terrainMesh.SetTriangles(_terrainIndices.ToArray(), 0);
                waterMesh.SetTriangles(_terrainIndices.ToArray(), 0);
                waterBackfaceMesh.SetTriangles(_waterBackfaceIndices.ToArray(), 0);
                overlayMesh.SetTriangles(_terrainIndices.ToArray(), 0);
                _indicesInitialized = true;
            }

            terrainMesh.RecalculateBounds();
            waterMesh.RecalculateBounds();
            waterBackfaceMesh.RecalculateBounds();
            //_cloudMesh.RecalculateBounds();
            terrainMesh.RecalculateNormals();
            //	_waterMesh.RecalculateNormals();
            //	_cloudMesh.RecalculateNormals();
            //_terrainMesh.RecalculateTangents();
            //_waterMesh.RecalculateTangents();

            if (ActiveOverlay != MeshOverlay.None)
            {
                overlayMesh.RecalculateBounds();
                overlayMesh.RecalculateNormals();
            }


        }

        public void HighlightCells(List<Tuple<int, float>> cells)
        {
            if (_indicesInitialized)
            {
                Utils.MemsetArray(_selectionCells.Length, default, _selectionCells, 0).Complete();
                for (int i = 0; i < cells.Count; i++)
                {
                    _selectionCells[cells[i].Item1] = cells[i].Item2;
                }
            }
        }


        private bool GetMeshOverlayData(MeshOverlay activeOverlay, SimState simState, StaticState staticState, WorldData worldData, out MeshOverlayData overlay)
        {
            float ticksPerYear = worldData.TicksPerSecond * 60 * 60 * 24 * 365;
            MeshOverlayColors colors;
            if (OverlayColors.TryGetValue(activeOverlay, out colors))
            {
                switch (activeOverlay)
                {
                    case MeshOverlay.AirTemperature:
                        overlay = new MeshOverlayData(colors, simState.Temperature);
                        return true;
                }
            }
            overlay = new MeshOverlayData(OverlayColors[MeshOverlay.AirTemperature], simState.Temperature);
            return false;

        }

    }
}