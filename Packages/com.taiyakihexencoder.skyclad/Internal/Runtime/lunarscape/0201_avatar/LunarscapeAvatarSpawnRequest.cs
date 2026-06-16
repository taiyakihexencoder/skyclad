using Unity.Entities;
using Unity.Mathematics;

namespace skyclad.lunarscape.internalProc {
	public struct LunarscapeAvatarSpawnRequest : IComponentData {
		public int id;
		public float3 position;
		public quaternion rotation;
	}
}