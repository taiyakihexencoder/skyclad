using System.Collections.Generic;

namespace skyclad.editor {
	/// <summary>
	/// 操作履歴
	/// </summary>
	internal sealed class GraphUIHistory {
		internal class GraphUILog {
			internal GraphUILogType type;
			internal string data;
		}

		internal enum GraphUILogType {
			/// <summary>
			/// データ型：GraphUIData.Variable
			/// </summary>
			AddVariable,

			/// <summary>
			/// データ型：{4桁のindex}:{GraphUIData.Variable}
			/// </summary>
			RemoveVariable,

			/// <summary>
			/// データ型：{variableGuid},{変更前},{変更後}
			/// </summary>
			RenameVariable,

			/// <summary>
			/// データ型：HistoryNode
			/// </summary>
			AddNode,

			/// <summary>
			/// データ型：HistoryDeleted
			/// </summary>
			RemoveGraphElements,

			/// <summary>
			/// データ型：variableGuid 移動前x座標 移動前y座標 移動後x座標 移動後y座標を'/'で区切る
			/// </summary>
			MoveNode,

			/// <summary>
			/// データ型：GraphUIData.Edge
			/// </summary>
			AddEdge,

			/// <summary>
			/// ノードの内部状態の変更
			/// データ型：guid|extra before update|extra after update
			/// </summary>
			ChangeExtra,

			/// <summary>
			/// ペーストによる追加
			/// データ型：GraphDataLogic.GraphParts
			/// </summary>
			Paste,
		}

		private List<GraphUILog> _logs;
		private int _pointer;

		internal GraphUIHistory() {
			_logs = new List<GraphUILog>();
			_pointer = 0;
		}

		internal void AddLog(GraphUILogType type, string data) {
			if (_pointer > 0) {
				_logs.RemoveRange(_logs.Count-_pointer, _pointer);
				_pointer = 0;
			}
			_logs.Add(new GraphUILog{ type = type, data = data,});
		}

		internal bool Undo(out GraphUILog log) {
			if (_pointer < _logs.Count) {
				log = _logs[_logs.Count-_pointer-1];
				_pointer++;
				return true;
			} else {
				log = null;
				return false;
			}
		}

		internal bool Redo(out GraphUILog log) {
			if (_pointer > 0) {
				log = _logs[_logs.Count-_pointer];
				_pointer--;
				return true;
			} else {
				log = null;
				return false;
			}
		}
	}
}