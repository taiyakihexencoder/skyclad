using Unity.Entities;
using Unity.Mathematics;

namespace skyclad.lunarscape.internalProc {
	/// <summary>
	/// LunarscapeFieldMeshComponentの親
	/// </summary>
	public struct LunarscapeFieldComponent : IComponentData {
		public int id;
		/// <summary>
		/// LunarscapeFieldMeshComponentの読込状態
		/// 読込開始でONになる。
		/// </summary>
		public bool active;
		public float3 boundsMin;
		public float3 boundsMax;
	}
}