using System.IO;
using UnityEngine;

namespace skyclad.editor {
	[System.Serializable]
	public sealed class FieldProjectSettings {
		private static string _assetPath = $"Skyclad{Path.DirectorySeparatorChar}auto-generated{Path.DirectorySeparatorChar}asset{Path.DirectorySeparatorChar}field";
		internal static string AssetPath => _assetPath;

		[SerializeField]
		private FieldType _fieldType = FieldType.Field2DSideView;
		public FieldType FieldType => _fieldType;

		/// <summary>
		/// サイドビューの設定
		/// </summary>
		[System.Serializable]
		public sealed class SideViewSettings {
			[SerializeField]
			private float _width = 10.0f;
			/// <summary>
			/// z軸方向の幅
			/// </summary>
			public float Width => _width;
			
			private float _zOffset = 0.0f;

			/// <summary>
			/// z軸オフセット
			/// </summary>
			public float ZOffset => _zOffset;
		}

		private SideViewSettings _sideView = new SideViewSettings();
		public SideViewSettings SideView => _sideView;

	}
}