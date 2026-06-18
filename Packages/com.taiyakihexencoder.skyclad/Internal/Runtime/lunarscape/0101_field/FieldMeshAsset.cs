using UnityEngine;

namespace skyclad.lunarscape.internalProc {
	/// <summary>
	/// アセットの親オブジェクト
	/// </summary>
	public sealed class FieldMeshAsset : ScriptableObject { 
		/// <summary>
		/// エディタアセットのGuid
		/// LoadTableとの対応関係のために必要。
		/// </summary>
		public string guid;
		public string[] subassets;
	}
}