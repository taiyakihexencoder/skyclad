using Unity.Mathematics;
using UnityEngine;

namespace skyclad.editor {
	[System.Serializable]
	internal struct CharacterProjectSettings {
		/// <summary>
		/// キャラクター単位
		/// </summary>
		[System.Serializable]
		internal struct Unit {
			/// <summary>
			/// 表示名
			/// </summary>
			public string name;

			/// <summary>
			/// 識別子
			/// </summary>
			public string guid;

			/// <summary>
			/// プレイヤーキャラクターかどうか
			/// </summary>
			public bool isPlayerCharacter;

			/// <summary>
			/// Colliderのguid
			/// </summary>
			public string colliderGuid;

			/// <summary>
			/// Controllerのguid
			/// </summary>
			public string controllerGuid;

			/// <summary>
			/// キャラクタータイプ関連の設定
			/// </summary>
			public CharacterType type;

			/// <summary>
			/// ステータス
			/// </summary>
			public ParameterInfo status;

			/// <summary>
			/// コントローラーパラメーター
			/// </summary>
			public ParameterInfo controller;

			/// <summary>
			/// ヒットボックス
			/// </summary>
			public CharacterHitBox[] hitBoxes;
		}

		internal enum ParameterType {
			Int,
			Bool,
			Float,
			Float2,
			Float3,
		}

		/// <summary>
		/// パラメーター定義
		/// </summary>
		[System.Serializable]
		internal struct ParameterDefs {
			/// <summary>
			/// パラメーター種別
			/// </summary>
			public ParameterType parameterType;

			/// <summary>
			/// パラメーター名
			/// </summary>
			public string name;
		}

		/// <summary>
		/// 個別のパラメーター設定
		/// 型ごとに保持したほうが効率的だが、
		/// 管理がややこしいのでstringに
		/// </summary>
		[System.Serializable]
		internal struct ParameterInfo {
			public string[] names;
			public string[] values;
		}

		/// <summary>
		/// Collider単位
		/// </summary>
		[System.Serializable]
		internal struct ColliderUnit {
			/// <summary>
			/// 識別子
			/// </summary>
			public string guid;

			/// <summary>
			/// 表示名
			/// </summary>
			public string name;

			/// <summary>
			/// Capsule半径
			/// </summary>
			public float radius;

			/// <summary>
			/// Capsule高さ
			/// </summary>
			public float height;
		}

		[System.Serializable]
		public struct CharacterType {
			/// <summary>
			/// Colliderがあるか
			/// </summary>
			public bool hasCollider;

			/// <summary>
			/// ステータスを持つか
			/// </summary>
			public bool hasStatus;

			/// <summary>
			/// コントローラー設定するか
			/// </summary>
			public bool hasController;
		}

		/// <summary>
		/// キャラクターの操作系
		/// </summary>
		[System.Serializable]
		public struct CharacterController {
			public string guid;
			public string name;

			[SerializeField]
			private ParameterDefs[] _parameters;
			public ParameterDefs[] Parameters => _parameters;

			public static CharacterController Create(string guid, string name, ParameterDefs[] parameterDefs) {
				return new CharacterController {
					guid = guid,
					name = name,
					_parameters = parameterDefs,
				};
			}
		}

		[System.Serializable]
		public struct CharacterHitBox {
			public Vector3 offset;
			public Vector3 extent;
		}

		[SerializeField]
		private Unit[] _units;
		public Unit[] Units => _units;

		[SerializeField]
		private ParameterDefs[] _statusParameterUnits;
		public ParameterDefs[] StatusParaneterUnits => _statusParameterUnits;

		[SerializeField]
		private ColliderUnit[] _colliderUnits;
		public ColliderUnit[] ColliderUnits => _colliderUnits;

		[SerializeField]
		private CharacterController[] _controllers;
		public CharacterController[] controllers => _controllers;
	}
}