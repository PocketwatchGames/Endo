using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.Mathematics;
using System;
using System.Linq;

public class EditHUD : MonoBehaviour
{
    public HUD HUD;
    public GameObject ToolOptionsPanel;

    public enum EditToolType
	{
        Select,
        Elevation,
        Erosion,
        Rainfall,
        Meteor,
        Planet
	}
    EditToolType _selectedTool;
    public EditTool[] Tools = new EditTool[6];

    public class CellSelectionOptions
	{
        public float BrushSize;
        public float Falloff;
        public float NoiseStrength;
        public float NoisePeriod;
        public float MaskTop;
        public float MaskBottom;
    }

    public class EditTool
	{
        public GameObject ToolOptionsPanel;
	}

    interface SelectionTool
    {
        CellSelectionOptions SelectionOptions { get; set; }
    }
    public class ElevationToolOptions : EditTool, SelectionTool
    {
        public CellSelectionOptions SelectionOptions { get; set; }
        public float TargetElevation;
        public float Strength;
        public float MinVal;
        public float MaxVal;

        public void CopyFrom(ElevationToolOptions other)
		{
            SelectionOptions.BrushSize = other.SelectionOptions.BrushSize;
            SelectionOptions.Falloff = other.SelectionOptions.Falloff;
            SelectionOptions.MaskBottom = other.SelectionOptions.MaskBottom;
            SelectionOptions.MaskTop = other.SelectionOptions.MaskTop;
            SelectionOptions.NoisePeriod = other.SelectionOptions.NoisePeriod;
            SelectionOptions.NoiseStrength = other.SelectionOptions.NoiseStrength;
            TargetElevation = other.TargetElevation;
            Strength = other.Strength;
            MinVal = other.MinVal;
            MaxVal = other.MaxVal;
		}
    }

    public class ErosionToolOptions : EditTool, SelectionTool
    {
        public CellSelectionOptions SelectionOptions { get; set; }
        public float Strength;
        public float MinVal;
        public float MaxVal;
    }

    public class RainfallToolOptions : EditTool, SelectionTool
    {
        public CellSelectionOptions SelectionOptions { get; set; }
        public float Strength;
    }
    public class MeteorToolOptions : EditTool, SelectionTool
    {
        public CellSelectionOptions SelectionOptions { get; set; }
        public float Strength;
    }
    public class PlanetToolOptions : EditTool
    {
    }

    void Awake()
    {

        var toolOptionsPanels = ToolOptionsPanel.GetComponentsInChildren<GameToolIdentifier>();
        Tools[(int)EditToolType.Elevation] = new ElevationToolOptions
        {
            ToolOptionsPanel = toolOptionsPanels.FirstOrDefault(i=>i.Tool == EditToolType.Elevation)?.gameObject,
            SelectionOptions = new CellSelectionOptions
            {
                BrushSize = 1,
                Falloff = 1,
                MaskTop = float.PositiveInfinity,
                MaskBottom = float.NegativeInfinity,
                NoiseStrength = 0,
                NoisePeriod = 10
            },
            TargetElevation = 0,
            Strength = 1000,
            MaxVal = float.PositiveInfinity,
            MinVal = float.NegativeInfinity,
        };
        Tools[(int)EditToolType.Erosion] = new ErosionToolOptions
        {
            ToolOptionsPanel = toolOptionsPanels.FirstOrDefault(i => i.Tool == EditToolType.Erosion)?.gameObject,
            SelectionOptions = new CellSelectionOptions
            {
                BrushSize = 1,
                Falloff = 1,
                MaskTop = float.PositiveInfinity,
                MaskBottom = float.NegativeInfinity,
            },
            Strength = 1000,
            MaxVal = float.PositiveInfinity,
            MinVal = float.NegativeInfinity,
        };
        Tools[(int)EditToolType.Rainfall] = new RainfallToolOptions
        {
            ToolOptionsPanel = toolOptionsPanels.FirstOrDefault(i => i.Tool == EditToolType.Rainfall)?.gameObject,
            SelectionOptions = new CellSelectionOptions
            {
                BrushSize = 1,
                Falloff = 1,
                MaskTop = float.PositiveInfinity,
                MaskBottom = 0,
            },
        };

    }

	private void Start()
	{
        OnToolClicked(null);
    }

    // Update is called once per frame
    void Update()
    {
        List<Tuple<int, float>> selectionCells = new List<Tuple<int, float>>();


        switch (_selectedTool)
        {
            case EditToolType.Elevation:
            {
                var tool = Tools[(int)_selectedTool] as ElevationToolOptions;
                if (tool != null && !EventSystem.current.IsPointerOverGameObject())
                {
                    var cell = HUD.GetMouseCellIndex();
                    if (cell.Item2 >= 0)
                    {
                        SelectCells(
                            cell.Item2,
                            tool.SelectionOptions,
                            GameManager.Active.GetActiveState(),
                            selectionCells);
                        if (Input.GetMouseButton(0))
                        {
                            GameManager.Active.Edit((last, next) =>
                            {
                                next.CopyFrom(last);
                                float strength = Time.deltaTime * tool.Strength;

                                for (int i = 0; i < selectionCells.Count; i++)
                                {
                                    int index = selectionCells[i].Item1;
                                    float diff = tool.TargetElevation - next.Elevation[index];
                                    float delta = math.min(strength, math.abs(diff));
                                    float direction = math.sign(diff) * delta;
                                    float elevation = math.clamp(next.Elevation[index] + direction * selectionCells[i].Item2, tool.MinVal, tool.MaxVal);
                                    next.Elevation[index] = elevation;
                                    next.WaterDepth[index] = math.max(0, -elevation);
                                    next.Sand[index] = (elevation < 1000) ? 1 : 0;
                                    next.Vegetation[index] = (elevation >= 1000 && elevation < 2500) ? 1 : 0;
                                    next.Dirt[index] = (elevation >= 2500 && elevation < 4000) ? 1 : 0;
                                    next.IceMass[index] = (elevation >= 4000) ? 1 : 0;
                                }
                            });
                        }
                    }
                }
                break;
            }
            case EditToolType.Erosion:
            {
                var tool = Tools[(int)_selectedTool] as ErosionToolOptions;
                if (tool != null && !EventSystem.current.IsPointerOverGameObject())
                {
                    var cell = HUD.GetMouseCellIndex();
                    if (cell.Item2 >= 0)
                    {
                        SelectCells(
                            cell.Item2,
                            tool.SelectionOptions,
                            GameManager.Active.GetActiveState(),
                            selectionCells);
                        if (Input.GetMouseButton(0))
                        {
                            GameManager.Active.Edit((last, next) =>
                            {
                                next.CopyFrom(last);
                                float strength = Time.deltaTime * tool.Strength;

                                for (int i = 0; i < selectionCells.Count; i++)
                                {
                                }
                            });
                        }
                    }
                }
                break;
            }
            case EditToolType.Rainfall:
            {
                var tool = Tools[(int)_selectedTool] as RainfallToolOptions;
                if (tool != null && !EventSystem.current.IsPointerOverGameObject())
                {
                    var cell = HUD.GetMouseCellIndex();
                    if (cell.Item2 >= 0)
                    {
                        SelectCells(
                            cell.Item2,
                            tool.SelectionOptions,
                            GameManager.Active.GetActiveState(),
                            selectionCells);
                        if (Input.GetMouseButton(0))
                        {
                            GameManager.Active.Edit((last, next) =>
                            {
                                next.CopyFrom(last);
                                float strength = Time.deltaTime * tool.Strength;

                                for (int i = 0; i < selectionCells.Count; i++)
                                {
                                }
                            });
                        }
                    }
                }
                break;
            }
        }

        HUD.View.PlanetView.HighlightCells(selectionCells);
    }

    private void SelectCells(int posIndex, CellSelectionOptions options, SimState state, List<Tuple<int, float>> selected)
	{
        var center = GameManager.Active.StaticState.SphericalPosition[posIndex];
        for (int i = 0; i < GameManager.Active.StaticState.Count; i++)
        {
            var p = GameManager.Active.StaticState.SphericalPosition[i];
            float dist = math.distance(p, center) * GameManager.Active.StaticState.PlanetRadius / GameManager.Active.StaticState.CellRadius;
            if (dist <= options.BrushSize)
            {
                float elevation = state.Elevation[i];
                if (elevation >= options.MaskBottom && elevation <= options.MaskTop)
                {
                    float strength = (1 - options.NoiseStrength) + (noise.snoise(p * options.NoisePeriod) * 0.5f + 0.5f) * options.NoiseStrength;

                    selected.Add(new Tuple<int, float>(i, strength * math.pow(1.0f - math.saturate(dist / options.BrushSize), options.Falloff)));
                }
            }
        }

    }

    public void OnToolClicked(GameToolIdentifier tool)
	{
        _selectedTool = tool != null ? tool.Tool : EditToolType.Select;

        var panels = ToolOptionsPanel.transform.GetComponentsInChildren<GameToolIdentifier>(true);
        foreach (var p in panels)
		{
            p.gameObject.SetActive(tool != null && p.Tool == tool.Tool);
		}
    }

    public void SetElevationTarget(TextSlider slider)
	{
        var tool = Tools[(int)_selectedTool] as ElevationToolOptions;
        if (tool != null)
        {
            tool.TargetElevation = slider.value;
        }
    }

    public void SetBrushSize(TextSlider slider)
    {
        var tool = Tools[(int)_selectedTool] as SelectionTool;
        if (tool != null)
        {
            tool.SelectionOptions.BrushSize = slider.value;
        }
    }

    public void SetFalloff(TextSlider slider)
    {
        var tool = Tools[(int)_selectedTool] as SelectionTool;
        if (tool != null)
        {
            tool.SelectionOptions.Falloff = 1.0f / slider.value;
        }
    }


    public void ClampOcean()
    {
        var tool = Tools[(int)_selectedTool] as ElevationToolOptions;
        if (tool != null)
        {
            tool.MaxVal = -100;
            tool.MinVal = float.NegativeInfinity;
            tool.SelectionOptions.MaskTop = 0;
            tool.SelectionOptions.MaskBottom = float.NegativeInfinity;
        }
    }
    public void ClampLand()
    {
        var tool = Tools[(int)_selectedTool] as ElevationToolOptions;
        if (tool != null)
        {
            tool.MaxVal = float.PositiveInfinity;
            tool.MinVal = 100;
            tool.SelectionOptions.MaskTop = float.PositiveInfinity;
            tool.SelectionOptions.MaskBottom = 0;
        }
    }
    public void ClearMask()
    {
        var tool = Tools[(int)_selectedTool] as ElevationToolOptions;
        if (tool != null)
        {
            tool.SelectionOptions.MaskTop = float.PositiveInfinity;
            tool.SelectionOptions.MaskBottom = float.NegativeInfinity;
            tool.MaxVal = float.PositiveInfinity;
            tool.MinVal = float.NegativeInfinity;
        }
    }
    public void SetStrength(TextSlider slider)
    {
        var tool = Tools[(int)_selectedTool] as ElevationToolOptions;
        if (tool != null)
        {
            tool.Strength = slider.value;
        }
    }
    public void SetNoiseStrength(TextSlider slider)
    {
        var tool = Tools[(int)_selectedTool] as SelectionTool;
        if (tool != null)
        {
            tool.SelectionOptions.NoiseStrength = slider.value;
        }
    }
    public void SetNoisePeriod(TextSlider slider)
    {
        var tool = Tools[(int)_selectedTool] as SelectionTool;
        if (tool != null)
        {
            tool.SelectionOptions.NoisePeriod = slider.value;
        }
    }


}
