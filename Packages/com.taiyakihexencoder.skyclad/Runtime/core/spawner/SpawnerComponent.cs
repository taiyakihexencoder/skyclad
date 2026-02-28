using Unity.Entities;
using Unity.Mathematics;

namespace skyclad {
	public struct SpawnerComponent : IComponentData {
		public long spawnId;
		public Entity prefab;
	}

	public struct RequestSpawnComponent : IComponentData {
		public long spawnId;

		public float3 position;
		public quaternion rotation;
		public float scale;
	}
}