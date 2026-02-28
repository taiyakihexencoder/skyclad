using Unity.Entities;

namespace skyclad {
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	public partial class SkycladSimulationSystemGroup : ComponentSystemGroup { }

	[UpdateInGroup(typeof(SkycladSimulationSystemGroup))]
	public partial class SkycladInputSystemGroup : ComponentSystemGroup { }
}