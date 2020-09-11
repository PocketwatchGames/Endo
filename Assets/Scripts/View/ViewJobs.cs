using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Unity.Burst;
using UnityEditorInternal.VersionControl;
using System;

namespace Endo
{


	[BurstCompile]
	public struct BuildTerrainVertsJob : IJobParallelFor
	{
		public NativeArray<float3> VTerrainPosition;
		public NativeArray<float4> VTerrainColor;
		public NativeArray<float3> VWaterPosition;
		public NativeArray<float3> VWaterNormal;
		public NativeArray<float4> VWaterColor;
		public NativeArray<Color32> VOverlayColor;
		public NativeArray<float3> VOverlayPosition;
		public NativeArray<float4> VTerrainUVs;

		[ReadOnly] public NativeArray<float> Selection;
		[ReadOnly] public NativeArray<float> TerrainElevation;
		[ReadOnly] public NativeArray<float> WaterElevation;
		[ReadOnly] public NativeArray<float4> TerrainColor;
		[ReadOnly] public NativeArray<float4> WaterColor;
		[ReadOnly] public NativeArray<float3> StandardVerts;
		[ReadOnly] public NativeArray<Color32> OverlayColor;
		[ReadOnly] public NativeArray<float4> TerrainState;

		public void Execute(int i)
		{
			float3 v = StandardVerts[i];
			int j = (int)(i / ViewComponent.VertsPerCell);

			VTerrainPosition[i] = v * TerrainElevation[j];
			VWaterPosition[i] = v * WaterElevation[j];
			VTerrainColor[i] = TerrainColor[j];
			VTerrainUVs[i] = TerrainState[j];

			VWaterColor[i] = WaterColor[j];
			VWaterNormal[i] = v;

			VOverlayPosition[i] = v * (math.max(TerrainElevation[j], WaterElevation[j]) + 0.001f);
			VOverlayColor[i] = OverlayColor[j];
		}

	}

	[BurstCompile]
	public struct CreateViewStateJob : IJobParallelFor
	{
		public NativeArray<float4> TerrainColor;
		public NativeArray<float> TerrainElevation;
		public NativeArray<float4> WaterColor;
		public NativeArray<float> WaterElevation;
		public NativeArray<float4> TerrainState;
		public NativeArray<Color32> OverlayColor;

		[ReadOnly] public NativeArray<float> Elevation;
		[ReadOnly] public NativeArray<float> WaterDepth;
		[ReadOnly] public NativeArray<float> Ice;
		[ReadOnly] public NativeArray<float> Vegetation;
		[ReadOnly] public NativeArray<float> Sand;
		[ReadOnly] public NativeArray<float> Dirt;
		[ReadOnly] public NativeSlice<float> MeshOverlayData;
		[ReadOnly] public NativeSlice<float> Explored;
		[ReadOnly] public NativeArray<CVP> MeshOverlayColors;
		[ReadOnly] public bool MeshOverlayActive;
		[ReadOnly] public float MeshOverlayMin;
		[ReadOnly] public float MeshOverlayInverseRange;
		[ReadOnly] public float TerrainScale;
		[ReadOnly] public float PlanetRadius;

		public void Execute(int i)
		{
			float elevation = Elevation[i];

			if (Explored[i] > 0)
			{
				float4 terrainColor = new float4(0.4f, 0.4f, 0.4f, 1);
				TerrainElevation[i] = (elevation * TerrainScale + PlanetRadius) / PlanetRadius;
	
				terrainColor = math.lerp(terrainColor, new float4(0.6f, 0.5f, 0.2f, 1.0f), math.saturate(Dirt[i]));
				terrainColor = math.lerp(terrainColor, new float4(0.6f, 0.6f, 0.4f, 1.0f), math.saturate(Sand[i]));
				terrainColor = math.lerp(terrainColor, new float4(0.2f, 0.7f, 0.1f, 1.0f), math.saturate(Vegetation[i]));
				terrainColor = math.lerp(terrainColor, new float4(0.8f, 0.9f, 1.0f, 1.0f), math.saturate(Ice[i]));
				TerrainColor[i] = terrainColor;

			}
			else
			{
				TerrainElevation[i] = 1;
				TerrainColor[i] = new float4(0, 0, 0, 1);
			}

			float waterDepth = WaterDepth[i];
			WaterColor[i] = waterDepth > 0 ? new float4(0, 0, 1, math.saturate(waterDepth / 100)) : new float4(0, 0, 0, 0);
			WaterElevation[i] = ((waterDepth == 0) ? 1 : ((elevation + waterDepth) * TerrainScale + PlanetRadius) / PlanetRadius);
			TerrainState[i] = new float4(0,0,0,Explored[i]);

			if (MeshOverlayActive)
			{
				OverlayColor[i] = CVP.Lerp(MeshOverlayColors, (MeshOverlayData[i] - MeshOverlayMin) * MeshOverlayInverseRange);
			}
			else
			{
				OverlayColor[i] = new Color32();
			}

		}


	}


}