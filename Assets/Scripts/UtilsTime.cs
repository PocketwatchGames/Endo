using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;

static class WorldTime {
	public static float GetTime(float ticks, float spinSpeed)
	{
		return math.modf(ticks * spinSpeed / (math.PI * 2), out var i);
	}
	public static float GetDays(float ticks, float spinSpeed)
	{
		return ticks * spinSpeed / (math.PI * 2);
	}
}
