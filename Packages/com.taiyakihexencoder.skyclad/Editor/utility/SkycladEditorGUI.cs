using UnityEditor;
using UnityEngine;

namespace skyclad.editor {
	public static class SkycladEditorGUI {
		public static class Layout {
			public enum Alignment { Left, Right, Center, }

			private class BoxScope : System.IDisposable {
				public BoxScope(RectOffset offset) {
					EditorGUILayout.BeginVertical(new GUIStyle { 
						padding = offset,
						stretchWidth = false,
					});
				}
				void System.IDisposable.Dispose() { EditorGUILayout.EndVertical(); }
			}

			private class HorizontalLayoutScope : System.IDisposable {
				public HorizontalLayoutScope() { EditorGUILayout.BeginHorizontal(); }
				void System.IDisposable.Dispose(){ EditorGUILayout.EndHorizontal(); }
			}

			private class VerticalLayoutScope : System.IDisposable {
				public VerticalLayoutScope() { EditorGUILayout.BeginHorizontal(); }
				void System.IDisposable.Dispose(){ EditorGUILayout.EndHorizontal(); }
			}

			public static System.IDisposable Horizontal => new HorizontalLayoutScope();
			public static System.IDisposable Vertical => new VerticalLayoutScope();
			public static System.IDisposable Box(RectOffset padding = null) => new BoxScope(padding ?? new RectOffset());
			public static void Space(int width = 0, int height = 0) {
				using(Box(new RectOffset(width, 0, height, 0))){}
			}

			public static void Label(string text, float? width = null) {
				GUIContent content = new GUIContent(text);
				EditorGUILayout.LabelField(content, GUILayout.Width(width ?? EditorStyles.label.CalcSize(content).x + EditorGUI.indentLevel * 15.0f));
			}

			public static string TextField(string text, float? width = null) {
				if (width == null) {
					return EditorGUILayout.TextField(text);
				} else {
					return EditorGUILayout.DelayedTextField(text, GUILayout.Width(width.Value));
				}
			}

			public static string TextField(string text, string label, float? width = null) {
				if (width == null) {
					return EditorGUILayout.TextField(label, text);
				} else {
					return EditorGUILayout.DelayedTextField(label, text, GUILayout.Width(width.Value));
				}
			}

			public static void TextField(SerializedProperty property, float? width = null) {
				property.stringValue = TextField(property.stringValue, width);
			}

			public static void TextField(SerializedProperty property, string label, float? width = null) {
				property.stringValue = TextField(property.stringValue, label, width);
			}

			public static bool Toggle(bool value, float? width = null, Alignment alignment = Alignment.Center) {
				return Toggle(value, "", width, alignment);
			}

			public static bool Toggle(bool value, string label, float? width = null, Alignment alignment = Alignment.Left) {
				if (width == null) {
					EditorGUILayout.BeginHorizontal();
				} else {
					EditorGUILayout.BeginHorizontal(GUILayout.Width(width.Value));
				}

				if (alignment == Alignment.Center || alignment == Alignment.Right) {
					GUILayout.FlexibleSpace();
				}

				bool result = EditorGUILayout.Toggle("", value, GUILayout.Width(12f));
				// emptyでもレイアウト調整で表示したほうがいい
				Label(label ?? "");

				if (alignment == Alignment.Center || alignment == Alignment.Left) {
					GUILayout.FlexibleSpace();
				}
				EditorGUILayout.EndHorizontal();
				return result;
			}

			public static int Toolbar(int selected, params string[] tabs) {
				using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar)) {
					return GUILayout.Toolbar(
						selected,
						tabs,
						new GUIStyle(EditorStyles.toolbarButton),
						GUI.ToolbarButtonSize.FitToContents
					);
				}
			}

			public static int Toolbar(
				int selected, 
				ref float scroll, 
				System.Action repaint,
				params string[] tabs
			) {
				using (
					new EditorGUILayout.ScrollViewScope(
						new Vector2(scroll, 0.0f),
						GUIStyle.none,
						GUIStyle.none,
						GUILayout.ExpandHeight(false)
					)
				) {
					int index = selected;
					using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar)) {
						index = GUILayout.Toolbar(
							selected,
							tabs,
							new GUIStyle(EditorStyles.toolbarButton),
							GUI.ToolbarButtonSize.FitToContents
						);
					}

					Rect rect = GUILayoutUtility.GetLastRect();

					Event evt = Event.current;
					if (evt.type == EventType.ScrollWheel &&
						rect.Contains(evt.mousePosition)
					) {
						scroll += evt.delta.y * 20.0f;
						EditorApplication.delayCall += () => {
							repaint();
						};
					}
					return index;
				}
			}

			public static bool PlusButton(float? width = null) {
				GUIContent content = EditorGUIUtility.IconContent("Toolbar Plus");
				return GUILayout.Button(
					content,
					GUILayout.Width(width ?? CalculateButtonContentSize(content))
				);
			}

			public static bool MinusButton(float? width = null) {
				GUIContent content = EditorGUIUtility.IconContent("Toolbar Minus");
				return GUILayout.Button(
					content,
					GUILayout.Width(width ?? CalculateButtonContentSize(content))
				);
			}

			public static bool Button(string text, float? width = null) {
				GUIContent content = new GUIContent(text);
				return GUILayout.Button(
					content,
					GUILayout.Width(width ?? CalculateButtonContentSize(content))
				);
			}

			private static float CalculateButtonContentSize(GUIContent content) {
				return EditorStyles.label.CalcSize(content).x + EditorGUI.indentLevel * 15.0f + 16.0f;
			}
		}
	}
}