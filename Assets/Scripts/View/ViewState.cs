using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Endo
{
	public class ViewState
	{
		public NativeArray<float4> TerrainColor;
		public NativeArray<float4> WaterColor;
		public NativeArray<float> TerrainElevation;
		public NativeArray<float> WaterElevation;
		public NativeArray<float> CloudDensity;
		public NativeArray<float> IceDensity;
		public NativeArray<float4> TerrainState;
		public NativeArray<Color32> OverlayColor;

		public void Init(int columns, int height)
		{
			TerrainColor = new NativeArray<float4>(columns, Allocator.Persistent);
			WaterColor = new NativeArray<float4>(columns, Allocator.Persistent);
			OverlayColor = new NativeArray<Color32>(columns, Allocator.Persistent);
			TerrainElevation = new NativeArray<float>(columns, Allocator.Persistent);
			WaterElevation = new NativeArray<float>(columns, Allocator.Persistent);
			CloudDensity = new NativeArray<float>(columns, Allocator.Persistent);
			IceDensity = new NativeArray<float>(columns, Allocator.Persistent);
			TerrainState = new NativeArray<float4>(columns, Allocator.Persistent);
		}

		public void Dispose()
		{
			TerrainColor.Dispose();
			WaterColor.Dispose();
			OverlayColor.Dispose();
			TerrainElevation.Dispose();
			WaterElevation.Dispose();
			CloudDensity.Dispose();
			IceDensity.Dispose();
			TerrainState.Dispose();

		}
	}
}