using Unity.Entities;

namespace skyclad.lifecycle {
	/// <summary>
	/// Enter AdventureフェーズのSkycladによる初期化を示すタグ
	/// </summary>
	public struct EnterAdventureSystemLoadComponent : IComponentData {}

	/// <summary>
	/// ワールド開始時処理の後に実行する
	/// </summary>
	public struct AfterWorldLoad : IComponentData, IEnableableComponent { }

	/// <summary>
	/// Exit AdventureフェーズのSkycladによるアンロードを示すタグ
	/// </summary>
	public struct ExitAdventureSystemUnloadComponent : IComponentData {}

	/// <summary>
	/// ワールド終了時処理の前に実行する
	/// </summary>
	public struct BeforeWorldUnload : IComponentData, IEnableableComponent { }

}