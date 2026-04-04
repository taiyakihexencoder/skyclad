using System.Collections.Generic;
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
					Label(text, null, UnityEngine.GUI.skin.label, modifier);
				}

				public static void Label(string text, string tooltip, StyleOption modifier = null) {
					Label(text, tooltip, UnityEngine.GUI.skin.label, modifier);
				}

				public static void Label(string text, GUIStyle style, StyleOption modifier = null) {
					Label(text, null, style, modifier);
				}

				public static void Label(string text, string tooltip, GUIStyle style, StyleOption modifier = null) {
					HorizontalAlignment(
						modifier, (modifier) => {
							bool fitWidth = modifier == null || (modifier.width == null && !modifier.expandWidth);
							StyleOption targetModifier = (modifier ?? Modifier).Copy();
							if (fitWidth) {
								targetModifier.FitLabel(new GUIContent(text));
							}
							EditorGUILayout.LabelField(
								new GUIContent(text, tooltip),
								style,
								options: targetModifier.LayoutOptions
							);
						}
					);
				}

				public static int IntField(int value, StyleOption modifier = null) {
					return EditorGUILayout.IntField(
						value: value,
						label: modifier?.label ?? new GUIContent(""),
						options: modifier?.LayoutOptions
					);
				}

				public static float FloatField(float value, StyleOption modifier = null) {
					return EditorGUILayout.FloatField(
						value: value,
						label: modifier?.label ?? new GUIContent(""),
						options: modifier?.LayoutOptions
					);
				}

				public static bool Toggle(bool value, StyleOption modifier = null) {
					List<GUILayoutOption> options = new List<GUILayoutOption>(modifier?.LayoutOptions ?? new GUILayoutOption[0]){
						GUILayout.Width(8.0f),
					};
					GUIStyle style = new GUIStyle(UnityEngine.GUI.skin.toggle);
					style.padding = new RectOffset(0,0,0,0);
					style.margin = new RectOffset(0,0,0,0);
					return HorizontalAlignment(
						value, modifier, (modifier) => {
							bool result = EditorGUILayout.Toggle(
								label: "",
								value: value,
								options: options.ToArray()
							);

							Label(modifier?.label?.text ?? "");

							return result;
						}
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

				private static T HorizontalAlignment<T>(T value, StyleOption modifier, System.Func<StyleOption, T> layout) {
					if (modifier != null && modifier.horizontalAlignment != StyleOption.HorizontalAlignment.None) {
						T result;
						EditorGUILayout.BeginHorizontal(modifier.LayoutOptions);
						if (modifier.horizontalAlignment == StyleOption.HorizontalAlignment.Right || modifier.horizontalAlignment == StyleOption.HorizontalAlignment.Center) {
							GUILayout.FlexibleSpace();
						}

						result = layout(modifier.IgnoreLayout());

						if (modifier.horizontalAlignment == StyleOption.HorizontalAlignment.Left || modifier.horizontalAlignment == StyleOption.HorizontalAlignment.Center) {
							GUILayout.FlexibleSpace();
						}
						EditorGUILayout.EndHorizontal();
						return result;
					} else {
						return layout(modifier);
					}
				}

				private static void HorizontalAlignment(StyleOption modifier, System.Action<StyleOption> layout) {
					if (modifier != null && modifier.horizontalAlignment != StyleOption.HorizontalAlignment.None) {
						EditorGUILayout.BeginHorizontal(modifier.LayoutOptions);
						if (modifier.horizontalAlignment == StyleOption.HorizontalAlignment.Right || modifier.horizontalAlignment == StyleOption.HorizontalAlignment.Center) {
							GUILayout.FlexibleSpace();
						}

						layout(modifier.IgnoreLayout());

						if (modifier.horizontalAlignment == StyleOption.HorizontalAlignment.Left || modifier.horizontalAlignment == StyleOption.HorizontalAlignment.Center) {
							GUILayout.FlexibleSpace();
						}
						EditorGUILayout.EndHorizontal();
					} else {
						layout(modifier);
					}
				}

			}
		}
	}
}