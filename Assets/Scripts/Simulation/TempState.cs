using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
public class TempState
{
	public NativeArray<float> SurfaceElevation;
	public NativeArray<float> OutgoingFlow;
	public NativeArray<float> FlowPercent;
	public NativeArray<float> WaterDelta;
	public TempState(StaticState staticState)
	{
		SurfaceElevation = new NativeArray<float>(staticState.Count, Allocator.Persistent);
		OutgoingFlow = new NativeArray<float>(staticState.Count, Allocator.Persistent);
		FlowPercent = new NativeArray<float>(staticState.Count * StaticState.MaxNeighbors, Allocator.Persistent);
		WaterDelta = new NativeArray<float>(staticState.Count, Allocator.Persistent);
	}

	public void Dispose()
	{
		SurfaceElevation.Dispose();
		OutgoingFlow.Dispose();
		FlowPercent.Dispose();
		WaterDelta.Dispose();
	}
}