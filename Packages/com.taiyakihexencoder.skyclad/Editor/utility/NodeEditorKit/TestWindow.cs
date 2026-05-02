using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace skyclad.editor {
	public sealed class TestWindow : BaseNodeEditorKitWindow {
		protected override GraphUI.IGraphSettings Settings => new GraphSettings();

		[MenuItem("Skyclad/Test Window")]
		public static void Open() {
			OpenWindow<TestWindow>();
		}

		protected override void Layout() {
			GraphUI graph = CreateGraph();
			graph.Floating.Add(new Button(() => {}){ text = "Test"});
			rootVisualElement.Add(graph);
		}

		private class GraphSettings : GraphUI.IGraphSettings {
			bool GraphUI.IGraphSettings.UseControlNode => true;
			bool GraphUI.IGraphSettings.EntryPoint => true;
			System.Type GraphUI.IGraphSettings.EditorWindowType => typeof(TestWindow);

			Dictionary<string, int> GraphUI.IGraphSettings.Entries => new Dictionary<string, int>();

			bool GraphUI.IGraphSettings.TryCreateNode(string guid, int type, out BaseNode node) {
				node = null;
				return false;
			}
		}
	}
}
