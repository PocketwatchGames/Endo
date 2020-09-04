using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToolElevationPanel : MonoBehaviour
{
    public EditHUD EditHUD;
    public TextSlider BrushSize;
    public TextSlider Falloff;
    public TextSlider TargetElevation;
    public TextSlider NoiseStrength;
    public TextSlider NoisePeriod;
    public TextSlider Strength;
    public Toggle MaskNone, MaskLand, MaskOcean;

    // Start is called before the first frame update
    void Start()
    {
        UpdateOptions();
    }

    private void UpdateOptions()
	{
        var options = EditHUD.Tools[(int)EditHUD.EditToolType.Elevation] as EditHUD.ElevationToolOptions;
        BrushSize.value = options.SelectionOptions.BrushSize;
        Falloff.value = 1.0f / options.SelectionOptions.Falloff;
        TargetElevation.value = options.TargetElevation;
        NoiseStrength.value = options.SelectionOptions.NoiseStrength;
        NoisePeriod.value = options.SelectionOptions.NoisePeriod;
        Strength.value = options.Strength;

        if (options.SelectionOptions.MaskTop == 0 && options.SelectionOptions.MaskBottom <= float.MinValue)
        {
            MaskOcean.isOn = true;
        }
        else if (options.SelectionOptions.MaskBottom == 0 && options.SelectionOptions.MaskTop >= float.MaxValue)
        {
            MaskLand.isOn = true;
        }
        else
        {
            MaskNone.isOn = true;
        }
    }

    EditHUD.ElevationToolOptions[] ElevationToolPresets = new[]
    {
        new EditHUD.ElevationToolOptions
        {
            SelectionOptions = new EditHUD.CellSelectionOptions
            {
                BrushSize = 1,
                Falloff = 0.01f,
                MaskBottom = float.MinValue,
                MaskTop = float.MaxValue
            },
            TargetElevation = 100,
            Strength = 100000,
            MinVal = float.MinValue,
            MaxVal = float.MaxValue,
        },
        new EditHUD.ElevationToolOptions
        {
            SelectionOptions = new EditHUD.CellSelectionOptions
            {
                BrushSize = 1,
                Falloff = 0.01f,
                MaskBottom = float.MinValue,
                MaskTop = float.MaxValue
            },
            TargetElevation = -100,
            Strength = 100000,
            MinVal = float.MinValue,
            MaxVal = float.MaxValue,
        },
        new EditHUD.ElevationToolOptions
        {
            SelectionOptions = new EditHUD.CellSelectionOptions
            {
                BrushSize = 1,
                Falloff = 0.25f,
                MaskBottom = 0,
                MaskTop = float.MaxValue,
            },
            TargetElevation = 1000,
            Strength = 5000,
            MinVal = float.MinValue,
            MaxVal = float.MaxValue,
        },
        new EditHUD.ElevationToolOptions
        {
            SelectionOptions = new EditHUD.CellSelectionOptions
            {
                BrushSize = 1,
                Falloff = 0.25f,
                MaskBottom = 0,
                MaskTop = float.MaxValue
            },
            TargetElevation = 2500,
            Strength = 5000,
            MinVal = float.MinValue,
            MaxVal = float.MaxValue,
        },
        new EditHUD.ElevationToolOptions
        {
            SelectionOptions = new EditHUD.CellSelectionOptions
            {
                BrushSize = 1,
                Falloff = 0.5f,
                MaskBottom = 0,
                MaskTop = float.MaxValue
            },
            TargetElevation = 5000,
            Strength = 5000,
            MinVal = float.MinValue,
            MaxVal = float.MaxValue,
        },
        new EditHUD.ElevationToolOptions
        {
            SelectionOptions = new EditHUD.CellSelectionOptions
            {
                BrushSize = 1,
                Falloff = 0.25f,
                MaskTop = 0,
                MaskBottom = float.MinValue,
            },
            TargetElevation = -1000,
            Strength = 5000,
            MinVal = float.MinValue,
            MaxVal = float.MaxValue,
        },
        new EditHUD.ElevationToolOptions
        {
            SelectionOptions = new EditHUD.CellSelectionOptions
            {
                BrushSize = 1,
                Falloff = 0.25f,
                MaskTop = 0,
                MaskBottom = float.MaxValue
            },
            TargetElevation = -2500,
            Strength = 5000,
            MinVal = float.MinValue,
            MaxVal = float.MaxValue,
        },
        new EditHUD.ElevationToolOptions
        {
            SelectionOptions = new EditHUD.CellSelectionOptions
            {
                BrushSize = 1,
                Falloff = 0.5f,
                MaskTop = 0,
                MaskBottom = float.MaxValue
            },
            TargetElevation = -5000,
            Strength = 5000,
            MinVal = float.MinValue,
            MaxVal = float.MaxValue,
        },
        new EditHUD.ElevationToolOptions
        {
            SelectionOptions = new EditHUD.CellSelectionOptions
            {
                BrushSize = 1,
                Falloff = 0.5f,
                MaskTop = float.MinValue,
                MaskBottom = float.MaxValue,
                NoiseStrength = 1,
                NoisePeriod = 10
            },
            TargetElevation = float.MaxValue,
            Strength = 5000,
            MinVal = float.MinValue,
            MaxVal = float.MaxValue,
        },
    };


    public void PresetClicked(int index)
    {
        (EditHUD.Tools[(int)EditHUD.EditToolType.Elevation] as EditHUD.ElevationToolOptions).CopyFrom(ElevationToolPresets[index]);
        UpdateOptions();
    }

}
