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

			/// <summary>
			/// ヒットボックスのレイヤー
			/// </summary>
			public uint hitBoxLayer;
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
		private string[] _dynamicParameters;
		public string[] DynamicParameters => _dynamicParameters;

		[SerializeField]
		private ColliderUnit[] _colliderUnits;
		public ColliderUnit[] ColliderUnits => _colliderUnits;

		[SerializeField]
		private CharacterController[] _controllers;
		public CharacterController[] controllers => _controllers;
	}
}