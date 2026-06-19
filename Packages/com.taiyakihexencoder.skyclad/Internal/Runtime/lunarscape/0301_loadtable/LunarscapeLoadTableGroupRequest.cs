using Unity.Collections;
using Unity.Entities;

namespace skyclad.lunarscape.internalProc {
	public struct LunarscapeLoadTableGroupRequest : IComponentData {
		public int id;
		public FixedString64Bytes guid;
	}
}