using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;
using System;

namespace Endo
{
	public class StaticState
	{
		public const int MaxNeighbors = 6;
		public const int MaxNeighborsVert = 8;
		public const int NeighborUp = MaxNeighborsVert - 1;
		public const int NeighborDown = MaxNeighborsVert - 2;

		public int Count;
		public int AnimalCount;
		public float PlanetRadius;
		public float CellSurfaceArea;
		public float CellRadius;
		public float CellCircumference;
		public NativeArray<float2> Coordinate;
		public NativeArray<float3> SphericalPosition;
		public NativeArray<int> Neighbors; // The cell that each edge points to
		public NativeArray<int> ReverseNeighbors; // the index in the edge array that points BACK to the indexed cell
		public NativeArray<int> NeighborsVert;
		public NativeArray<int> ReverseNeighborsVert;
		public NativeArray<float> NeighborDistInverse;
		public NativeArray<float> NeighborDist;
		public NativeArray<float3> NeighborDir;
		public NativeArray<float3> NeighborTangent;
		public NativeArray<float3> NeighborDiffInverse;
		public NativeArray<float> CoriolisMultiplier;
		private WorldData _worldData;


		public void Init(float radius, Icosphere icosphere, WorldData worldData)
		{
			_worldData = worldData;
			PlanetRadius = radius;
			Count = icosphere.Vertices.Length;
			Coordinate = new NativeArray<float2>(Count, Allocator.Persistent);
			SphericalPosition = new NativeArray<float3>(Count, Allocator.Persistent);
			CoriolisMultiplier = new NativeArray<float>(Count, Allocator.Persistent);
			Neighbors = new NativeArray<int>(Count * MaxNeighbors, Allocator.Persistent);
			ReverseNeighbors = new NativeArray<int>(Count * MaxNeighbors, Allocator.Persistent);
			NeighborsVert = new NativeArray<int>(Count * MaxNeighborsVert * worldData.AirLayers, Allocator.Persistent);
			ReverseNeighborsVert = new NativeArray<int>(Count * MaxNeighborsVert * worldData.AirLayers, Allocator.Persistent);
			NeighborDir = new NativeArray<float3>(Count * MaxNeighbors, Allocator.Persistent);
			NeighborDistInverse = new NativeArray<float>(Count * MaxNeighbors, Allocator.Persistent);
			NeighborTangent = new NativeArray<float3>(Count * MaxNeighbors, Allocator.Persistent);
			NeighborDiffInverse = new NativeArray<float3>(Count * MaxNeighbors, Allocator.Persistent);
			NeighborDist = new NativeArray<float>(Count * MaxNeighbors, Allocator.Persistent);
			float surfaceArea = 4 * math.PI * PlanetRadius * PlanetRadius;
			CellSurfaceArea = surfaceArea / Count;
			CellRadius = math.sqrt(CellSurfaceArea / math.PI);
			CellCircumference = math.PI * 2 * CellRadius;

			var neighborList = new List<Tuple<int, float3>>[Count];
			for (int i = 0; i < Count; i++)
			{
				neighborList[i] = new List<Tuple<int, float3>>();
			}

			for (int i = 0; i < Count; i++)
			{
				var v = icosphere.Vertices[i];
				Coordinate[i] = new float2(-math.atan2(v.x, v.z), math.asin(v.y));
				SphericalPosition[i] = new float3(v.x, v.y, v.z);
			}

			for (int i = 0; i < icosphere.Polygons.Count; i++)
			{
				var p = icosphere.Polygons[i];
				for (int j = 0; j < 3; j++)
				{
					int vertIndex = p.m_Vertices[(j + 1) % 3];
					neighborList[p.m_Vertices[j]].Add(new Tuple<int, float3>(vertIndex, SphericalPosition[vertIndex]));
				}
			}
			for (int i = 0; i < Count; i++)
			{
				var pos = SphericalPosition[i];
				var forward = neighborList[i][0].Item2 - pos;
				float forwardLength = math.length(forward);

				neighborList[i].Sort(delegate (Tuple<int, float3> a, Tuple<int, float3> b)
				{
					float3 diffA = a.Item2 - pos;
					float3 diffB = b.Item2 - pos;
					float dotA = math.dot(diffA, forward);
					float dotB = math.dot(diffB, forward);
					float angleA = diffA.Equals(forward) ? 0 : math.acos(dotA / (math.length(diffA) * forwardLength));
					float angleB = diffB.Equals(forward) ? 0 : math.acos(dotB / (math.length(diffB) * forwardLength));
					angleA *= math.sign(math.dot(pos, math.cross(forward, diffA)));
					angleB *= math.sign(math.dot(pos, math.cross(forward, diffB)));
					return (int)math.sign(angleB - angleA);
				});
				for (int j = 0; j < MaxNeighbors; j++)
				{
					int index = i * MaxNeighbors + j;
					if (j < neighborList[i].Count)
					{
						int n = neighborList[i][j].Item1;
						Neighbors[index] = n;
						var diff = SphericalPosition[n] - pos;
						float dist = math.length(diff * PlanetRadius);
						NeighborDist[index] = dist;
						NeighborDistInverse[index] = 1.0f / dist;
						NeighborDir[index] = math.normalize(math.cross(math.cross(pos, diff), pos));
						NeighborDiffInverse[index] = NeighborDir[index] * NeighborDistInverse[index];
						NeighborTangent[index] = NeighborDir[index] * dist;

					}
					else
					{
						Neighbors[index] = -1;
					}
				}
			}

			for (int i = 0; i < Count * MaxNeighbors; i++)
			{
				ReverseNeighbors[i] = -1;
				int cellIndex = i / MaxNeighbors;
				int nIndex = Neighbors[i];
				if (nIndex >= 0)
				{
					for (int j = 0; j < MaxNeighbors; j++)
					{
						if (Neighbors[nIndex * MaxNeighbors + j] == cellIndex)
						{
							ReverseNeighbors[i] = nIndex * MaxNeighbors + j;
						}
					}
					Debug.Assert(Neighbors[ReverseNeighbors[i]] == cellIndex);
				}
			}

			for (int i = 0; i < NeighborsVert.Length; i++)
			{
				int cellIndex = i / MaxNeighborsVert;
				int columnIndex = cellIndex % Count;
				int n = i - cellIndex * MaxNeighborsVert;
				int layer = cellIndex / Count;
				if (n == NeighborUp)
				{
					if (layer >= worldData.AirLayers - 2 || layer == 0)
					{
						NeighborsVert[i] = -1;
					}
					else
					{
						NeighborsVert[i] = cellIndex + Count;
					}
				}
				else if (n == NeighborDown)
				{
					if (layer <= 1 || layer == worldData.AirLayers - 1)
					{
						NeighborsVert[i] = -1;
					}
					else
					{
						NeighborsVert[i] = cellIndex - Count;
					}
				}
				else
				{
					int neighbor = Neighbors[columnIndex * MaxNeighbors + n];
					if (neighbor >= 0)
					{
						NeighborsVert[i] = neighbor + layer * Count;
					}
					else
					{
						NeighborsVert[i] = -1;
					}

				}
				Debug.Assert(NeighborsVert[i] < Count * worldData.AirLayers);
			}

			for (int i = 0; i < NeighborsVert.Length; i++)
			{
				int cellIndex = i / MaxNeighborsVert;
				int columnIndex = cellIndex % Count;
				int n = i - cellIndex * MaxNeighborsVert;
				int layer = cellIndex / Count;
				if (n == NeighborUp)
				{
					if (layer >= worldData.AirLayers - 2 || layer == 0)
					{
						ReverseNeighborsVert[i] = -1;
					}
					else
					{
						ReverseNeighborsVert[i] = i + Count * MaxNeighborsVert - 1;
					}
				}
				else if (n == NeighborDown)
				{
					if (layer <= 1 || layer == worldData.AirLayers - 1)
					{
						ReverseNeighborsVert[i] = -1;
					}
					else
					{
						ReverseNeighborsVert[i] = i - Count * MaxNeighborsVert + 1;
					}
				}
				else
				{
					int neighbor = Neighbors[columnIndex * MaxNeighbors + n];
					if (neighbor >= 0)
					{
						int reverseNeighbor = ReverseNeighbors[columnIndex * MaxNeighbors + n];
						ReverseNeighborsVert[i] = (reverseNeighbor / MaxNeighbors) * MaxNeighborsVert + reverseNeighbor % MaxNeighbors + layer * Count * MaxNeighborsVert;
					}
					else
					{
						ReverseNeighborsVert[i] = -1;
					}

				}
				Debug.Assert(ReverseNeighborsVert[i] < 0 || NeighborsVert[ReverseNeighborsVert[i]] == cellIndex);
			}


			SortedDictionary<float, SortedDictionary<float, int>> vertsByCoord = new SortedDictionary<float, SortedDictionary<float, int>>();
			for (int i = 0; i < Coordinate.Length; i++)
			{
				SortedDictionary<float, int> vertsAtLatitude;
				float latitude = Coordinate[i].y;
				if (!vertsByCoord.TryGetValue(latitude, out vertsAtLatitude))
				{
					vertsAtLatitude = new SortedDictionary<float, int>();
					vertsByCoord.Add(latitude, vertsAtLatitude);
				}
				vertsAtLatitude.Add(Coordinate[i].x, i);

				CoriolisMultiplier[i] = math.sin(latitude);

			}


		}

		public void Dispose()
		{
			Neighbors.Dispose();
			ReverseNeighbors.Dispose();
			NeighborsVert.Dispose();
			ReverseNeighborsVert.Dispose();
			NeighborDir.Dispose();
			NeighborDist.Dispose();
			NeighborDistInverse.Dispose();
			NeighborDiffInverse.Dispose();
			NeighborTangent.Dispose();
			Coordinate.Dispose();
			SphericalPosition.Dispose();
			CoriolisMultiplier.Dispose();
		}

		public int GetWaterIndex(int layer, int i)
		{
			return Count * layer + i;
		}

		public static int GetMaxNeighbors(int cell, NativeSlice<int> neighbors)
		{
			return (neighbors[(cell + 1) * MaxNeighbors - 1] >= 0) ? MaxNeighbors : (MaxNeighbors - 1);
		}

		public int GetLayerIndexAir(int layer, int index)
		{
			return layer * Count + index;
		}
		public int GetLayerIndexWater(int layer, int index)
		{
			return layer * Count + index;
		}

		public NativeSlice<T> GetSliceAir<T>(NativeArray<T> arr) where T : struct
		{
			return new NativeSlice<T>(arr, Count, (_worldData.AirLayers - 2) * Count);
		}
		public NativeSlice<T> GetSliceWater<T>(NativeArray<T> arr) where T : struct
		{
			return new NativeSlice<T>(arr, Count, (_worldData.WaterLayers - 2) * Count);
		}
		public NativeSlice<T> GetSliceAirNeighbors<T>(NativeArray<T> arr) where T : struct
		{
			return new NativeSlice<T>(arr, Count * StaticState.MaxNeighborsVert, (_worldData.AirLayers - 2) * Count * StaticState.MaxNeighborsVert);
		}
		public NativeSlice<T> GetSliceWaterNeighbors<T>(NativeArray<T> arr) where T : struct
		{
			return new NativeSlice<T>(arr, Count * StaticState.MaxNeighborsVert, (_worldData.WaterLayers - 2) * Count * StaticState.MaxNeighborsVert);
		}
		public NativeSlice<T> GetSliceLayer<T>(NativeArray<T> arr, int layer) where T : struct
		{
			return new NativeSlice<T>(arr, layer * Count, Count);
		}
		public NativeSlice<T> GetSliceLayers<T>(NativeArray<T> arr, int layer, int layerCount) where T : struct
		{
			return new NativeSlice<T>(arr, layer * Count, Count * layerCount);
		}

		public static int GetCellIndexFromEdgeVert(int index)
		{
			return index / MaxNeighborsVert;
		}


		public static int GetNextHorizontalNeighborVert(NativeArray<int> neighbors, int edgeIndex)
		{
			int cellIndex = edgeIndex / MaxNeighborsVert;
			int nIndex = neighbors[cellIndex * MaxNeighborsVert + (edgeIndex + 1) % StaticState.MaxNeighbors];
			if (nIndex < 0)
			{
				nIndex = neighbors[cellIndex * MaxNeighborsVert + (edgeIndex + 2) % StaticState.MaxNeighbors];
			}
			return nIndex;
		}

		public static int GetPrevHorizontalNeighborVert(NativeArray<int> neighbors, int edgeIndex)
		{
			int cellIndex = edgeIndex / MaxNeighborsVert;
			int nIndex = neighbors[cellIndex * MaxNeighborsVert + (edgeIndex + StaticState.MaxNeighbors - 1) % StaticState.MaxNeighbors];
			if (nIndex < 0)
			{
				nIndex = neighbors[cellIndex * MaxNeighborsVert + (edgeIndex + StaticState.MaxNeighbors - 2) % StaticState.MaxNeighbors];
			}
			return nIndex;
		}
	}
}