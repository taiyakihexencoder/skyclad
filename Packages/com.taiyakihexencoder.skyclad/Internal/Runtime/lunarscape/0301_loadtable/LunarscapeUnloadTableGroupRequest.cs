using Unity.Collections;
using Unity.Entities;

namespace skyclad.lunarscape.internalProc {
	public struct LunarscapeUnloadTableGroupRequest : IComponentData {
		public FixedString64Bytes guid;
	}
}