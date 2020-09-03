using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;

public class Icosphere : MonoBehaviour {


	[HideInInspector] public List<Polygon> Polygons = new List<Polygon>();
	[HideInInspector] public NativeArray<float3> Vertices = new NativeArray<float3>();
	[HideInInspector] public NativeArray<int> Neighbors;
	[HideInInspector] public Mesh Mesh;

	private List<int> _indices = new List<int>();

	public void Init(int recursions)
	{
		Polygons = new List<Polygon>();

		// An icosahedron has 12 vertices, and
		// since they're completely symmetrical the
		// formula for calculating them is kind of
		// symmetrical too:

		float t = (1.0f + Mathf.Sqrt(5.0f)) / 2.0f;

		List<float3> vertexList = new List<float3>();
		vertexList.Add(math.normalize(new float3(-1, t, 0)));
		vertexList.Add(math.normalize(new float3(1, t, 0)));
		vertexList.Add(math.normalize(new float3(-1, -t, 0)));
		vertexList.Add(math.normalize(new float3(1, -t, 0)));
		vertexList.Add(math.normalize(new float3(0, -1, t)));
		vertexList.Add(math.normalize(new float3(0, 1, t)));
		vertexList.Add(math.normalize(new float3(0, -1, -t)));
		vertexList.Add(math.normalize(new float3(0, 1, -t)));
		vertexList.Add(math.normalize(new float3(t, 0, -1)));
		vertexList.Add(math.normalize(new float3(t, 0, 1)));
		vertexList.Add(math.normalize(new float3(-t, 0, -1)));
		vertexList.Add(math.normalize(new float3(-t, 0, 1)));

		// And here's the formula for the 20 sides,
		// referencing the 12 vertices we just created.

		Polygons.Add(new Polygon(0, 11, 5));
		Polygons.Add(new Polygon(0, 5, 1));
		Polygons.Add(new Polygon(0, 1, 7));
		Polygons.Add(new Polygon(0, 7, 10));
		Polygons.Add(new Polygon(0, 10, 11));
		Polygons.Add(new Polygon(1, 5, 9));
		Polygons.Add(new Polygon(5, 11, 4));
		Polygons.Add(new Polygon(11, 10, 2));
		Polygons.Add(new Polygon(10, 7, 6));
		Polygons.Add(new Polygon(7, 1, 8));
		Polygons.Add(new Polygon(3, 9, 4));
		Polygons.Add(new Polygon(3, 4, 2));
		Polygons.Add(new Polygon(3, 2, 6));
		Polygons.Add(new Polygon(3, 6, 8));
		Polygons.Add(new Polygon(3, 8, 9));
		Polygons.Add(new Polygon(4, 9, 5));
		Polygons.Add(new Polygon(2, 4, 11));
		Polygons.Add(new Polygon(6, 2, 10));
		Polygons.Add(new Polygon(8, 6, 7));
		Polygons.Add(new Polygon(9, 8, 1));

		Subdivide(recursions, vertexList);

		Vertices = new NativeArray<float3>(vertexList.ToArray(), Allocator.Persistent);
		InitNeighbors();

		for (int i = 0; i < Polygons.Count; i++)
		{
			_indices.Add(Polygons[i].m_Vertices[0]);
			_indices.Add(Polygons[i].m_Vertices[2]);
			_indices.Add(Polygons[i].m_Vertices[1]);
		}

		var mesh = new Mesh();
		mesh.SetVertices(Vertices);
		mesh.SetTriangles(_indices.ToArray(), 0);
		mesh.RecalculateBounds();

	}

	public void Dispose()
	{
		Vertices.Dispose();
		Neighbors.Dispose();
	}

	private void Subdivide(int recursions, List<float3> vertexList)
	{
		var midPointCache = new Dictionary<int, int>();

		for (int i = 0; i < recursions; i++)
		{
			var newPolys = new List<Polygon>();
			foreach (var poly in Polygons)
			{
				int a = poly.m_Vertices[0];
				int b = poly.m_Vertices[1];
				int c = poly.m_Vertices[2];

				// Use GetMidPointIndex to either create a
				// new vertex between two old vertices, or
				// find the one that was already created.

				int ab = GetMidPointIndex(midPointCache, a, b, vertexList);
				int bc = GetMidPointIndex(midPointCache, b, c, vertexList);
				int ca = GetMidPointIndex(midPointCache, c, a, vertexList);

				// Create the four new polygons using our original
				// three vertices, and the three new midpoints.
				newPolys.Add(new Polygon(a, ab, ca));
				newPolys.Add(new Polygon(b, bc, ab));
				newPolys.Add(new Polygon(c, ca, bc));
				newPolys.Add(new Polygon(ab, bc, ca));
			}
			// Replace all our old polygons with the new set of
			// subdivided ones.
			Polygons = newPolys;
		}
	}
	private int GetMidPointIndex(Dictionary<int, int> cache, int indexA, int indexB, List<float3> vertexList)
	{
		// We create a key out of the two original indices
		// by storing the smaller index in the upper two bytes
		// of an integer, and the larger index in the lower two
		// bytes. By sorting them according to whichever is smaller
		// we ensure that this function returns the same result
		// whether you call
		// GetMidPointIndex(cache, 5, 9)
		// or...
		// GetMidPointIndex(cache, 9, 5)

		int smallerIndex = Mathf.Min(indexA, indexB);
		int greaterIndex = Mathf.Max(indexA, indexB);
		int key = (smallerIndex << 16) + greaterIndex;

		// If a midpoint is already defined, just return it.

		int ret;
		if (cache.TryGetValue(key, out ret))
			return ret;

		// If we're here, it's because a midpoint for these two
		// vertices hasn't been created yet. Let's do that now!

		float3 p1 = vertexList[indexA];
		float3 p2 = vertexList[indexB];
		float3 middle = math.normalize(math.lerp(p1, p2, 0.5f));

		ret = vertexList.Count;
		vertexList.Add(middle);

		// Add our new midpoint to the cache so we don't have
		// to do this again. =)

		cache.Add(key, ret);
		return ret;
	}

	private void InitNeighbors()
	{
		Neighbors = new NativeArray<int>(Vertices.Length * 6, Allocator.Persistent);
		var neighborList = new List<Tuple<int, float3>>[Vertices.Length];
		for (int i = 0; i < Vertices.Length; i++)
		{
			neighborList[i] = new List<Tuple<int, float3>>();
		}

		for (int i = 0; i < Polygons.Count; i++)
		{
			var p = Polygons[i];
			for (int j = 0; j < 3; j++)
			{
				int vertIndex = p.m_Vertices[(j + 1) % 3];
				neighborList[p.m_Vertices[j]].Add(new Tuple<int, float3>(vertIndex, Vertices[vertIndex]));
			}
		}
		for (int i = 0; i < Vertices.Length; i++)
		{
			var pos = Vertices[i];
			var forward = math.normalize(neighborList[i][0].Item2 - pos);

			neighborList[i].Sort(delegate (Tuple<int, float3> a, Tuple<int, float3> b)
			{
				float3 diffA = math.normalize(a.Item2 - pos);
				float3 diffB = math.normalize(b.Item2 - pos);
				float dotA = math.dot(diffA, forward);
				float dotB = math.dot(diffB, forward);
				float angleA = diffA.Equals(forward) ? 0 : math.acos(dotA);
				float angleB = diffB.Equals(forward) ? 0 : math.acos(dotB);
				angleA *= math.dot(pos, math.cross(forward, diffA)) >= 0 ? 1 : -1;
				angleB *= math.dot(pos, math.cross(forward, diffB)) >= 0 ? 1 : -1;
				return (int)math.sign(angleB - angleA);
			});
			for (int j = 0; j < 6; j++)
			{
				int index = i * 6 + j;
				if (j < neighborList[i].Count)
				{
					int n = neighborList[i][j].Item1;
					Neighbors[index] = n;
				}
				else
				{
					Neighbors[index] = -1;
				}
			}
		}
	}
}
