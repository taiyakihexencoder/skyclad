using UnityEngine;

namespace skyclad.editor {
	[System.Serializable]
	internal sealed class BulletProjectSettings {
		[System.Serializable]
		internal struct Group {
			/// <summary>
			/// グループ
			/// </summary>
			public Unit[] units;

			/// <summary>
			/// 物理レイヤー
			/// </summary>
			public uint hitBoxLayer;

			public string guid;
			public string name;
		}

		[System.Serializable]
		internal struct Unit {
			/// <summary>
			/// 表示名
			/// </summary>
			public string name;

			/// <summary>
			/// ヒットボックスの形状タイプ
			/// </summary>
			public BulletHitBoxType hitBoxType;

			/// <summary>
			/// Box: 幅・高さ・奥行
			/// Sphere: 半径
			/// Cylinder: 底面半径・高さ
			/// </summary>
			public Vector3 extent;


			/// <summary>
			/// パラメーター
			/// </summary>
			public ParameterInfo parameter;
		}

		[SerializeField]
		private Group[] _groups;
		public Group[] groups => _groups;

		[SerializeField]
		private ParameterDefs[] _parameterDefs;
		public ParameterDefs[] ParameterDefs => _parameterDefs;
	}
}