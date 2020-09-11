using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

namespace Endo
{
	[BurstCompile]
	public struct UpdateExplorationJob : IJob
	{
		public NativeArray<float> Exploration;
		[ReadOnly] public NativeSlice<int> AnimalSpecies;
		[ReadOnly] public NativeSlice<int> AnimalPositions;

		public void Execute()
		{
			for (int i = 0; i < AnimalSpecies.Length; i++)
			{
				if (AnimalSpecies[i] > 0)
				{
					Exploration[AnimalPositions[i]] = 1;
				}
			}
		}
	}
}
