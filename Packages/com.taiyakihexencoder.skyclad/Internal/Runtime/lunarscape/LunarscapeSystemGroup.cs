using Unity.Entities;

namespace skyclad.lunarscape.internalProc {
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	public sealed partial class LunarscapeSimulationSystemGroup : ComponentSystemGroup { }
}