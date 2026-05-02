using UnityEditor;
using UnityEngine;

namespace skyclad.editor {
	/// <summary>
	/// CreateGraphでグラフビューを表示する。
	/// IGraphSettingsでグラフの機能をカスタムする。
	/// </summary>
	public abstract class BaseNodeEditorKitWindow : _BaseNodeEditorKitWindow {
		protected abstract GraphUI.IGraphSettings Settings { get; }

		[SerializeField]
		private GraphUIData _data = null;

		protected static void OpenWindow<T>() where T : BaseNodeEditorKitWindow {
			T window = GetWindow<T>();
			window._data = null;
		}

		internal static void OpenWindow(System.Type type, GraphUIData data = null) {
			EditorWindow window = GetWindow(type);
			if (window is BaseNodeEditorKitWindow kit) {
				kit._data = data;
			}
		}

		protected sealed override void OnEnable() {
			// OnEnableがGetWindowの瞬間に呼ばれるため、
			// _dataがセットされてからレイアウトするために、delayCallを用いる。
			EditorApplication.delayCall += Layout;
		}

		protected abstract void Layout();

		protected GraphUI CreateGraph() {
			return new GraphUI(_data, Settings);
		}
	}

	public abstract class _BaseNodeEditorKitWindow : EditorWindow {
		protected virtual void OnEnable() { }
	}
}
