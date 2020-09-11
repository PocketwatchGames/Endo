using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;

namespace Endo
{
	public class SimTick
	{

		JobHelper _columnJobHelper;
		JobHelper _neighborJobHelper;
		JobHelper _animalJobHelper;

		public SimTick(StaticState staticState)
		{
			_columnJobHelper = new JobHelper(staticState.Count);
			_neighborJobHelper = new JobHelper(staticState.Count * StaticState.MaxNeighbors);
			_animalJobHelper = new JobHelper(staticState.AnimalCount);
		}
		public JobHandle Tick(SimState lastState, SimState nextState, StaticState staticState, TempState tempState, WorldData worldData, JobHandle dependency)
		{
			float coriolisTerm = 2 * lastState.Planet.SpinSpeed;

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

			nextState.WaterDepth.CopyFrom(lastState.WaterDepth);
			nextState.Flow.CopyFrom(lastState.Flow);
			nextState.Vegetation.CopyFrom(lastState.Vegetation);
			nextState.Sand.CopyFrom(lastState.Sand);
			nextState.Dirt.CopyFrom(lastState.Dirt);
			nextState.OrganicMass.CopyFrom(lastState.OrganicMass);
			nextState.Elevation.CopyFrom(lastState.Elevation);
			nextState.Explored.CopyFrom(lastState.Explored);

			nextState.AnimalSpecies.CopyFrom(lastState.AnimalSpecies);
			nextState.AnimalPosition.CopyFrom(lastState.AnimalPosition);

			nextState.Planet = lastState.Planet;



			bool sync = false;
			dependency = _columnJobHelper.Schedule(sync, 1, dependency,
				new UpdateSurfaceElevationJob()
				{
					SurfaceElevation = tempState.SurfaceElevation,
					WaterDepth = nextState.WaterDepth,
					Elevation = nextState.Elevation
				});


			dependency = _neighborJobHelper.Schedule(sync, 1, dependency,
				new UpdateFlowVelocityJob()
				{
					Flow = nextState.Flow,
					LastFlow = lastState.Flow,
					SurfaceElevation = tempState.SurfaceElevation,
					WaterDepth = nextState.WaterDepth,
					NeighborDistInverse = staticState.NeighborDistInverse,
					Neighbors = staticState.Neighbors,
					SecondsPerTick = worldData.SecondsPerTick,
					Gravity = nextState.Planet.Gravity,
					Damping = worldData.SurfaceWaterFlowDamping,
					ViscosityInverse = 1.0f - worldData.WaterViscosity
				});
			dependency = _columnJobHelper.Schedule(sync, 1, dependency,
				new SumOutgoingFlowJob()
				{
					OutgoingFlow = tempState.OutgoingFlow,
					Flow = nextState.Flow,
				});
			dependency = _neighborJobHelper.Schedule(sync, 1, dependency,
				new LimitOutgoingFlowJob()
				{
					Flow = nextState.Flow,
					FlowPercent = tempState.FlowPercent,
					OutgoingFlow = tempState.OutgoingFlow,
					WaterDepth = nextState.WaterDepth,
					Neighbors = staticState.Neighbors,
				});
			dependency = _columnJobHelper.Schedule(sync, 1, dependency,
				new ApplyFlowWaterJob()
				{
					Delta = tempState.WaterDelta,
					Depth = nextState.WaterDepth,
					Positions = staticState.SphericalPosition,
					Neighbors = staticState.Neighbors,
					ReverseNeighbors = staticState.ReverseNeighbors,
					FlowPercent = tempState.FlowPercent,
					CoriolisMultiplier = staticState.CoriolisMultiplier,
					CoriolisTerm = coriolisTerm,
					SecondsPerTick = worldData.SecondsPerTick

				});
			dependency = _columnJobHelper.Schedule(sync, 1, dependency,
				new ApplyWaterDeltaJob()
				{
					Depth = nextState.WaterDepth,
					Delta = tempState.WaterDelta,
				});

			dependency = _animalJobHelper.Schedule(sync, dependency, new UpdateExplorationJob()
			{
				Exploration = nextState.Explored,
				AnimalSpecies = nextState.AnimalSpecies,
				AnimalPositions = nextState.AnimalPosition,
			});

			return dependency;
		}
	}
}