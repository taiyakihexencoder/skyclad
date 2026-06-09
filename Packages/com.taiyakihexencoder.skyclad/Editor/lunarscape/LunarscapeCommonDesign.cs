using UnityEngine;
using UnityEngine.UIElements;

namespace skyclad.lunarscape.editor {
	internal static class LunarscapeCommonDesign {
		public static VisualElement Title(string text) {
			Label label = new Label(text);
			label.style.fontSize = 20f;
			label.style.unityFontStyleAndWeight = FontStyle.Bold;
			label.style.paddingBottom = 12f;
			return label;
		}

		public static VisualElement Header(string text) {
			Label label = new Label(text);
			label.style.fontSize = 16f;
			label.style.unityFontStyleAndWeight = FontStyle.Bold;
			label.style.paddingBottom = 8f;
			return label;
		}

		public static VisualElement MainFrame() {
			ScrollView mainFrame = new ScrollView(ScrollViewMode.Vertical);
			mainFrame.style.paddingLeft = 16f;
			mainFrame.style.paddingRight = 16f;
			mainFrame.style.paddingTop = 12f;
			mainFrame.style.paddingBottom = 12f;
			mainFrame.style.flexDirection = FlexDirection.Column;
			return mainFrame;
		}
	}
}