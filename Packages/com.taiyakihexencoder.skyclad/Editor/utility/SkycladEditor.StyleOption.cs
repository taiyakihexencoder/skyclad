using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace skyclad.editor {
	public static partial class SkycladEditor {
		public static StyleOption Modifier => new CustomStyleOption();

		public abstract class StyleOption {
			internal GUIContent label = null;
			internal GUILayoutOption width = null;
			internal bool expandWidth = false;

			internal StyleOption(){ }

			public StyleOption FitLabel(GUIContent content) {
				return Width(EditorStyles.label.CalcSize(content).x + EditorGUI.indentLevel * 15.0f);
			}

			public StyleOption Label(string label) {
				this.label = new GUIContent(label);
				return this;
			}

			public StyleOption Width(float width) {
				this.width = GUILayout.Width(width);
				return this;
			}

			public StyleOption ExpandWidth {
				get {
					expandWidth = true;
					return this;
				}
			}

			internal GUILayoutOption[] LayoutOptions {
				get {
					List<GUILayoutOption> layoutOption = new List<GUILayoutOption>();
					if (width != null) {
						layoutOption.Add(width);
					}
					if (expandWidth) {
						layoutOption.Add(GUILayout.ExpandWidth(true));
					}
					return layoutOption.ToArray();
				}
			}
		}

		private class CustomStyleOption : StyleOption { }
	}
}