using UnityEditor;
using UnityEngine;

namespace skyclad.editor {
	public static partial class SkycladEditor {
		public static partial class GUI {
			public static class Layout {
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

				private class VerticalScrollScope : System.IDisposable {
					public VerticalScrollScope(ref float scroll) { scroll = EditorGUILayout.BeginScrollView(new Vector2(0.0f, scroll)).y; }
					void System.IDisposable.Dispose() { EditorGUILayout.EndScrollView(); }
				}

				public static System.IDisposable Horizontal => new HorizontalLayoutScope();
				public static System.IDisposable Vertical => new VerticalLayoutScope();
				public static System.IDisposable VerticalScroll(ref float scroll) => new VerticalScrollScope(ref scroll);
				public static System.IDisposable Box(RectOffset padding = null) => new BoxScope(padding ?? new RectOffset());
				public static void Space(int width = 0, int height = 0) {
					using(Box(new RectOffset(width, 0, height, 0))){}
				}

				public static bool Foldout(SerializedProperty property, string label) {
					property.isExpanded = EditorGUILayout.Foldout(property.isExpanded, new GUIContent(label));
					return property.isExpanded;
				}

				public static void Label(string text, StyleOption modifier = null) {
					EditorGUILayout.LabelField(
						new GUIContent(text),
						options: (modifier ?? Modifier.FitLabel(new GUIContent(text))).LayoutOptions
					);
				}

				public static int IntField(int value, StyleOption modifier = null) {
					return EditorGUILayout.IntField(
						value: value,
						label: modifier?.label ?? new GUIContent(""),
						options: modifier?.LayoutOptions
					);
				}

				public static float FloadField(float value, StyleOption modifier = null) {
					return EditorGUILayout.FloatField(
						value: value,
						label: modifier?.label ?? new GUIContent(""),
						options: modifier?.LayoutOptions
					);
				}

				public static bool Toggle(bool value, StyleOption modifier = null) {
					return EditorGUILayout.Toggle(
						value: value,
						label: modifier?.label ?? new GUIContent(""),
						options: modifier?.LayoutOptions
					);
				}

				/// <summary>
				/// int型Enumの場合のみ使える選択Field
				/// </summary>
				public static int EnumField<T>(int value, StyleOption modifier = null) where T : System.Enum {
					T selected = default;
					foreach(T e in System.Enum.GetValues(typeof(T))) {
						if (e.GetHashCode() == value) {
							selected = e;
						}
					}
					return EditorGUILayout.EnumPopup(
						selected: selected, 
						label: modifier?.label ?? new GUIContent(""),
						options: modifier?.LayoutOptions
					).GetHashCode();
				}

				public static int IntPopup(
					int selected,
					in int[] values,
					in string[] displayNames,
					StyleOption modifier = null
				) {
					GUIContent[] contents = new GUIContent[displayNames.Length];
					for(int i = 0; i < contents.Length; ++i) {
						contents[i] = new GUIContent(displayNames[i]);
					}
					return EditorGUILayout.IntPopup(
						label: modifier?.label ?? new GUIContent(""),
						selectedValue: selected,
						displayedOptions: contents,
						optionValues: values,
						options: modifier?.LayoutOptions
					);
				}

				public static Vector3 Vector2Field(Vector2 value, StyleOption modifier = null) {
					return EditorGUILayout.Vector2Field(
						value: value,
						label: modifier?.label ?? new GUIContent(""),
						options: modifier?.LayoutOptions
					);
				}

				public static Vector3 Vector3Field(Vector3 value, StyleOption modifier = null) {
					return EditorGUILayout.Vector3Field(
						value: value,
						label: modifier?.label ?? new GUIContent(""),
						options: modifier?.LayoutOptions
					);
				}

				public static string TextField(string text, StyleOption modifier = null) {
					return EditorGUILayout.TextField(
						text: text,
						label: modifier?.label ?? new GUIContent(""),
						options: modifier?.LayoutOptions
					);
				}

				public static int Toolbar(int selected, params string[] tabs) {
					using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar)) {
						return GUILayout.Toolbar(
							selected,
							tabs,
							new GUIStyle(EditorStyles.toolbarButton),
							UnityEngine.GUI.ToolbarButtonSize.FitToContents
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
								UnityEngine.GUI.ToolbarButtonSize.FitToContents
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
}