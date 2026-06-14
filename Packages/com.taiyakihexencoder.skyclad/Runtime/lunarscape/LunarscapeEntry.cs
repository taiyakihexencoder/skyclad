using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace skyclad.lunarscape {
	public struct LunarscapeEntry : IBufferElementData {
		public FixedString64Bytes entryName;
		public float3 position;
		public quaternion rotation;
	}
}