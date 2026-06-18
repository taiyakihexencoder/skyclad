using Unity.Collections;
using Unity.Entities;

namespace skyclad.lunarscape.internalProc {
	public struct LunarscapeLoadTableRequest : IComponentData {
		public FixedString64Bytes guid;
	}
}