using Unity.Entities;
using Unity.Mathematics;

namespace skyclad.lunarscape.internalProc {
	/// <summary>
	/// フィールド読み込みの基準となる地点情報
	/// </summary>
	public partial struct LunarscapeFieldObservePoint : IComponentData {
		public int index;
		public float3 position;
	}
}