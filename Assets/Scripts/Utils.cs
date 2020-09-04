using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using UnityEngine;

public static class Utils
{
	public static float Sqr(float x) { return x * x; }
	public static int Sqr(int x) { return x * x; }


	public static JobHandle MemsetArray<T>(int count, JobHandle dependency, NativeArray<T> array, T value) where T : struct
	{
		return new Unity.Entities.MemsetNativeArray<T>()
		{
			Source = array,
			Value = value
		}.Schedule(count, 128, dependency);
	}

	public static JobHandle MemCopy(NativeSlice<float> dest, NativeSlice<float> src, JobHandle dependencies)
	{
		return new MemCopyFloat()
		{
			Dest = dest,
			Src = src,

		}.Schedule(src.Length, 128, dependencies);
	}
	public static JobHandle MemCopy(NativeSlice<short> dest, NativeSlice<short> src, JobHandle dependencies)
	{
		return new MemCopyShort()
		{
			Dest = dest,
			Src = src,

		}.Schedule(src.Length, 128, dependencies);
	}


	public static float2 GetPolarCoordinates(float3 referencePos)
	{
		return math.float2(math.atan2(referencePos.z, referencePos.x), math.asin(referencePos.y));
	}

	public static float3 GetPolarCoordinates(float3 referencePos, float3 vector)
	{
		float up = math.dot(referencePos, vector);
		float3 tangent = vector - up * referencePos;
		if (referencePos.y < 1 && referencePos.y > -1)
		{
			var east = math.cross(referencePos, math.float3(0, 1, 0));
			var north = math.cross(east, referencePos);
			return math.float3(math.dot(tangent, east), math.dot(tangent, north), up);
		}
		else
		{
			return math.float3(0, math.length(tangent), up);
		}
	}

	public static float3 GetHorizontalComponent(float3 v, float3 up)
	{
		return v - math.dot(v, up) * up;
	}

	public static float3 GetVerticalComponent(float3 v, float3 up)
	{
		return math.dot(v, up) * up;
	}

	static public float RepeatExclusive(float x, float y)
	{
		while (x < 0)
		{
			x += y;
		}
		while (x >= y)
		{
			x -= y;
		}
		return x;
	}

	static public float WrapAngle(float x)
	{
		while (x < math.PI)
		{
			x += math.PI * 2;
		}
		while (x >= math.PI)
		{
			x -= math.PI * 2;
		}
		return x;
	}

	public static bool IsMouseOverGameWindow { get { return !(0 > Input.mousePosition.x || 0 > Input.mousePosition.y || Screen.width < Input.mousePosition.x || Screen.height < Input.mousePosition.y); } }

	public static bool RayTriangleIntersect(
		float3 orig, float3 dir,
		float3 v0, float3 v1, float3 v2,
		float t, float u, float v)
	{
		const float kEpsilon = 0.00000001f;

		// compute plane's normal
		float3 v0v1 = v1 - v0;
		float3 v0v2 = v2 - v0;
		// no need to normalize
		float3 N = math.cross(v0v1, v0v2); // N 
		float denom = math.dot(N, N);

		// Step 1: finding P

		// check if ray and plane are parallel ?
		float NdotRayDirection = math.dot(N, dir);
		if (math.abs(NdotRayDirection) < kEpsilon) // almost 0 
			return false; // they are parallel so they don't intersect ! 

		// compute d parameter using equation 2
		float d = math.dot(N, v0);

		// compute t (equation 3)
		t = (math.dot(N, orig) + d) / NdotRayDirection;
		// check if the triangle is in behind the ray
		if (t < 0) return false; // the triangle is behind 

		// compute the intersection point using equation 1
		float3 P = orig + t * dir;

		// Step 2: inside-outside test
		float3 C; // vector perpendicular to triangle's plane 

		// edge 0
		float3 edge0 = v1 - v0;
		float3 vp0 = P - v0;
		C = math.cross(edge0, vp0);
		if (math.dot(N, C) < 0) return false; // P is on the right side 

		// edge 1
		float3 edge1 = v2 - v1;
		float3 vp1 = P - v1;
		C = math.cross(edge1, vp1);
		if ((u = math.dot(N, C)) < 0) return false; // P is on the right side 

		// edge 2
		float3 edge2 = v0 - v2;
		float3 vp2 = P - v2;
		C = math.cross(edge2, vp2);
		if ((v = math.dot(N, C)) < 0) return false; // P is on the right side; 

		u /= denom;
		v /= denom;

		return true; // this ray hits the triangle 
	}

	public static bool GetBarycentricIntersection(
		float3 p, float3 a, float3 b, float3 c,
		out float u, out float v, out float w)
	{
		float3 v0 = b - a, v1 = c - a, v2 = p - a;
		float d00 = math.dot(v0, v0);
		float d01 = math.dot(v0, v1);
		float d11 = math.dot(v1, v1);
		float d20 = math.dot(v2, v0);
		float d21 = math.dot(v2, v1);
		float inverseDenom = 1.0f / (d00 * d11 - d01 * d01);
		v = (d11 * d20 - d01 * d21) * inverseDenom;
		w = (d00 * d21 - d01 * d20) * inverseDenom;
		u = 1.0f - v - w;
		return v >= 0 && v <= 1 && w >= 0 && w <= 1 && u >= 0 && u <= 1;
	}
}

public struct Optional<T>
{
	public bool hasValue { get; private set; }
	public T value { get; private set; }

	public Optional(T t)
	{
		value = t;
		hasValue = true;
	}
	public void Set(T t)
	{
		value = t;
		hasValue = true;
	}
	public void Clear()
	{
		hasValue = false;
	}
}

public enum JobType
{
	Schedule,
	Run
}
public class JobHelper
{

	private int _cellCount;

	public JobHelper(int cellCount)
	{
		_cellCount = cellCount;
	}


	public JobHandle Schedule<T>(bool runSynchronously, int batchCount, JobHandle dependencies, T job) where T : struct, IJobParallelFor
	{
		if (!runSynchronously)
		{
			return job.Schedule(_cellCount, batchCount, dependencies);
		}
		dependencies.Complete();
		job.Run(_cellCount);
		return default(JobHandle);
	}
	public JobHandle Schedule<T>(JobType jobType, int batchCount, JobHandle dependencies, T job) where T : struct, IJobParallelFor
	{
		return Schedule(jobType == JobType.Run, batchCount, dependencies, job);
	}
	public JobHandle ScheduleOrMemset<S, T>(bool runSynchronously, int batchCount, bool schedule, NativeArray<S> arrayToSet, S setVal, JobHandle dependencies, T job) where T : struct, IJobParallelFor where S : struct
	{
		// TODO: we need a version of this for slices
		if (schedule)
		{
			return Schedule(runSynchronously, batchCount, dependencies, job);
		}
		else
		{
			return Utils.MemsetArray<S>(_cellCount, dependencies, arrayToSet, setVal);
		}
	}
}

[BurstCompile]
public struct MemCopyFloat : IJobParallelFor
{
	public NativeSlice<float> Dest;
	[ReadOnly] public NativeSlice<float> Src;
	public void Execute(int i)
	{
		Dest[i] = Src[i];
	}

}
[BurstCompile]
public struct MemCopyShort : IJobParallelFor
{
	public NativeSlice<short> Dest;
	[ReadOnly] public NativeSlice<short> Src;
	public void Execute(int i)
	{
		Dest[i] = Src[i];
	}

}

