using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using Unity.Jobs;
using UnityEngine;

namespace Endo
{
	public class SimComponent : MonoBehaviour
	{
		public int Subdivisions;
		public TextAsset WorldGenAsset;
		public TextAsset WorldDataAsset;
		public Icosphere IcospherePrefab;

		[Header("Simulation Features")]
		public WorldData WorldData;
		public SimSettings SimSettings = new SimSettings()
		{
			MakeAirIncompressible = true,
			MakeWaterIncompressible = true,
			WaterSurfaceFlowEnabled = true,
			RebalanceWaterLayers = true,
			AdvectionAir = true,
			DiffusionAir = true,
			AdvectionWater = true,
			DiffusionWater = true,
			AdvectionCloud = true,
			DiffusionCloud = true,

			Condensation = true,
			Evaporation = true,
			Flora = true,
			Freezing = true,
			IceMelting = true,
			Plankton = true,
			Precipitation = true,
			SoilRespiration = true,
			GroundWater = true,
			AirWaterCarbonDioxideDiffusion = true,

			ConductionAirIce = true,
			ConductionAirTerrain = true,
			ConductionAirWater = true,
			ConductionIceTerrain = true,
			ConductionIceWater = true,
			ConductionWaterTerrain = true,

			IncompressibilityIterations = 20,

			CheckForDegeneracy = false,
			CollectGlobalsDebug = false,
			CollectOverlay = false,
			LogState = false,
			LogStateIndex = 0,

		};

		[HideInInspector] public StaticState StaticState;
		[HideInInspector] public Icosphere Icosphere;


		private WorldGenData _worldGenData = new WorldGenData();
		private SimState[] _simStates = new SimState[2];
		private TempState _tempState;
		private int _curSimStateIndex;
		private bool _initialized;
		private JobHandle _simJobHandle;
		private SimTick _simulation;

		public event Action<SimState> SimTickEvent;
		public event Action<SimState> NewGameEvent;

		private void OnDestroy()
		{
			foreach (var i in _simStates)
			{
				i.Dispose();
			}
			StaticState.Dispose();
			Icosphere.Dispose();
			_tempState.Dispose();
		}

		public void NewGame()
		{

			WorldData = JsonUtility.FromJson<WorldData>(WorldDataAsset.text);
			WorldData.Init();

			_worldGenData = JsonUtility.FromJson<WorldGenData>(WorldGenAsset.text);

			Icosphere = Instantiate(IcospherePrefab, transform);
			Icosphere.Init(Subdivisions);

			StaticState = new StaticState();
			StaticState.Init(_worldGenData.Radius, Icosphere, WorldData);

			_simulation = new SimTick(StaticState);

			_tempState = new TempState(StaticState);


			int height = 3;
			for (int i = 0; i < _simStates.Length; i++)
			{
				_simStates[i] = new SimState();
				_simStates[i].Init(StaticState);
			}

			WorldGen.Generate(StaticState.Count, height, _worldGenData, _simStates[_curSimStateIndex], StaticState);
			_initialized = true;

			NewGameEvent?.Invoke(_simStates[_curSimStateIndex]);
		}

		// Update is called once per frame
		void Update()
		{
			if (!_initialized)
			{
				NewGame();
			}
		}

		public SimState GetActiveState()
		{
			_simJobHandle.Complete();
			return _simStates[_curSimStateIndex];
		}

		private void FixedUpdate()
		{
			if (_initialized)
			{
				Tick();
			}
		}

		void Tick()
		{
			int nextState = (_curSimStateIndex + 1) % _simStates.Length;
			_simJobHandle = _simulation.Tick(_simStates[_curSimStateIndex], _simStates[nextState], StaticState, _tempState, WorldData, _simJobHandle);
			_simJobHandle.Complete();
			_curSimStateIndex = nextState;
			SimTickEvent?.Invoke(_simStates[nextState]);
		}

		public delegate void EditSimStateFunc(SimState lastState, SimState nextState);
		public void Edit(EditSimStateFunc editSimState)
		{
			_simJobHandle.Complete();

			int nextStateIndex = (_curSimStateIndex + 1) % _simStates.Length;
			var lastState = _simStates[_curSimStateIndex];
			var nextState = _simStates[nextStateIndex];
			_curSimStateIndex = nextStateIndex;

			editSimState(lastState, nextState);

			SimTickEvent?.Invoke(nextState);
		}


	}
}