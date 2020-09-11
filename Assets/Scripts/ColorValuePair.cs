using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;

public struct CVP
{
	public Color32 Color;
	public float Value;
	public CVP(Color c, float v)
	{
		Color = c;
		Value = v;
	}

	public static Color32 Lerp(NativeArray<CVP> colors, float value)
	{
		for (int i = 0; i < colors.Length - 1; i++)
		{
			if (value < colors[i + 1].Value)
			{
				return Color32.Lerp(colors[i].Color, colors[i + 1].Color, (value - colors[i].Value) / (colors[i + 1].Value - colors[i].Value));
			}
		}
		return colors[colors.Length - 1].Color;
	}

};

