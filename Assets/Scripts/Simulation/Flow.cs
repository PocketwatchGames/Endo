using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;


[BurstCompile]
public struct UpdateSurfaceElevationJob : IJobParallelFor
{
	public NativeArray<float> SurfaceElevation;
	[ReadOnly] public NativeSlice<float> WaterDepth;
	[ReadOnly] public NativeArray<float> Elevation;

	public void Execute(int i)
	{
		SurfaceElevation[i] = Elevation[i] + WaterDepth[i];
	}
}
[BurstCompile]
public struct UpdateFlowVelocityJob : IJobParallelFor
{
	public NativeArray<float> Flow;

	[ReadOnly] public NativeArray<float> LastFlow;
	[ReadOnly] public NativeArray<float> SurfaceElevation;
	[ReadOnly] public NativeSlice<float> WaterDepth;
	[ReadOnly] public NativeArray<float> NeighborDistInverse;
	[ReadOnly] public NativeArray<int> Neighbors;
	[ReadOnly] public float SecondsPerTick;
	[ReadOnly] public float Gravity;
	[ReadOnly] public float Damping;
	[ReadOnly] public float ViscosityInverse;

	public void Execute(int i)
	{
		int nIndex = Neighbors[i];
		if (nIndex < 0)
		{
			return;
		}
		int cellIndex = i / StaticState.MaxNeighbors;
		float waterDepth = WaterDepth[cellIndex];
		float elevationDiff = SurfaceElevation[cellIndex] - SurfaceElevation[nIndex];
		int upwindIndex = elevationDiff > 0 ? cellIndex : nIndex;
		float acceleration = Gravity * elevationDiff * NeighborDistInverse[i];

		float v = LastFlow[i] * Damping + acceleration * SecondsPerTick * ViscosityInverse;
		Flow[i] = v;
	}
}

[BurstCompile]
public struct SumOutgoingFlowJob : IJobParallelFor
{
	public NativeArray<float> OutgoingFlow;
	[ReadOnly] public NativeArray<float> Flow;
	public void Execute(int i)
	{
		float outgoingFlow = 0;
		for (int j = 0; j < StaticState.MaxNeighbors; j++)
		{
			float f = Flow[i * StaticState.MaxNeighbors + j];
			outgoingFlow += math.max(0, f);
		}
		OutgoingFlow[i] = outgoingFlow;
	}
}

[BurstCompile]
public struct LimitOutgoingFlowJob : IJobParallelFor
{
	public NativeArray<float> Flow;
	public NativeArray<float> FlowPercent;
	[ReadOnly] public NativeArray<float> OutgoingFlow;
	[ReadOnly] public NativeSlice<float> WaterDepth;
	[ReadOnly] public NativeArray<int> Neighbors;
	public void Execute(int i)
	{
		int nIndex = Neighbors[i];
		if (nIndex < 0)
		{
			return;
		}
		float flow = Flow[i];
		if (flow > 0)
		{
			int cellIndex = i / StaticState.MaxNeighbors;
			float waterDepth = WaterDepth[cellIndex];
			if (waterDepth > 0)
			{
				float outgoing = OutgoingFlow[cellIndex];
				if (outgoing > 0)
				{
					Flow[i] = flow * math.min(1, waterDepth / outgoing);
					FlowPercent[i] = Flow[i] / waterDepth;
				}
			}
			else
			{
				Flow[i] = 0;
				FlowPercent[i] = 0;
			}
		}
		else
		{
			float waterDepth = WaterDepth[nIndex];
			if (waterDepth > 0)
			{
				float outgoing = OutgoingFlow[nIndex];
				if (outgoing > 0)
				{
					Flow[i] = flow * math.min(1, waterDepth / outgoing);
					FlowPercent[i] = Flow[i] / waterDepth;
				}
			}
			else
			{
				Flow[i] = 0;
				FlowPercent[i] = 0;
			}
		}
	}
}


[BurstCompile]
public struct ApplyFlowWaterJob : IJobParallelFor
{

	public NativeSlice<float> Delta;
	[ReadOnly] public NativeSlice<float> Depth;
	[ReadOnly] public NativeArray<float3> Positions;
	[ReadOnly] public NativeArray<int> Neighbors;
	[ReadOnly] public NativeArray<int> ReverseNeighbors;
	[ReadOnly] public NativeArray<float> FlowPercent;
	[ReadOnly] public NativeArray<float> CoriolisMultiplier;
	[ReadOnly] public float CoriolisTerm;
	[ReadOnly] public float SecondsPerTick;

	public void Execute(int i)
	{
		float mass = 0;
		float massPercentRemaining = 1;

#if !DISABLE_SURFACE_FLOW

		for (int j = 0; j < StaticState.MaxNeighbors; j++)
		{
			int n = i * StaticState.MaxNeighbors + j;
			int nIndex = Neighbors[n];
			if (nIndex >= 0)
			{
				float flowPercent = -FlowPercent[n];
				if (flowPercent < 0)
				{
					massPercentRemaining += flowPercent;
				}
				else
				{
					int incomingNIndex = ReverseNeighbors[n];
					if (Neighbors[incomingNIndex] == i)
					{
						float massIncoming = Depth[nIndex] * flowPercent;
						mass += massIncoming;


					}
				}
			}
		}

		massPercentRemaining = math.max(0, massPercentRemaining);
		float massRemaining = Depth[i] * massPercentRemaining;
		mass += massRemaining;

#endif

		Delta[i] = mass;
	}
}

[BurstCompile]
public struct ApplyWaterDeltaJob : IJobParallelFor
{

	public NativeSlice<float> Depth;
	[ReadOnly] public NativeSlice<float> Delta;

	public void Execute(int i)
	{
		Depth[i] = Delta[i];
	}
}
