using UnityEngine;

namespace skyclad {
	/// <summary>
	/// 何らかの順番を定義するための属性。
	/// たとえばReflectionによるGetFieldsやGetPropertiesは取得順序が保証されていない
	/// </summary>
	[System.AttributeUsage(System.AttributeTargets.All)]
	public class OrderAttribute : PropertyAttribute {
		public readonly int value;

		public OrderAttribute(int value) {
			this.value = value;
		}
	}
}