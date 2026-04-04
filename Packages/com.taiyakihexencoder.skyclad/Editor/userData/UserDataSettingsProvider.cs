using System.Collections.Generic;
using UnityEditor;

namespace skyclad.editor {
	public class UserDataSettingsProvider : SettingsProvider {
		private SerializedObject serializedObject;
		private UserDataSettingsModel model;

		private int tabIndex;
		private static string[] tabs = new string[]{
			"all",
			"int",
			"bool",
			"float",
			"Vector2",
			"Vector3",
		};

		public UserDataSettingsProvider(
			string path, 
			SettingsScope scopes, 
			IEnumerable<string> keywords = null
		) : base(path, scopes, keywords) { 
			serializedObject = new SerializedObject(SkycladProjectSettings.Instance);

			model = new UserDataSettingsModel(serializedObject);
		}

		[SettingsProvider]
		public static SettingsProvider CreateSettingsProvider() {
			return new UserDataSettingsProvider(
				path: "Skyclad/User Data",
				scopes: SettingsScope.Project,
				keywords: new string[] { "skyclad", }
			);
		}

		public override void OnGUI(string searchContext){

			using (serializedObject.ChangeCheckScope()) {
				if (SkycladEditor.GUI.Layout.Button("Update Script")) {
					try {
						UserDataScriptGenerator.Generate(serializedObject);
					} catch (System.Exception e) {
						EditorUtility.DisplayDialog("Error", e.Message, "Ok");
						UnityEngine.Debug.LogError(e);
					}
				}

				tabIndex = SkycladEditor.GUI.Layout.Toolbar(tabIndex, tabs);

				switch(tabIndex) {
					case 0: {
						ParameterTableHeader();
						ParameterTable(model.IntProperties, p => p.intValue.ToString());
						ParameterTableToggle(model.BoolProperties);
						ParameterTable(model.FloatProperties, p => p.floatValue.ToString());
						ParameterTable(model.Vector2Properties, p => $"({p.vector2Value.x}, {p.vector2Value.y})");
						ParameterTable(model.Vector3Properties, p => $"({p.vector3Value.x}, {p.vector3Value.y}, {p.vector3Value.z})");
						break;
					}
					case 1: {
						ParameterView(
							model.IntProperties,
							(p) => {
								p.intValue = SkycladEditor.GUI.Layout.IntField(p.intValue, SkycladEditor.Modifier.Width(120.0f));
							}
						);
						break;
					}
					case 2: {
						ParameterView(
							model.BoolProperties,
							(p) => {
								p.boolValue = SkycladEditor.GUI.Layout.Toggle(p.boolValue, SkycladEditor.Modifier.Width(60.0f));
							}
						);
						break;
					}
					case 3: {
						ParameterView(
							model.FloatProperties,
							(p) => {
								p.floatValue = SkycladEditor.GUI.Layout.FloatField(p.floatValue, SkycladEditor.Modifier.Width(120.0f));
							}
						);
						break;
					}
					case 4: {
						ParameterView(
							model.Vector2Properties,
							(p) => {
								p.vector2Value = SkycladEditor.GUI.Layout.Vector2Field(p.vector2Value);
							}
						);
						break;
					}
					case 5: {
						ParameterView(
							model.Vector3Properties,
							(p) => {
								p.vector3Value = SkycladEditor.GUI.Layout.Vector3Field(p.vector3Value);
							}
						);
						break;
					}
				}
			}
		}

		private void ParameterTableHeader() {
			using (SkycladEditor.GUI.Layout.Horizontal) {
				SkycladEditor.GUI.Layout.Label("name", SkycladEditor.Modifier.Width(200.0f));
				SkycladEditor.GUI.Layout.Space(width:20);
				SkycladEditor.GUI.Layout.Label("default", SkycladEditor.Modifier.Width(200.0f));
			}
			SkycladEditor.GUI.Layout.Space(height: 12);
		}

		private void ParameterTable(
			SerializedProperty listProperty,
			System.Func<SerializedProperty, string> text
		) {
			for(int i = 0; i < listProperty.arraySize; ++i) {
				using (SkycladEditor.GUI.Layout.Horizontal) {
					SerializedProperty property = listProperty.Of(i);
					SkycladEditor.GUI.Layout.Label(
						property.Of("name").stringValue,
						SkycladEditor.Modifier
							.Width(200.0f)
					);

					SkycladEditor.GUI.Layout.Space(width:20);

					SkycladEditor.GUI.Layout.Label(
						text(property.Of("defaultValue")),
						SkycladEditor.Modifier
							.Width(200.0f)
					);
				}
			}
		}

		private void ParameterTableToggle(SerializedProperty listProperty) {
			for(int i = 0; i < listProperty.arraySize; ++i) {
				using (SkycladEditor.GUI.Layout.Horizontal) {
					SerializedProperty property = listProperty.Of(i);
					SkycladEditor.GUI.Layout.Label(
						property.Of("name").stringValue,
						SkycladEditor.Modifier
							.Width(200.0f)
					);

					SkycladEditor.GUI.Layout.Space(width:20);
			
					SkycladEditor.GUI.Layout.Toggle(property.Of("defaultValue").boolValue);
				}
			}
		}


		private void ParameterView(
			SerializedProperty listProperty,
			System.Action<SerializedProperty> edit
		) {
			using(SkycladEditor.GUI.Layout.Box(new UnityEngine.RectOffset(20, 20, 10, 10))) {
				for (int i = 0; i < listProperty.arraySize; ++i) {
					using(SkycladEditor.GUI.Layout.Horizontal) {
						SerializedProperty property = listProperty.Of(i);

						SkycladEditor.GUI.Layout.Label("name");
						property.Of("name").stringValue = SkycladEditor.GUI.Layout.TextField(
							property.Of("name").stringValue,
							SkycladEditor.Modifier
								.Width(200.0f)
						);
						SkycladEditor.GUI.Layout.Space(width:20);

						SkycladEditor.GUI.Layout.Label("default");
						SkycladEditor.GUI.Layout.Space(width:20);
						edit(property.Of("defaultValue"));
						if (SkycladEditor.GUI.Layout.MinusButton()) {
							listProperty.DeleteArrayElementAtIndex(i);
							break;
						}
					}

					SkycladEditor.GUI.Layout.Space(height: 16);
				}

				if (SkycladEditor.GUI.Layout.PlusButton()) {
					model.AddParameter(listProperty);
				}
			}
		}
	}
}