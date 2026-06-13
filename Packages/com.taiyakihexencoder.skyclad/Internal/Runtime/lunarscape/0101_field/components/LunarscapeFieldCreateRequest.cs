using Unity.Collections;
using Unity.Entities;

namespace skyclad.lunarscape.internalProc {
	public struct LunarscapeFieldCreateRequest : IComponentData {
		public Entity entity;
		public int id;
	}
}