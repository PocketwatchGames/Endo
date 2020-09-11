using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Endo
{
	[Serializable]
	public class WorldData
	{

		public const int TerrainLayers = 5;

		public float SecondsPerTick;
		public int AirLayers;
		public int WaterLayers;
		public float TropopauseElevation;
		public float BoundaryZoneElevation;

		[Header("Solar Energy")]
		// atmospheric heat balance https://energyeducation.ca/encyclopedia/Earth%27s_heat_balance
		// https://en.wikipedia.org/wiki/Earth%27s_energy_budget
		// https://en.wikipedia.org/wiki/Electromagnetic_absorption_by_water
		// Water vapor is responsible for 70% of solar absorption and about 60% of absorption of thermal radiation.
		public float SolarAbsorptivityAir; // total absorbed by atmosphere AFTER reflection about 30%
		public float SolarAbsorptivityWaterVapor; // total absorbed by atmosphere AFTER reflection about 30%
		public float SolarAbsorptivityDust; // total absorbed by atmosphere AFTER reflection about 30%
		public float SolarAbsorptivityCloud; // 6% absorbed by clouds

		[Header("Albedo")]
		// For values of different surfaces:
		// https://en.wikipedia.org/wiki/Albedo
		public float AlbedoAir; // 7% is reflected due to atmospheric scattering 
		public float AlbedoWaterVapor;
		public float AlbedoDust;
		public float AlbedoReductionGroundWaterSaturation;
		public float AlbedoWaterRange; // how much is reflected due to slope
		public float AlbedoSlopePower;
		public float AlbedoWaterMin;
		public float AlbedoIceMin;
		public float AlbedoIceRange;
		public float AlbedoSandMin;
		public float AlbedoSandRange;
		public float AlbedoSoilMin;
		public float AlbedoSoilRange;
		public float AlbedoFloraMin;
		public float AlbedoFloraRange;
		//public const float AlbedoCloud = 0.05f; // 24% incoming  reflected back to space by clouds (avg, globally)
		public float minCloudFreezingTemperature;
		public float maxCloudFreezingTemperature;
		public float rainDropSizeAlbedoMin;
		public float rainDropSizeAlbedoMax;

		[Header("Thermal Energy")]
		//public float EvaporativeHeatLoss = 0.6f; // global average = 78 watts
		// Net Back Radiation: The ocean transmits electromagnetic radiation into the atmosphere in proportion to the fourth power of the sea surface temperature(black-body radiation)
		// https://eesc.columbia.edu/courses/ees/climate/lectures/o_atm.html


		// TODO: should this be a constant or should co2 and water vapor be absorbing the window radiation?
		public float EnergyLostThroughAtmosphereWindow; // AKA Atmospheric window global average = 40 watts = 6.7% of all surface and atmospheric radiation

		// https://en.wikipedia.org/wiki/Electromagnetic_absorption_by_water
		// Water vapor is responsible for 70% of solar absorption and about 60% of absorption of thermal radiation.
		// carbon dioxide accounts for just 26% of the greenhouse effect.
		// The total absorptivity of carbon dioxide at its current concentration in the atmosphere is 0.0017. Therefore, for an air temperature of 308 K (35 °C), carbon dioxide contributes with 13.5 K

		// emissivity values obtained here: https://www.thermoworks.com/emissivity-table
		// and here https://www.aspen-electronics.com/uploads/3/7/1/2/37123419/emissivity-table.pdf
		public float ThermalEmissivityWater;
		public float ThermalEmissivitySalt;
		public float ThermalEmissivityIce;
		public float ThermalEmissivityAir;
		public float ThermalEmissivityOxygen;
		public float ThermalEmissivityCarbonDioxide;
		public float ThermalEmissivityWaterVapor;
		public float ThermalEmissivityDust;
		public float ThermalEmissivityDirt;
		public float ThermalEmissivitySand;
		public float ThermalEmissivityFlora;
		public float ThermalEmissivityLava;

		// TODO: should we parameterize the micro-conduction that allows for water to heat the air faster than it can cool it?
		//public float AirWaterConductionPositive; // global avg = 16 watts per degree delta between air and ocean (global avg = 24 watts per m^2 of ocean)

		[Header("Evaporation")] // evaporation on earth maxes out around 2.5M per year
		public float WaterHeatingDepth;
		public float WaterAirCarbonDiffusionCoefficient; // standard carbon/air: 407.4 ppm, carbon/water: 90 ppm
		public float WaterAirCarbonDiffusionDepth;
		public float EvaporationLatentHeatFromAir;

		[Header("Ice")]
		public float IceHeatingDepth;
		public float FreezePointReductionPerSalinity;

		[Header("Rain and Clouds")]
		public float rainDropDragCoefficient;
		public float rainDropMaxSize;
		public float rainDropMinSize;
		public float RainDropGrowthRate;
		public float CloudDissapationRateWind;
		public float CloudDissapationRateDryAir;

		[Header("Diffusion")]
		public float CloudDiffusionCoefficient;
		public float AirDiffusionCoefficientHorizontal;
		public float AirDiffusionCoefficientVertical;
		public float WaterDiffusionCoefficientHorizontal;
		public float WaterDiffusionCoefficientVertical;

		[Header("Wind")]
		public float WindWaterFriction;
		public float WindIceFriction;
		public float WindTerrainFrictionMin;
		public float WindTerrainFrictionMax;
		public float WindFloraFriction;
		public float MaxTerrainRoughnessForWindFriction;
		public float WaterSurfaceFrictionDepth;

		[Header("Water Current")]
		public float WindToWaterCurrentFrictionCoefficient;
		public float WaterDensityPerSalinity;
		public float WaterDensityPerDegree;
		public float WaterDensityCurrentSpeed;

		[Header("Terrain and Ground Water")]
		public float SoilHeatDepth;
		public float SoilRespirationSpeed;
		public float GroundCarbonFertility;
		public float GroundWaterFlowSpeed;
		public float GroundWaterMax;
		public float GroundWaterMaxDepth;
		public float GroundWaterAbsorptionRate;
		public float GroundWaterDiffusionCoefficient;
		public float FullCoverageIce;
		public float FullCoverageWater;

		[Header("Flora")]
		public float FloraGrowthRate;
		public float FloraDeathRate;
		public float FloraWaterConsumptionRate;
		public float FloraGrowthTemperatureRangeInverse;
		public float FloraAirSurfaceArea;
		public float FloraEnergyForPhotosynthesis;
		public float FloraCarbonDioxideExtractionEfficiency;
		public float FloraOxygenExtractionEfficiency;
		public float FloraPhotosynthesisSpeed;
		public float FloraRespirationSpeed;
		public float FloraRespirationPerDegree;

		[Header("Plankton")]
		public float PlanktonDensityMax;
		public float PlanktonEnergyForPhotosynthesis;
		public float PlanktonCarbonDioxideExtractionEfficiency;
		public float PlanktonPhotosynthesisSpeed;
		public float PlanktonRespirationSpeed;
		public float PlanktonRespirationPerDegree;
		public float PlanktonGrowthRate;
		public float PlanktonDeathRate;

		[Header("Lava")]
		public float LavaCrystalizationTemperature;
		public float MagmaTemperature;
		public float CrustDepthForEruption;
		public float DustVerticalVelocity;
		public float DustPerLavaEjected;
		public float MagmaPressureCrustReductionSpeed;
		public float LavaToRockMassAdjustment;
		public float LavaEruptionSpeed;
		public float LavaFlowDamping;
		public float LavaViscosity;

		[Header("SurfaceWaterFlow")]
		public float SurfaceWaterFlowDamping;
		public float SurfaceWaterDepth;
		public float ThermoclineDepth;
		public float WaterViscosity;

		#region Constants
		public const float TemperatureLapseRate = -0.0065f;
		public const float TemperatureLapseRateInverse = 1.0f / TemperatureLapseRate;
		public const float AdiabaticLapseRate = 0.0098f;
		public const float StandardPressure = 101325;
		public const float StandardTemperature = 288.15f;
		public const float MolarMassAir = 0.0289647f;
		public const float MolarMassWater = 0.01802f;
		public const float MolarMassAirInverse = 1.0f / MolarMassAir;
		public const float UniversalGasConstant = 8.3144598f;
		public const float FreezingTemperature = 273.15f;
		public const float StefanBoltzmannConstant = 0.00000005670373f;
		// specific heat is joules to raise one degree (kJ/kgK)
		public const float SpecificHeatIce = 2.108f;
		public const float SpecificHeatFlora = 1.76f;
		public const float SpecificHeatWater = 4.187f;
		public const float SpecificHeatWaterVapor = 1.996f;
		public const float SpecificHeatSalt = 0.85f;
		public const float SpecificHeatAtmosphere = 1.158f;
		public const float SpecificHeatSoil = 0.80f;
		public const float SpecificHeatSand = 0.84f;
		public const float SpecificHeatLava = 0.84f;
		public const float LatentHeatWaterLiquid = 334.0f;
		public const float LatentHeatWaterVapor = 2264.705f;
		public const float LatentHeatLava = 400000f;
		public const float MassEarthAir = 1.29f;
		public const float MassCarbonDioxide = 44.01f;
		public const float MassWater = 1000f;
		public const float MassSalt = 2170f;
		public const float MassIce = 919f;
		public const float MassSoil = 1200f;
		public const float MassSand = 1600f;
		public const float MassLava = 3100f;
		public const float DensityWater = 997f;
		public const float DensityAir = 1.21f;
		public const float ConductivityAir = 0.0262f;
		public const float ConductivityWater = 0.606f;
		public const float ConductivityIce = 2.18f;
		public const float ConductivityTerrain = 0.2f;
		public const float ConductivityFlora = 0.25f;
		public const float ConductivityLava = 1.45f;
		public const float ThermalContactResistance = 0.00005f;
		// TODO: make custom conductivity for terrain based on soil/flora properties
		public const float ConductivityAirWater = 1.0f / (1.0f / ConductivityAir + 1.0f / ConductivityWater + ThermalContactResistance);
		public const float ConductivityAirIce = 1.0f / (1.0f / ConductivityAir + 1.0f / ConductivityIce + ThermalContactResistance);
		public const float ConductivityAirLava = 1.0f / (1.0f / ConductivityAir + 1.0f / ConductivityLava + ThermalContactResistance);
		public const float ConductivityAirTerrain = 1.0f / (1.0f / ConductivityAir + 1.0f / ConductivityTerrain + ThermalContactResistance);
		public const float ConductivityIceWater = 1.0f / (1.0f / ConductivityWater + 1.0f / ConductivityIce + ThermalContactResistance);
		public const float ConductivityIceLava = 1.0f / (1.0f / ConductivityLava + 1.0f / ConductivityIce + ThermalContactResistance);
		public const float ConductivityIceTerrain = 1.0f / (1.0f / ConductivityTerrain + 1.0f / ConductivityIce + ThermalContactResistance);
		public const float ConductivityWaterLava = 1.0f / (1.0f / ConductivityLava + 1.0f / ConductivityWater + ThermalContactResistance);
		public const float ConductivityWaterTerrain = 1.0f / (1.0f / ConductivityTerrain + 1.0f / ConductivityWater + ThermalContactResistance);
		public const float GasConstantAir = UniversalGasConstant / MolarMassAir * 1000;
		public const float GasConstantWaterVapor = UniversalGasConstant / MolarMassWater * 1000;
		public const float PressureExponent = 1.0f / (UniversalGasConstant * TemperatureLapseRate);
		public const float DryAirAdiabaticLapseRate = AdiabaticLapseRate / SpecificHeatAtmosphere;
		public const float inverseSpecificHeatIce = 1.0f / SpecificHeatIce;
		public const float InverseDensityAir = 1.0f / DensityAir;
		#endregion

		#region Nonserialized
		[NonSerialized] public float TicksPerSecond;
		[NonSerialized] public float TicksPerYear;
		[NonSerialized] public float inverseFullCoverageFlora;
		[NonSerialized] public float inverseFullCoverageWater;
		[NonSerialized] public float inverseFullCoverageIce;
		[NonSerialized] public float wattsToKJPerTick;
		[NonSerialized] public float declinationOfSun;
		[NonSerialized] public float sunHitsAtmosphereBelowHorizonAmount;
		[NonSerialized] public float inverseSunAtmosphereAmount;

		[NonSerialized] public int LayerCount;
		[NonSerialized] public int TerrainLayer;
		[NonSerialized] public int CloudLayer;
		[NonSerialized] public int WaterLayer0;
		[NonSerialized] public int FloraLayer;
		[NonSerialized] public int LavaLayer;
		[NonSerialized] public int IceLayer;
		[NonSerialized] public int AirLayer0;
		[NonSerialized] public int BottomWaterLayer;
		[NonSerialized] public int SurfaceWaterLayer;
		[NonSerialized] public int SurfaceWaterLayerGlobal;
		[NonSerialized] public int SurfaceAirLayer;
		[NonSerialized] public int SurfaceAirLayerGlobal;


		#endregion

		public void Init()
		{

			TicksPerSecond = 1.0f / SecondsPerTick;
			TicksPerYear = 60 * 60 * 24 * 365 / SecondsPerTick;

			inverseFullCoverageWater = 1.0f / FullCoverageWater;
			inverseFullCoverageIce = 1.0f / FullCoverageIce;
			wattsToKJPerTick = SecondsPerTick * 1000;
			sunHitsAtmosphereBelowHorizonAmount = 0.055f;
			inverseSunAtmosphereAmount = 1.0f / (1.0f + sunHitsAtmosphereBelowHorizonAmount);


			LayerCount = AirLayers + WaterLayers + TerrainLayers;
			TerrainLayer = 0;
			LavaLayer = TerrainLayer + 1;
			FloraLayer = LavaLayer + 1;
			WaterLayer0 = FloraLayer + 1;
			IceLayer = WaterLayer0 + WaterLayers;
			AirLayer0 = IceLayer + 1;
			BottomWaterLayer = 1;
			SurfaceWaterLayer = WaterLayers - 2;
			SurfaceWaterLayerGlobal = SurfaceWaterLayer + WaterLayer0;
			SurfaceAirLayer = 1;
			SurfaceAirLayerGlobal = AirLayer0 + SurfaceAirLayer;
			CloudLayer = AirLayer0 + AirLayers;

		}

	}
}