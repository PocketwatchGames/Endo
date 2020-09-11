using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Endo
{
	[Serializable]
	public struct SimSettings
	{
		[Header("Debug")]
		public bool CheckForDegeneracy;
		public bool LogState;
		public int LogStateIndex;
		public SynchronousOverride SynchronousOverrides;

		[Header("Sim")]
		public bool MakeAirIncompressible;
		public bool MakeWaterIncompressible;
		public bool WaterSurfaceFlowEnabled;
		public bool RebalanceWaterLayers;
		public bool AdvectionAir;
		public bool DiffusionAir;
		public bool AdvectionWater;
		public bool DiffusionWater;
		public bool AdvectionCloud;
		public bool DiffusionCloud;
		public bool Condensation;
		public bool Evaporation;
		public bool Freezing;
		public bool Plankton;
		public bool Precipitation;
		public bool Flora;
		public bool IceMelting;
		public bool SoilRespiration;
		public bool GroundWater;
		public bool AirWaterCarbonDioxideDiffusion;
		public bool ConductionAirIce;
		public bool ConductionAirWater;
		public bool ConductionAirTerrain;
		public bool ConductionIceWater;
		public bool ConductionIceTerrain;
		public bool ConductionWaterTerrain;
		public int IncompressibilityIterations;

		[HideInInspector] public bool CollectGlobalsDebug;
		[HideInInspector] public bool CollectOverlay;

		[Serializable]
		public struct SynchronousOverride
		{
			public bool ThermalRadiation;
			public bool Albedo;
			public bool AirAbsorptivity;
			public bool SolarRadiationAbsorbed;
			public bool ThermalRadiationAbsorbed;
			public bool Conduction;
			public bool Energy;
			public bool FluxDust;
			public bool FluxFreeze;
			public bool FluxIceMelt;
			public bool FluxCondensation;
			public bool FluxEvaporation;
			public bool FluxPlankton;
			public bool FluxCloud;
			public bool FluxFlora;
			public bool FluxLava;
			public bool FluxTerrain;
			public bool UpdateMassCloud;
			public bool UpdateMassAir;
			public bool AdvectionCloud;
			public bool AdvectionAir;
			public bool AdvectionWater;
			public bool DiffusionCloud;
			public bool DiffusionAir;
			public bool DiffusionWater;
			public bool TempState;
			public bool Tools;
		}

	}


}