using Unity.Entities;
using Unity.Physics.Systems;

namespace skyclad {
	// Simulation

	[UpdateInGroup(typeof(SimulationSystemGroup))]
	public partial class SkycladSimulationSystemGroup : ComponentSystemGroup { }

	[UpdateInGroup(typeof(SkycladSimulationSystemGroup))]
	public partial class SkycladLifecycleSystemGroup : ComponentSystemGroup { }

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

	// LateSimulation

	[UpdateInGroup(typeof(LateSimulationSystemGroup))]
	public partial class SkycladCameraLateUpdateSystemGroup : ComponentSystemGroup { }

	// FixedStepSimulation

	[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
	public partial class SkycladFixedStepSimulationSystemGroup : ComponentSystemGroup { }

	// AfterPhysics

	[UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
	public partial class SkycladColliderGroup : ComponentSystemGroup { }

}
