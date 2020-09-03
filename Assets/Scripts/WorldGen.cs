using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
public static class WorldGen
{
	static public void Generate(int columns, int height, WorldGenData data, SimState state, StaticState staticState)
	{
		for (int i=0;i<columns;i++)
		{
			var pos = staticState.SphericalPosition[i];
			state.Elevation[i] = (0.5f * noise.snoise(pos) + 0.4f * noise.snoise(pos * 3) + 0.1f * noise.snoise(pos * 9)) * 12000f - 1000f;
			state.WaterDepth[i] = math.max(0, -state.Elevation[i]);
			state.Dirt[i] = math.max(0, -state.Elevation[i]);
			state.Sand[i] = math.max(0, -state.Elevation[i]);
			state.Vegetation[i] = math.max(0, -state.Elevation[i]);
			state.IceMass[i] = noise.snoise(pos) * (math.saturate(state.Elevation[i]) + 1000) * math.abs(staticState.Coordinate[i].y) / 1000;
			state.Dirt[i] = math.saturate(noise.snoise(pos));
			state.Sand[i] = math.saturate(noise.snoise(pos + new float3(6567)));
			state.Vegetation[i] = math.saturate(noise.snoise(pos + new float3(543252)));
		}
	}
}
