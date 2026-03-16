using System.Threading;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Authoring;
using UnityEngine;

namespace skyclad.lifecycle {
	/// <summary>
	/// アプリケーション開始時の処理の実装
	/// </summary>
	internal static class OnLaunch {
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Launch() {
			OnLaunchHandler();

			// 仮
			OnStartIngame.StartIngame();
		}

		private static void OnLaunchHandler() {
			Debug.Log("Launch.");
			SynchronizationContext mainContext = SynchronizationContext.Current;

			SkycladDataTables.Init(mainContext);
			character.CharacterCollider.InitColliders();

			EntityManager entityManager = SkycladUtility.ECS.EntityManager;
			PhysicsStep physicsStep = PhysicsStep.Default;
			physicsStep.SynchronizeCollisionWorld = 1;
			physicsStep.SimulationType = SimulationType.UnityPhysics;
			Entity entity = entityManager.CreateSingleton(physicsStep, "Physics");
			entityManager.AddComponentData(entity, new PhysicsDebugDisplayData { DrawColliders = 1, });
		}
	}
}