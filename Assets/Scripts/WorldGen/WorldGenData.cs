using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Endo
{
	[Serializable]
	public class WorldGenData
	{
		public float Radius;
		public float DistanceToSun;
		public float MinElevation;
		public float MaxElevation;
		public float MaxRoughness;
		public float GroundWaterDepthMin;
		public float GroundWaterDepthMax;
		public float AtmosphereMass;
		public float TroposphereMass;
		public float MinTemperature;
		public float MaxTemperature;
		public float Gravity;
		public float Oxygen;
		public float AirCarbonPercent; // there are 2,500 billion tons of carbon in soil, compared with 800 billion tons in the atmosphere and 560 billion tons in plant and animal life
		public float AirOxygenPercent;
		public float AirNitrogenPercent;
		public float AirMethanePercent;
		public float WaterCarbonPercent;
		public float SolarRadiation; // extraterrestrial solar radiation // https://en.wikipedia.org/wiki/Sunlight (1367 w/m^2)
		public float TiltAngle;
		public float SpinAngle;
		public float SpinTime;
		public float OrbitTime;
		// http://zebu.uoregon.edu/1996/ph162/l18.html
		// Averaged over the earths surface, the heat energy flow is 0.06 Watts per square meter
		public float GeothermalHeat;
		public float MaxSalinity;
		public float MinSalinity;
		public int NumPlates;
		public float WaterTemperatureDepthFalloff;
		public float MagmaMin;
		public float MagmaMax;
		public float CrustMin;
		public float CrustMax;
	}
}