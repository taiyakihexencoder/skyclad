using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace skyclad.editor {
	internal sealed class GlobalSettingsProvider : SettingsProvider {
		private SerializedObject serializedObject;
		private FieldInfo[] layerFieldInfo;

		internal GlobalSettingsProvider(
			string path,
			SettingsScope scopes,
			IEnumerable<string> keywords = null
		) : base(path, scopes, keywords) {
			serializedObject = new SerializedObject(SkycladProjectSettings.Instance);
			layerFieldInfo = typeof(Layer).GetFields(BindingFlags.Static | BindingFlags.Public);
		}

		[SettingsProvider]
		public static SettingsProvider CreateSettingsProvider() {
			return new GlobalSettingsProvider(
				path: "Skyclad/Global",
				scopes: SettingsScope.Project,
				keywords: new string[] { "skyclad", }
			);
		}

		public override void OnGUI(string searchContext) {
			serializedObject.Update();

			using (serializedObject.ChangeCheckScope()) {
				using (SkycladEditor.GUI.Layout.Box(new RectOffset(20, 20, 20, 20))) {
					LayersGUI();
				}
			}
		}

		private void LayersGUI() {
			SerializedProperty layersProperty = serializedObject.FindProperty("_global._colliderLayers");
			SerializedProperty collidesProperty = serializedObject.FindProperty("_global._collides");

			using (SkycladEditor.GUI.Layout.Horizontal) {
				SkycladEditor.GUI.Layout.Label("DataTable");
				SkycladEditor.GUI.Layout.Space(width: 30);
				if (SkycladEditor.GUI.Layout.Button("Update Script")) {
					DataTableLoaderGenerator.Generate();
				}
			}

			SkycladEditor.GUI.Layout.Space(height: 32);

			using (SkycladEditor.GUI.Layout.Horizontal) {
				SkycladEditor.GUI.Layout.Label("Layers");
				SkycladEditor.GUI.Layout.Space(width: 30);
				if (SkycladEditor.GUI.Layout.Button("Update Script")) {
					GenerateLayerScript();
				}
			}

			List<uint> notChangeList = new List<uint>(GlobalProjectSettings.LAYER_DEFAULT);
			List<uint> notChangeCollisionList = new List<uint>(GlobalProjectSettings.COLLISION_DEFAULT);

			if (layersProperty.arraySize != 32) {
				layersProperty.arraySize = 32;
			}

			if (collidesProperty.arraySize != 32*32) {
				collidesProperty.arraySize = 32*32;
			}

			for (int i = 0; i < 32; ++i) {
				int index = notChangeList.FindIndex(_ => _ == (uint)1 << i);

				if (index >= 0) {
					// 名前を固定
					foreach(FieldInfo field in layerFieldInfo) {
						if (field.FieldType == typeof(uint) && field.IsLiteral && (uint)field.GetValue(null) == notChangeList[index]) {
							layersProperty.Of(i).stringValue = field.Name;
						}
					}

					// 衝突関係を固定
					uint flags = notChangeCollisionList[index];
					for (int j = 0; j < 32; ++j) {
						collidesProperty.Of(i * 32 + j).boolValue = (flags & ((uint)1 << j)) != 0;
					}
				}
			}

			GUIStyle evenNumberLabelStyle = new GUIStyle(GUI.skin.label);
			evenNumberLabelStyle.normal.textColor = new Color(0.5f,1.0f,0.5f,1f);
			evenNumberLabelStyle.fontSize = 9;
			GUIStyle oddsNumberLabelStyle = new GUIStyle(GUI.skin.label);
			oddsNumberLabelStyle.normal.textColor = new Color(0.5f,0.5f,1.0f,1f);
			oddsNumberLabelStyle.fontSize = 9;
			using (SkycladEditor.GUI.Layout.Horizontal) {
				SkycladEditor.GUI.Layout.Label("", SkycladEditor.Modifier.Width(24));
				SkycladEditor.GUI.Layout.Label("Name", SkycladEditor.Modifier.Width(180));
				for(int i = 0; i < 32; ++i) {
					SerializedProperty layerProperty = layersProperty.Of(i);
					SkycladEditor.GUI.Layout.Label(
						i.ToString("00"), 
						layerProperty.stringValue,
						i % 2 == 0 ? evenNumberLabelStyle : oddsNumberLabelStyle,
						SkycladEditor.Modifier.Width(13)
					);
				}
			}

			for (int i = 0; i < 32; ++i) {
				using (SkycladEditor.GUI.Layout.Horizontal) {
					SkycladEditor.GUI.Layout.Label(
						i.ToString("00"), 
						i % 2 == 0 ? evenNumberLabelStyle : oddsNumberLabelStyle,
						SkycladEditor.Modifier.Width(24)
					);

					SerializedProperty layerProperty = layersProperty.Of(i);
					layerProperty.stringValue = SkycladEditor.GUI.Layout.TextField(layerProperty.stringValue, SkycladEditor.Modifier.Width(180.0f));

					string name = layerProperty.stringValue;

					using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(name) || notChangeList.Contains((uint)(1 << i)))) {
						for (int n = 0; n <= i; ++n) {
							string name2 = layersProperty.Of(n).stringValue;
							using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(name2) || notChangeList.Contains((uint)(1 << n)))) {
								SerializedProperty collideProperty = collidesProperty.Of(i * 32 + n);
								collideProperty.boolValue = SkycladEditor.GUI.Layout.Toggle(
									collideProperty.boolValue
								);
								collidesProperty.Of(i + n * 32).boolValue = collideProperty.boolValue;
							}
						}
					}
				}
			}
			
			using (SkycladEditor.GUI.Layout.Horizontal) {
				SkycladEditor.GUI.Layout.Space(width: 208);
				for(int i = 0; i < 32; ++i) {
					SerializedProperty layerProperty = layersProperty.Of(i);
					SkycladEditor.GUI.Layout.Label(
						i.ToString("00"), 
						layerProperty.stringValue,
						i % 2 == 0 ? evenNumberLabelStyle : oddsNumberLabelStyle,
						SkycladEditor.Modifier.Width(13)
					);
				}
			}
		}

		private void GenerateLayerScript() {
			SerializedProperty layersProperty = serializedObject.FindProperty("_global._colliderLayers");
			SerializedProperty collidesProperty = serializedObject.FindProperty("_global._collides");

			SourceCodeGenerator gen = new SourceCodeGenerator();

			List<uint> defaultLayerList = new List<uint>(GlobalProjectSettings.LAYER_DEFAULT);
			gen.AppendLine($"namespace skyclad {{");

			gen.AddIndent(); {
				gen.AppendLine($"public static partial class Layer {{");

				gen.AddIndent(); {
					for(int i = 0; i < layersProperty.arraySize; ++i) {
						SerializedProperty layerProperty = layersProperty.Of(i);
						string name = layerProperty.stringValue;
						if (!string.IsNullOrEmpty(name) && !defaultLayerList.Contains((uint)1 << i)) {
							gen.AppendLine($"public const uint {name} = {((uint)1 << i)}u;");
						}
					}

					gen.AppendLine($"");
					gen.AppendLine($"// - Collides - //");
					gen.AppendLine($"public static partial class CollidesWith {{");

					for (int i = 0; i < layersProperty.arraySize; ++i) {
						SerializedProperty layerProperty = layersProperty.Of(i);
						string name = layerProperty.stringValue;

						if (!string.IsNullOrEmpty(name) && !defaultLayerList.Contains((uint)1 << i)) {
							List<string> collides = new List<string>();
							for (int n = 0; n < 32; ++n) {
								if (collidesProperty.Of(i * 32 + n).boolValue) {
									string name2 = layersProperty.Of(n).stringValue;
									if (!string.IsNullOrEmpty(name2)) {
										collides.Add(name2);
									}
								}
							}
							gen.AddIndent(); {
								if (!string.IsNullOrEmpty(name)) {
									gen.AppendLine($"public const uint {name} = {(collides.Count > 0 ? "Layer."+string.Join(" | Layer.", collides) : "0u")};");
								}
							} gen.RemoveIndent();
						}

					}
					gen.AppendLine($"}}");	
				} gen.RemoveIndent();
				gen.AppendLine($"}}");
			} gen.RemoveIndent();

			gen.AppendLine($"}}");
			gen.Generate($"global{Path.DirectorySeparatorChar}Layer.cs");
		}
	}
}