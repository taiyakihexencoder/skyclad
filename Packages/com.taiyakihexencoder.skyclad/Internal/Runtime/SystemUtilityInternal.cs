using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Authoring;

namespace skyclad.internalProc {
	public static class SystemUtilityInternal {
		public static void QuitApp() {
#if UNITY_EDITOR
			D.Log("Quit Requested.");
			UnityEditor.EditorApplication.isPlaying = false;
#else
			Application.Quit();
#endif
		}

		public static void ApplyPhysics(
			World world,
			bool drawDebugColliders
		) {
			EntityManager entityManager = world.EntityManager;
			PhysicsStep physicsStep = PhysicsStep.Default;
			physicsStep.SynchronizeCollisionWorld = 1;
			physicsStep.SimulationType = SimulationType.UnityPhysics;
			Entity entity = entityManager.CreateSingleton(physicsStep, "Physics");
			
			// Debug
			entityManager.AddComponentData(entity, new PhysicsDebugDisplayData { 
				DrawColliders = drawDebugColliders ? 1 : 0, 
			});
		}
	}
}