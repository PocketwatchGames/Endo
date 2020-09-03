using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Unity.Burst;
using UnityEditorInternal.VersionControl;
using System;

public class PlanetView
{

	public const int VertsPerCell = 25;
	public const int VertsPerCloud = 25;
	public const int MaxNeighbors = 6;

	private List<int> _terrainIndices;
	private NativeArray<float3> _terrainVertices;
	private NativeArray<float4> _terrainColors;
	private NativeArray<float3> _terrainNormals;

	private List<int> _waterBackfaceIndices;
	private NativeArray<float3> _waterVertices;
	private NativeArray<float4> _waterColors;
	private NativeArray<float3> _waterNormals;

	private NativeArray<float3> _overlayVertices;
	private NativeArray<float4> _overlayColors;
	private NativeArray<float> _selectionCells;

	private NativeArray<float3> _standardVerts;

	private JobHelper _perCellJobHelper;
	private JobHelper _perVertexJobHelper;

	private bool _indicesInitialized;

	public void Init(Icosphere icosphere, int cellCount, float slopeMin, float slopeMax)
	{
		_perCellJobHelper = new JobHelper(cellCount);
		_perVertexJobHelper = new JobHelper(cellCount * VertsPerCell);

		_terrainVertices = new NativeArray<float3>(cellCount * VertsPerCell, Allocator.Persistent);
		_terrainNormals = new NativeArray<float3>(cellCount * VertsPerCell, Allocator.Persistent);
		_terrainColors = new NativeArray<float4>(cellCount * VertsPerCell, Allocator.Persistent);
		_terrainIndices = new List<int>();

		_waterVertices = new NativeArray<float3>(cellCount * VertsPerCell, Allocator.Persistent);
		_waterNormals = new NativeArray<float3>(cellCount * VertsPerCell, Allocator.Persistent);
		_waterColors = new NativeArray<float4>(cellCount * VertsPerCell, Allocator.Persistent);
		_waterBackfaceIndices = new List<int>();

		_overlayVertices = new NativeArray<float3>(cellCount * VertsPerCell, Allocator.Persistent);
		_overlayColors = new NativeArray<float4>(cellCount * VertsPerCell, Allocator.Persistent);
		_selectionCells = new NativeArray<float>(cellCount, Allocator.Persistent);

		_standardVerts = new NativeArray<float3>(cellCount * VertsPerCell, Allocator.Persistent);

		Unity.Mathematics.Random random = new Unity.Mathematics.Random(1);

		const float maxSlope = 3;
		for (int i = 0; i < icosphere.Vertices.Length; i++)
		{
			float3 pos = icosphere.Vertices[i];
			_standardVerts[i * VertsPerCell] = pos;
			int neighborCount = (icosphere.Neighbors[(i + 1) * MaxNeighbors - 1] >= 0) ? MaxNeighbors : (MaxNeighbors - 1);
			for (int j = 0; j < neighborCount; j++)
			{
				int neighborIndex1 = icosphere.Neighbors[i * MaxNeighbors + j];
				int neighborIndex2 = icosphere.Neighbors[i * MaxNeighbors + (j + 1) % neighborCount];

				{
					float slope = random.NextFloat() * (slopeMax - slopeMin) + slopeMin;
					float3 slopePoint = (icosphere.Vertices[neighborIndex1] + icosphere.Vertices[neighborIndex2] + pos * (1 + slope)) / (maxSlope + slope);
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
					int neighborNeighborCount = (icosphere.Neighbors[(neighborIndex1 + 1) * MaxNeighbors - 1] >= 0) ? MaxNeighbors : (MaxNeighbors - 1);
					for (int k = 0; k < neighborNeighborCount; k++)
					{
						if (icosphere.Neighbors[neighborIndex1 * MaxNeighbors + k] == i)
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
					int neighborNeighborCount = (icosphere.Neighbors[(neighborIndex2 + 1) * MaxNeighbors - 1] >= 0) ? MaxNeighbors : (MaxNeighbors - 1);
					for (int k = 0; k < neighborNeighborCount; k++)
					{
						if (icosphere.Neighbors[neighborIndex2 * MaxNeighbors + k] == i)
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

	public void Dispose()
	{
		_terrainVertices.Dispose();
		_terrainNormals.Dispose();
		_terrainColors.Dispose();

		_waterVertices.Dispose();
		_waterNormals.Dispose();
		_waterColors.Dispose();

		_overlayVertices.Dispose();
		_overlayColors.Dispose();

		_standardVerts.Dispose();

		_selectionCells.Dispose();
	}

	public int GetClosestVert(int triangleIndex, int vIndex)
	{
		return _terrainIndices[triangleIndex * 3 + vIndex] / VertsPerCell;
	}

	public JobHandle BuildRenderState(SimState from, ViewState to, WorldData worldData, StaticState staticState, float terrainScale, JobHandle dependency)
	{
		//to.Ticks = from.PlanetState.Ticks;
		//to.Position = from.PlanetState.Position;
		//to.Rotation = math.degrees(from.PlanetState.Rotation);

		var buildRenderStateJobHandle = _perCellJobHelper.Schedule(
			true, 1,
			new BuildRenderStateCellJob()
			{
				TerrainColor = to.TerrainColor,
				TerrainElevation = to.TerrainElevation,
				WaterColor = to.WaterColor,
				WaterElevation = to.WaterElevation,

				Elevation = from.Elevation,
				WaterDepth = from.WaterDepth,
				Dirt = from.Dirt,
				Sand = from.Sand,
				Vegetation = from.Vegetation,
				Ice = from.IceMass,
				PlanetRadius = staticState.PlanetRadius,
				TerrainScale = terrainScale
			}, dependency);

		return buildRenderStateJobHandle;
	}

	public JobHandle Lerp(int cellCount, ViewState lastState, ViewState nextState, ViewState state, float t)
	{
		int batchCount = 1;
		NativeList<JobHandle> dependencies = new NativeList<JobHandle>(Allocator.TempJob);
		dependencies.Add((new LerpJobfloat { Progress = t, Out = state.TerrainElevation, Start = lastState.TerrainElevation, End = nextState.TerrainElevation }).Schedule(cellCount, batchCount));
		dependencies.Add((new LerpJobfloat4 { Progress = t, Out = state.TerrainColor, Start = lastState.TerrainColor, End = nextState.TerrainColor }).Schedule(cellCount, batchCount));
		dependencies.Add((new LerpJobfloat { Progress = t, Out = state.WaterElevation, Start = lastState.WaterElevation, End = nextState.WaterElevation }).Schedule(cellCount, batchCount));
		dependencies.Add((new LerpJobfloat4 { Progress = t, Out = state.WaterColor, Start = lastState.WaterColor, End = nextState.WaterColor }).Schedule(cellCount, batchCount));

		var jobHandle = JobHandle.CombineDependencies(dependencies);
		dependencies.Dispose();
		return jobHandle;
	}

	public void Update(Mesh terrainMesh, Mesh waterMesh, Mesh waterBackfaceMesh, Mesh overlayMesh, ViewState viewState, JobHandle dependencies)
	{
		var getVertsHandle = _perVertexJobHelper.Schedule(
			JobType.Schedule, 64,
			new BuildTerrainVertsJob()
			{
				VTerrainPosition = _terrainVertices,
				VTerrainColor = _terrainColors,
				VWaterPosition = _waterVertices,
				VWaterNormal = _waterNormals,
				VWaterColor = _waterColors,
				VOverlayColor = _overlayColors,
				VOverlayPosition = _overlayVertices,

				TerrainElevation = viewState.TerrainElevation,
				TerrainColor = viewState.TerrainColor,
				WaterElevation = viewState.WaterElevation,
				WaterColor = viewState.WaterColor,
				Selection = _selectionCells,
				StandardVerts = _standardVerts,
			}, dependencies);


		getVertsHandle.Complete();

		terrainMesh.SetVertices(_terrainVertices);
		//terrainMesh.SetNormals(_terrainNormals);
		terrainMesh.SetColors(_terrainColors);

		overlayMesh.SetVertices(_overlayVertices);
		overlayMesh.SetUVs(1, _overlayColors);

		waterMesh.SetVertices(_waterVertices);
		waterMesh.SetNormals(_waterNormals);
		waterMesh.SetColors(_waterColors);

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
		overlayMesh.RecalculateBounds();
		//_cloudMesh.RecalculateBounds();

		terrainMesh.RecalculateNormals();
		//	_waterMesh.RecalculateNormals();
		//	_cloudMesh.RecalculateNormals();
		//_terrainMesh.RecalculateTangents();
		//_waterMesh.RecalculateTangents();

	}

	public void HighlightCells(List<Tuple<int, float>> cells)
	{
		if (_indicesInitialized)
		{
			Utils.MemsetArray(GameManager.Active.StaticState.Count, default, _selectionCells, 0).Complete();
			for (int i = 0; i < cells.Count; i++)
			{
				_selectionCells[cells[i].Item1] = cells[i].Item2;
			}
		}
	}
}


[BurstCompile]
public struct BuildTerrainVertsJob : IJobParallelFor
{
	public NativeArray<float3> VTerrainPosition;
	public NativeArray<float4> VTerrainColor;
	public NativeArray<float3> VWaterPosition;
	public NativeArray<float3> VWaterNormal;
	public NativeArray<float4> VWaterColor;
	public NativeArray<float4> VOverlayColor;
	public NativeArray<float3> VOverlayPosition;

	[ReadOnly] public NativeArray<float> Selection;
	[ReadOnly] public NativeArray<float> TerrainElevation;
	[ReadOnly] public NativeArray<float> WaterElevation;
	[ReadOnly] public NativeArray<float4> TerrainColor;
	[ReadOnly] public NativeArray<float4> WaterColor;
	[ReadOnly] public NativeArray<float3> StandardVerts;

	public void Execute(int i)
	{
		float3 v = StandardVerts[i];
		int j = (int)(i / PlanetView.VertsPerCell);

		VTerrainPosition[i] = v * TerrainElevation[j];
		VWaterPosition[i] = v * WaterElevation[j];
		VTerrainColor[i] = TerrainColor[j];


		VWaterColor[i] = WaterColor[j];
		VWaterNormal[i] = v;

		VOverlayPosition[i] = v * (math.max(TerrainElevation[j], WaterElevation[j]) + 0.001f);
		VOverlayColor[i] = new float4(Selection[j], 0,0,0);
	}

}

[BurstCompile]
public struct BuildRenderStateCellJob : IJobParallelFor
{
	public NativeArray<float4> TerrainColor;
	public NativeArray<float> TerrainElevation;
	public NativeArray<float4> WaterColor;
	public NativeArray<float> WaterElevation;

	[ReadOnly] public NativeArray<float> Elevation;
	[ReadOnly] public NativeArray<float> WaterDepth;
	[ReadOnly] public NativeArray<float> Ice;
	[ReadOnly] public NativeArray<float> Vegetation;
	[ReadOnly] public NativeArray<float> Sand;
	[ReadOnly] public NativeArray<float> Dirt;
	[ReadOnly] public float TerrainScale;
	[ReadOnly] public float PlanetRadius;

	public void Execute(int i)
	{
		float elevation = Elevation[i];

		float4 terrainColor = new float4(0.4f, 0.4f, 0.4f, 1);
		terrainColor = math.lerp(terrainColor, new float4(0.6f, 0.5f, 0.2f, 1.0f), math.saturate(Dirt[i]));
		terrainColor = math.lerp(terrainColor, new float4(0.6f, 0.6f, 0.4f, 1.0f), math.saturate(Sand[i]));
		terrainColor = math.lerp(terrainColor, new float4(0.2f, 0.7f, 0.1f, 1.0f), math.saturate(Vegetation[i]));
		terrainColor = math.lerp(terrainColor, new float4(0.8f, 0.9f, 1.0f, 1.0f), math.saturate(Ice[i]));
		TerrainColor[i] = terrainColor;
		TerrainElevation[i] = (elevation * TerrainScale + PlanetRadius) / PlanetRadius;

		float waterDepth = WaterDepth[i];
		WaterColor[i] = new float4(0,0,1,1);
		WaterElevation[i] = ((waterDepth == 0) ? 0.99f : ((elevation + waterDepth) * TerrainScale + PlanetRadius) / PlanetRadius);
	}


}


