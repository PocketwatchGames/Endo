using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;

public static class Simulation
{
	public static JobHandle Tick(SimState lastState, SimState nextState, JobHandle dependency)
	{
		nextState.CarbonDioxideMass.CopyFrom(lastState.CarbonDioxideMass);
		nextState.CloudMass.CopyFrom(lastState.CloudMass);
		nextState.Current.CopyFrom(lastState.Current);
		nextState.IceMass.CopyFrom(lastState.IceMass);
		nextState.LandMass.CopyFrom(lastState.LandMass);
		nextState.MineralMass.CopyFrom(lastState.MineralMass);
		nextState.NitrogenMass.CopyFrom(lastState.NitrogenMass);
		nextState.OrganicMass.CopyFrom(lastState.OrganicMass);
		nextState.OxygenMass.CopyFrom(lastState.OxygenMass);
		nextState.SaltMass.CopyFrom(lastState.SaltMass);
		nextState.Temperature.CopyFrom(lastState.Temperature);
		nextState.VaporMass.CopyFrom(lastState.VaporMass);
		nextState.WaterMass.CopyFrom(lastState.WaterMass);
		nextState.Planet = lastState.Planet;
		return dependency;
	}
}
