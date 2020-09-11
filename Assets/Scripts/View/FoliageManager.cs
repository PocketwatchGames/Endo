using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System.Xml.Serialization;
using System;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;

namespace Endo
{
	[Serializable]
	public class FoliageManager
	{
		public struct FoliageData
		{
			public int CellIndex;
			public float CoverageMin;
			public float CoverageRangeInverse;
			public float3 BaseScale;
			public float GrowthSpeed;
			public float GrowthDelay;
			public float3 Position;
			public Quaternion Rotation;
			public int FoliageType;
		}
		public struct FoliageState
		{
			public float DestScale;
		}
		public struct FoliageTransform
		{
			public float3 LocalPosition;
			public float3 LocalScale;
			public float Scale;
			public float GrowthDelay;
			public bool Active;
		}

		public int MaxFoliagePerCell = 4;
		public float TreeScale = 0.02f;
		public float TreeScaleRange = 0.2f;
		public float PerturbCellRadius = 0.4f;
		public float MinTreeScale = 0.5f;
		public float FloraCoveragePowerForTrees = 0.1f;
		public float TreeGrowthSpeed = 4;
		public float TreeGrowthSpeedRange = 4;
		public float TreeGrowthDelayRange = 1.5f;
		public float SpawnScale = 0.01f;

		public GameObject FoliageParent;
		public List<GameObject> FoliagePrefabs;

		private GameObject[] _foliage;
		private int _maxFoliagePerCell;
		private NativeArray<FoliageData> _foliageData;
		private NativeArray<FoliageState> _foliageState;
		private NativeArray<FoliageTransform> _foliageTransform;

		public void Init(int cellCount, ref StaticState staticState)
		{
			_maxFoliagePerCell = MaxFoliagePerCell;
			_foliage = new GameObject[cellCount * _maxFoliagePerCell];
			_foliageData = new NativeArray<FoliageData>(cellCount * _maxFoliagePerCell, Allocator.Persistent);
			_foliageState = new NativeArray<FoliageState>(cellCount * _maxFoliagePerCell, Allocator.Persistent);
			_foliageTransform = new NativeArray<FoliageTransform>(cellCount * _maxFoliagePerCell, Allocator.Persistent);

			var initFoliageJob = new InitFoliageJob()
			{
				Data = _foliageData,
				SphericalPosition = staticState.SphericalPosition,
				FoliageTypes = FoliagePrefabs.Count,
				FloraCoveragePowerForTrees = FloraCoveragePowerForTrees,
				GrowthDelayRange = TreeGrowthDelayRange,
				GrowthSpeed = TreeGrowthSpeed,
				GrowthSpeedRange = TreeGrowthSpeedRange,
				MaxFoliagePerCell = _maxFoliagePerCell,
				PerturbDistance = PerturbCellRadius * math.length(staticState.SphericalPosition[0] - staticState.SphericalPosition[staticState.Neighbors[0]]),
				RandomSeed = 3452,
				Scale = TreeScale,
				ScaleRange = TreeScaleRange,
			};
			var handle = initFoliageJob.Schedule(_foliageData.Length, 100);
			handle.Complete();

		}

		public void Dispose()
		{
			_foliageData.Dispose();
			_foliageState.Dispose();
			_foliageTransform.Dispose();
		}

		public JobHandle Tick(ref TempState tempState, JobHandle dependency)
		{
			var updateStateJob = new UpdateFoliageSimStateJob()
			{
				State = _foliageState,
				Data = _foliageData,
				FloraCoverage = tempState.FloraCoverage,
				MinScale = MinTreeScale
			};
			return updateStateJob.Schedule(_foliageState.Length, 100, dependency);
		}


		public void Update(ref ViewState viewState)
		{
			var updateStateJob = new UpdateFoliageRenderStateJob()
			{
				RenderState = _foliageTransform,
				SimState = _foliageState,
				Data = _foliageData,
				Elevation = viewState.TerrainElevation,
				SpawnScale = SpawnScale,
				DeltaTime = Time.deltaTime
			};
			var handle = updateStateJob.Schedule(_foliageTransform.Length, 100);
			handle.Complete();

			for (int i = 0; i < _foliageTransform.Length; i++)
			{
				if (!_foliageTransform[i].Active)
				{
					if (_foliage[i] != null)
					{
						GameObject.Destroy(_foliage[i]);
						_foliage[i] = null;
					}
				}
				else
				{
					if (_foliage[i] == null)
					{
						_foliage[i] = GameObject.Instantiate(FoliagePrefabs[_foliageData[i].FoliageType]);
						_foliage[i].transform.SetParent(FoliageParent.transform);
						_foliage[i].transform.localRotation = _foliageData[i].Rotation;
					}
					_foliage[i].transform.localPosition = _foliageTransform[i].LocalPosition;
					_foliage[i].transform.localScale = _foliageTransform[i].LocalScale;
				}
			}
		}

		[BurstCompile]
		public struct InitFoliageJob : IJobParallelFor
		{
			public NativeArray<FoliageData> Data;
			[ReadOnly] public NativeArray<float3> SphericalPosition;
			[ReadOnly] public float PerturbDistance;
			[ReadOnly] public int MaxFoliagePerCell;
			[ReadOnly] public float Scale;
			[ReadOnly] public float ScaleRange;
			[ReadOnly] public float GrowthSpeed;
			[ReadOnly] public float GrowthSpeedRange;
			[ReadOnly] public float GrowthDelayRange;
			[ReadOnly] public int RandomSeed;
			[ReadOnly] public int FoliageTypes;
			[ReadOnly] public float FloraCoveragePowerForTrees;
			public void Execute(int i)
			{
				int cellIndex = i / MaxFoliagePerCell;
				var random = new Unity.Mathematics.Random((uint)(RandomSeed ^ i + i ^ RandomSeed + i + RandomSeed) + 1);
				float3 pos = SphericalPosition[cellIndex];
				float3 forward = math.cross(pos, new float3(0, 1, 0));
				float3 right = math.cross(forward, pos);
				float2 perturb = PerturbDistance * (new float2(random.NextFloat(), random.NextFloat()) * 2 - 1);
				float3 scale = Scale + random.NextFloat() * ScaleRange;
				float growthSpeed = GrowthSpeed + random.NextFloat() * GrowthSpeedRange;
				float growthDelay = random.NextFloat() * GrowthDelayRange;
				var position = pos + perturb.x * right + perturb.y * forward;
				var rot = Quaternion.FromToRotation(Vector3.up, pos) * Quaternion.AngleAxis(random.NextFloat(360), Vector3.up);
				int foliageType = random.NextInt(FoliageTypes);
				int treeIndex = i % MaxFoliagePerCell;
				float coverageMin = math.pow((float)(treeIndex + 1) / (MaxFoliagePerCell + 1), FloraCoveragePowerForTrees);
				float coverageMax = math.pow(math.saturate((float)(treeIndex + 2) / (MaxFoliagePerCell + 1)), FloraCoveragePowerForTrees);
				Data[i] = new FoliageData()
				{
					BaseScale = scale,
					GrowthSpeed = growthSpeed,
					GrowthDelay = growthDelay,
					Position = position,
					Rotation = rot,
					CellIndex = cellIndex,
					FoliageType = foliageType,
					CoverageMin = coverageMin,
					CoverageRangeInverse = 1.0f / (coverageMax - coverageMin),
				};
			}
		}
		[BurstCompile]
		public struct UpdateFoliageSimStateJob : IJobParallelFor
		{
			public NativeArray<FoliageState> State;
			[ReadOnly] public NativeArray<FoliageData> Data;
			[ReadOnly] public NativeArray<float> FloraCoverage;
			[ReadOnly] public float MinScale;
			public void Execute(int i)
			{
				var data = Data[i];
				float coverage = FloraCoverage[data.CellIndex];
				float scale = math.saturate((coverage - data.CoverageMin) * data.CoverageRangeInverse);

				State[i] = new FoliageState()
				{
					DestScale = scale > 0 ? (MinScale + (1.0f - MinScale) * scale) : 0,
				};
			}
		}
		[BurstCompile]
		public struct UpdateFoliageRenderStateJob : IJobParallelFor
		{
			public NativeArray<FoliageTransform> RenderState;
			[ReadOnly] public NativeArray<FoliageState> SimState;
			[ReadOnly] public NativeArray<FoliageData> Data;
			[ReadOnly] public NativeArray<float> Elevation;
			[ReadOnly] public float SpawnScale;
			[ReadOnly] public float DeltaTime;
			public void Execute(int i)
			{
				var newState = RenderState[i];

				float curScale = RenderState[i].Scale;
				float destScale = SimState[i].DestScale;
				if (newState.GrowthDelay > 0)
				{
					newState.GrowthDelay -= DeltaTime;
				}
				else
				{
					var data = Data[i];
					if (!newState.Active && destScale > 0)
					{
						newState.GrowthDelay = data.GrowthDelay;
						newState.Scale = SpawnScale;
					}
					else
					{
						newState.Scale = (destScale - curScale) * math.min(1, DeltaTime * data.GrowthSpeed) + curScale;
						newState.LocalPosition = data.Position * Elevation[data.CellIndex];
						newState.LocalScale = data.BaseScale * newState.Scale;
					}
				}
				newState.Active = destScale > curScale || newState.Scale > 0.01f;
				RenderState[i] = newState;
			}
		}

	}
}