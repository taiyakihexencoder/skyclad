using Unity.Entities;
using Unity.Mathematics;

namespace skyclad.input {
	/// <summary>
	/// 入力を検知するEntityのリスト
	/// </summary>
	public struct InputListenerBufferElement : IBufferElementData {
		public Entity target;
		public bool enabled;
	}

	/// <summary>
	/// 入力を検知するEntityのON/OFFリクエスト
	/// InputListenerBufferElementに含まれない場合は新規で追加される
	/// </summary>
	public struct RequestUpdateInputListenerComponent : IComponentData {
		public Entity target;
		public bool enabled;
	}

	/// <summary>
	/// スティック入力情報
	/// </summary>
	public struct InputAxisStateComponent : IComponentData {
		public float2 axis0;
		public float2 axis1;
	}

	/// <summary>
	/// 4ボタンの入力情報
	/// </summary>
	public struct InputMainButtonStateComponent : IComponentData {
		public bool button0;
		public bool button1;
		public bool button2;
		public bool button3;
	}

	/// <summary>
	/// サイドボタンの入力情報
	/// </summary>
	public struct InputSideButtonStateComponent : IComponentData {
		public bool buttonBumperL;
		public bool buttonBumperR;
		public bool buttonTriggerL;
		public bool buttonTriggerR;
		public bool buttonStickL;
		public bool buttonStickR;
	}

	/// <summary>
	/// その他ボタンの入力情報
	/// </summary>
	public struct InputOtherButtonStateComponent : IComponentData {
		public bool buttonStart;
		public bool buttonSelect;
	}

	/// <summary>
	/// ボタンを押した瞬間の情報
	/// </summary>
	public struct InputButtonDownEventBufferElement : IBufferElementData {
		public InputButton button;
	}

	/// <summary>
	/// ボタンを離した瞬間の情報
	/// </summary>
	public struct InputButtonUpEventBufferElement : IBufferElementData {
		public InputButton button;
	}
}
