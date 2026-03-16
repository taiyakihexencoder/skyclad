using UnityEngine;

namespace skyclad.editor {
	/// <summary>
	/// Dioramaのプロジェクト設定
	/// </summary>
	[System.Serializable]
	internal struct DioramaProjectSettings {
		/// <summary>
		/// Dioramaの単位
		/// </summary>
		[System.Serializable]
		internal struct Unit {
			/// <summary>
			/// 識別子
			/// </summary>
			public string guid;

			/// <summary>
			/// 名前
			/// </summary>
			public string name;

			/// <summary>
			/// 開始タイミングで読み込むかどうか
			/// </summary>
			public bool loadOnLaunch;

			/// <summary>
			/// 基準位置
			/// </summary>
			public Vector3 basis;

			/// <summary>
			/// 表示キャラクター
			/// </summary>
			public Character[] characters;

			/// <summary>
			/// フィールドメッシュ
			/// </summary>
			public field.FieldAssetReference[] fieldAssets;
		}

		/// <summary>
		/// 表示キャラクター情報
		/// </summary>
		[System.Serializable]
		internal struct Character {
			/// <summary>
			/// 識別子
			/// </summary>
			public string guid;

			/// <summary>
			/// 位置
			/// </summary>
			public Vector3 position;

			/// <summary>
			/// 向き
			/// </summary>
			public Quaternion rotation;
		}

		[SerializeField]
		private Unit[] _units;

		/// <summary>
		/// Dioramaの単位
		/// </summary>
		public Unit[] Units => _units;
	}
}