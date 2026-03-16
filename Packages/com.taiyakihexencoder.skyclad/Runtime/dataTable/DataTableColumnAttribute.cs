using UnityEngine;

namespace skyclad {
	/// <summary>
	/// DataTable生成対象に設定する属性。
	/// [tableName]: 生成するテーブルbytesの名前
	/// </summary>
	[System.AttributeUsage(System.AttributeTargets.Struct)]
	public sealed class DataTableColumnAttribute : PropertyAttribute {
		public readonly string tableName;
		public readonly DataTableType tableType;

		internal DataTableColumnAttribute(
			string tableName, 
			DataTableType tableType = DataTableType.Manual
		) {
			this.tableName = tableName;
			this.tableType = tableType;
		}
	}

	public enum DataTableType {
		/// <summary>
		/// 手動でロード・アンロードを行う。
		/// </summary>
		Manual,

		/// <summary>
		/// 起動時に読み込み、終了までアンロードしない。
		/// </summary>
		Persistent,

		/// <summary>
		/// インゲーム開始時にロードし、インゲーム終了時にアンロードする。
		/// </summary>
		Ingame,
	}
}