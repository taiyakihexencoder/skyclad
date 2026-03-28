using Unity.Entities;
using Unity.Mathematics;

namespace skyclad.control {

	/// <summary>
	/// 汎用的なキャラクターコントロール
	/// </summary>
	public struct SkycladCharacterControlComponent : IComponentData {
		/// <summary>
		/// 接地している
		/// </summary>
		public bool isGrounded;

		/// <summary>
		/// 地面の傾き
		/// </summary>
		public float3 normal;

		/// <summary>
		/// 地面に吸着
		/// </summary>
		public bool snapToGround;

		/// <summary>
		/// 吸着処理を無視
		/// </summary>
		public bool ignoreSnapToGround;

		/// <summary>
		/// 受けている外力
		/// </summary>
		public float3 force;

		/// <summary>
		/// スピードの変動
		/// </summary>
		public float3 velocityChanges;

		/// <summary>
		/// 向く方向
		/// </summary>
		public quaternion lookDirection;
	}

	/// <summary>
	/// キャラクターコントロールに渡す指示
	/// </summary>
	public struct CharacterControlInstruction : IComponentData {
		/// <summary>
		/// 現在の速度と望まれる速度の誤差がほぼ0になるのにかかる秒数
		/// </summary>
		public float moveCorrectionSeconds;

		/// <summary>
		/// 望ましい移動速度
		/// </summary>
		public float3 preferMove;

		/// <summary>
		///	見る方向の上書き。
		///	指定されていない場合は移動で自動的に変わる
		/// </summary>
		public quaternion overrideLookDirection;
	}

	/// <summary>
	/// キャラクターにアクションのキューを送る
	/// </summary>
	public struct CharacterActionCueBufferElement : IBufferElementData {
		public CharacterAction action;
	}
}