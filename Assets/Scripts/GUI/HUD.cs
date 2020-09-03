using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class HUD : MonoBehaviour
{
    public WorldView View;


	public Tuple<Vector3, int> GetMouseCellIndex()
	{
		var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;
		int cellIndex = -1;
		Vector3 worldPos = Vector3.zero;
		if (Physics.Raycast(ray, out hit, Mathf.Infinity))
		{
			if (hit.collider.gameObject.transform.parent == View.Planet.transform)
			{
				int tIndex;
				if (hit.barycentricCoordinate.x > hit.barycentricCoordinate.y && hit.barycentricCoordinate.x > hit.barycentricCoordinate.z)
				{
					tIndex = 0;
				}
				else if (hit.barycentricCoordinate.y > hit.barycentricCoordinate.z)
				{
					tIndex = 1;
				}
				else
				{
					tIndex = 2;
				}
				cellIndex = View.PlanetView.GetClosestVert(hit.triangleIndex, tIndex);
			}
		}
		return new Tuple<Vector3, int>(worldPos, cellIndex);
	}

}
