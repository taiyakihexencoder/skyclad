using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace skyclad.lunarscape.editor {
	internal sealed class LunarscapeEditorWindow : EditorWindow {
		private const float MENU_WIDTH = 120f;

		[SerializeField]
		private Tabs _current = Tabs.None;

		private enum Tabs {
			None,
			General,
			Field,
			Layer,
		}

		[MenuItem("Skyclad/Lunarscape")]
		private static void OpenWindow() {
			LunarscapeEditorWindow window = GetWindow<LunarscapeEditorWindow>("Lunarscape");
		}

		private void OnEnable() {
			CreateBaseLayout();
		}

		private void CreateBaseLayout() {
			TwoPaneSplitView baseView = new TwoPaneSplitView(
				fixedPaneIndex: 0,
				fixedPaneStartDimension: MENU_WIDTH,
				orientation: TwoPaneSplitViewOrientation.Horizontal
			);

			VisualElement contentRoot = new VisualElement();
			contentRoot.Add(CreateContentView());
			System.Action repaint = () => {
				contentRoot.Clear();
				contentRoot.Add(CreateContentView());
			};

			baseView.Add(CreateListView(repaint));
			baseView.Add(contentRoot);
			rootVisualElement.Add(baseView);
		}

		private VisualElement CreateListView(System.Action repaint) {
			ScrollView scrollView = new ScrollView(ScrollViewMode.Vertical);
			scrollView.style.flexDirection = FlexDirection.Column;

			foreach(Tabs tab in System.Enum.GetValues(typeof(Tabs))) {
				if (tab != Tabs.None) {
					Tabs targetTab = tab;
					VisualElement element = new VisualElement();
					element.userData = targetTab;
					element.style.width = new StyleLength(StyleKeyword.Auto);
					element.style.height = 40f;
					element.RegisterCallback<MouseDownEvent>(
						evt => {
							if (evt.button == 0) {
								_current = targetTab;
								OnListSelected(scrollView);
								repaint();
							}
						}
					);

					Label label = new Label(tab.ToString());
					label.style.flexGrow = 1f;
					label.style.unityTextAlign = TextAnchor.MiddleCenter;
					element.Add(label);
					scrollView.Add(element);
				}
			}

			return scrollView;
		}

		private void OnListSelected(VisualElement parent) {
			foreach (VisualElement ve in parent.Children()) {
				if (ve.userData is Tabs tab) {
					ve.style.backgroundColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f, tab == _current ? 1.0f : 0.0f));
				}
			}
		}

		private VisualElement CreateContentView() {
			switch(_current) {
				case Tabs.General: { return new LunarscapeEditorGeneralTab(); }
				case Tabs.Field: { return new LunarscapeEditorFieldTab(); }
				case Tabs.Layer: { return new LunarscapeEditorLayerTab(); }
				default: { return new VisualElement(); }
			}
		}
	}
}