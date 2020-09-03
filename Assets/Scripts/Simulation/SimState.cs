using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;
using UnityEditorInternal;

public class SimState
{
	public PlanetState Planet;
	public NativeArray<float> Temperature;
	public NativeArray<float> LandMass;
	public NativeArray<float> VaporMass;
	public NativeArray<float> CloudMass;
	public NativeArray<float> IceMass;
	public NativeArray<float> WaterMass;
	public NativeArray<float> SaltMass;
	public NativeArray<float> MineralMass;
	public NativeArray<float> CarbonDioxideMass;
	public NativeArray<float> OxygenMass;
	public NativeArray<float> NitrogenMass;
	public NativeArray<float> OrganicMass;
	public NativeArray<float> Dirt;
	public NativeArray<float> Sand;
	public NativeArray<float> Vegetation;
	public NativeArray<float3> Current;

	public NativeArray<float> Elevation;
	public NativeArray<float> WaterDepth;


	private bool _initialized;

	public void Init(int columns, int height)
	{		
		Temperature = new NativeArray<float>(columns * height, Allocator.Persistent);
		LandMass = new NativeArray<float>(columns * height, Allocator.Persistent);
		VaporMass = new NativeArray<float>(columns * height, Allocator.Persistent);
		CloudMass = new NativeArray<float>(columns * height, Allocator.Persistent);
		IceMass = new NativeArray<float>(columns * height, Allocator.Persistent);
		WaterMass = new NativeArray<float>(columns * height, Allocator.Persistent);
		SaltMass = new NativeArray<float>(columns * height, Allocator.Persistent);
		MineralMass = new NativeArray<float>(columns * height, Allocator.Persistent);
		CarbonDioxideMass = new NativeArray<float>(columns * height, Allocator.Persistent);
		OxygenMass = new NativeArray<float>(columns * height, Allocator.Persistent);
		NitrogenMass = new NativeArray<float>(columns * height, Allocator.Persistent);
		OrganicMass = new NativeArray<float>(columns * height, Allocator.Persistent);
		Dirt = new NativeArray<float>(columns * height, Allocator.Persistent);
		Sand = new NativeArray<float>(columns * height, Allocator.Persistent);
		Vegetation = new NativeArray<float>(columns * height, Allocator.Persistent);
		Current = new NativeArray<float3>(columns * height, Allocator.Persistent);

		Elevation = new NativeArray<float>(columns, Allocator.Persistent);
		WaterDepth = new NativeArray<float>(columns, Allocator.Persistent);

		_initialized = true;
	}

	public void Dispose()
	{
		if (!_initialized)
		{
			return;
		}
		_initialized = false;
		Temperature.Dispose();
		LandMass.Dispose();
		VaporMass.Dispose();
		CloudMass.Dispose();
		IceMass.Dispose();
		WaterMass.Dispose();
		SaltMass.Dispose();
		MineralMass.Dispose();
		CarbonDioxideMass.Dispose();
		OxygenMass.Dispose();
		NitrogenMass.Dispose();
		OrganicMass.Dispose();
		Dirt.Dispose();
		Sand.Dispose();
		Vegetation.Dispose();
		Current.Dispose();

		Elevation.Dispose();
		WaterDepth.Dispose();
	}

	public void CopyFrom(SimState from)
	{
		Planet = from.Planet;

		Temperature.CopyFrom(from.Temperature);
		LandMass.CopyFrom(from.LandMass);
		VaporMass.CopyFrom(from.VaporMass);
		CloudMass.CopyFrom(from.CloudMass);
		IceMass.CopyFrom(from.IceMass);
		WaterMass.CopyFrom(from.WaterMass);
		SaltMass.CopyFrom(from.SaltMass);
		MineralMass.CopyFrom(from.MineralMass);
		CarbonDioxideMass.CopyFrom(from.CarbonDioxideMass);
		OxygenMass.CopyFrom(from.OxygenMass);
		NitrogenMass.CopyFrom(from.NitrogenMass);
		OrganicMass.CopyFrom(from.OrganicMass);
		Dirt.CopyFrom(from.Dirt);
		Sand.CopyFrom(from.Sand);
		Vegetation.CopyFrom(from.Vegetation);
		Current.CopyFrom(from.Current);

		Elevation.CopyFrom(from.Elevation);
		WaterDepth.CopyFrom(from.WaterDepth);
	}


}
