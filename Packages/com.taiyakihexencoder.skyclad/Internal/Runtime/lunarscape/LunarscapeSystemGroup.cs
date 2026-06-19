using Unity.Entities;
using Unity.Transforms;

namespace skyclad.lunarscape.internalProc {
	[UpdateInGroup(typeof(SimulationSystemGroup)), UpdateBefore(typeof(TransformSystemGroup))]
	public sealed partial class LunarscapeSimulationSystemGroup : ComponentSystemGroup { }

	[UpdateInGroup(typeof(SimulationSystemGroup)), UpdateAfter(typeof(TransformSystemGroup))]
	public sealed partial class LunarscapeSimulationAfterTransformSystemGroup : ComponentSystemGroup { }

	[UpdateInGroup(typeof(LunarscapeSimulationSystemGroup))]
	public sealed partial class LunarscapeSimulationFieldSystemGroup : ComponentSystemGroup { }

	[UpdateInGroup(typeof(LunarscapeSimulationSystemGroup))]
	public sealed partial class LunarscapeSimulationAvatarSystemGroup : ComponentSystemGroup { }
}