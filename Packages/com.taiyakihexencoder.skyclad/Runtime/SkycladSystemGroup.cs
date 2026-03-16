using Unity.Entities;
using Unity.Physics.Systems;

namespace skyclad {
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	public partial class SkycladSimulationSystemGroup : ComponentSystemGroup { }

	[UpdateInGroup(typeof(SkycladSimulationSystemGroup))]
	public partial class SkycladInputSystemGroup : ComponentSystemGroup { }

	[UpdateInGroup(typeof(SkycladSimulationSystemGroup))]
	public partial class SkycladCharacterSystemGroup : ComponentSystemGroup { }

	[UpdateInGroup(typeof(SkycladSimulationSystemGroup))]
	public partial class SkycladDioramaSystemGroup : ComponentSystemGroup { }

	[UpdateInGroup(typeof(SkycladSimulationSystemGroup))]
	public partial class SkycladFieldSystemGroup : ComponentSystemGroup { }

	[UpdateInGroup(typeof(SkycladSimulationSystemGroup))]
	public partial class SkycladDataTableSystemGroup : ComponentSystemGroup { }

	[UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
	public partial class SkycladColliderGroup : ComponentSystemGroup { }
}
