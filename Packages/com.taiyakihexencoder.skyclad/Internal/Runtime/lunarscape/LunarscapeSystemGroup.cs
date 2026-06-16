using Unity.Entities;

namespace skyclad.lunarscape.internalProc {
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	public sealed partial class LunarscapeSimulationSystemGroup : ComponentSystemGroup { }

	[UpdateInGroup(typeof(LunarscapeSimulationSystemGroup))]
	public sealed partial class LunarscapeSimulationFieldSystemGroup : ComponentSystemGroup { }

	[UpdateInGroup(typeof(LunarscapeSimulationSystemGroup))]
	public sealed partial class LunarscapeSimulationAvatarSystemGroup : ComponentSystemGroup { }
}