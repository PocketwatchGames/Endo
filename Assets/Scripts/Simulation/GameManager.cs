using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using Unity.Jobs;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Active { get; private set; }

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
    private int _curSimStateIndex;
    private bool _initialized;
	private JobHandle _simJobHandle;

	public event Action<SimState> SimTickEvent;
	public event Action<SimState> NewGameEvent;

	private void Awake()
	{
        Active = this;

    }

	private void OnDestroy()
	{
		foreach (var i in _simStates)
		{
			i.Dispose();
		}
		StaticState.Dispose();
		Icosphere.Dispose();
	}

	public void NewGame()
    {

		WorldData = JsonUtility.FromJson<WorldData>(WorldDataAsset.text);
		WorldData.Init();

		_worldGenData = JsonUtility.FromJson<WorldGenData>(WorldGenAsset.text);


		Icosphere = Instantiate(IcospherePrefab, transform);
		Icosphere.Init(Subdivisions);

		StaticState = new StaticState();
		StaticState.Init(_worldGenData.Radius, Icosphere, ref WorldData);

		int height = 3;
		for (int i = 0; i < _simStates.Length; i++)
		{
			_simStates[i] = new SimState();
			_simStates[i].Init(StaticState.Count, height);
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


	void Tick()
	{
        int nextState = (_curSimStateIndex + 1) % _simStates.Length;
		_simJobHandle = Simulation.Tick(_simStates[_curSimStateIndex], _simStates[nextState], _simJobHandle);
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
