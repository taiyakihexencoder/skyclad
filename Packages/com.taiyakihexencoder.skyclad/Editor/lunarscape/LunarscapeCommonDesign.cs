using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace skyclad.lunarscape.editor {
	public static class LunarscapeCommonDesign {
		public static VisualElement Title(string text) {
			Label label = new Label(text);
			label.style.fontSize = 20f;
			label.style.unityFontStyleAndWeight = FontStyle.Bold;
			return label;
		}

		public static VisualElement MainFrame() {
			VisualElement mainFrame = new VisualElement();
			mainFrame.style.paddingLeft = 16f;
			mainFrame.style.paddingRight = 16f;
			mainFrame.style.paddingTop = 12f;
			mainFrame.style.paddingBottom = 12f;
			return mainFrame;
		}
	}
}