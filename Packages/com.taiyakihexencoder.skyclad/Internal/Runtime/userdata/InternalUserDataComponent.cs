using Unity.Entities;

namespace skyclad.internalProc {
		/// <summary>
	/// ユーザーデータロードのリクエスト
	/// </summary>
	public struct InternalRequestLoadUserDataComponent : IComponentData {
		public int slot;
	}

	/// <summary>
	/// ユーザーデータアンロードのリクエスト
	/// </summary>
	public struct InternalRequestUnloadUserDataComponent : IComponentData { 
	}
	
	/// <summary>
	/// ユーザーデータセーブのリクエスト
	/// </summary>
	public struct InternalRequestSaveUserDataComponent : IComponentData { 
		public int slot;
	}
}