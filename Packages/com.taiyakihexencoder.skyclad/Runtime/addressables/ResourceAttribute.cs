using UnityEngine;

namespace skyclad {
	/// <summary>
	/// リソースをpathを絞って選択・設定する
	/// </summary>
	[System.AttributeUsage(System.AttributeTargets.Field)]
	public sealed class ResourceAttribute : PropertyAttribute {
		public readonly string path;
		public ResourceAttribute(string path = null) {
			this.path = path;
		}
	}
}