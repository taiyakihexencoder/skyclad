using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace skyclad.editor {
	internal sealed class CharacterSettingsProvider : SettingsProvider {
		private SerializedObject serializedObject;

		// キャラクタータブ管理
		private int _tabIndex = 0;
		private float _tabScroll = 0.0f;

		// コントロールタブ管理
		private int _controlTabIndex = 0;
		private float _controlTabScroll = 0.0f;

		private const int GENERAL_TAB = 0;

		private List<string> colliderGuidList;
		private List<string> colliderNameList;

		private List<string> controllerGuidList;
		private List<string> controllerNameList;

		private List<int> layerValueList;
		private List<string> layerNameList;

		internal CharacterSettingsProvider(
			string path,
			SettingsScope scopes,
			IEnumerable<string> keywords = null
		) : base(path, scopes, keywords) {
			serializedObject = new SerializedObject(SkycladProjectSettings.Instance);

			colliderGuidList = new List<string>();
			colliderNameList = new List<string>();

			controllerGuidList = new List<string>();
			controllerNameList = new List<string>();

			layerValueList = new List<int>();
			layerNameList = new List<string>();
			foreach(FieldInfo field in typeof(Layer).GetFields(BindingFlags.Static | BindingFlags.Public)) {
				if (field.IsLiteral && field.FieldType == typeof(uint)) {
					layerValueList.Add((int)(uint)field.GetValue(null));
					layerNameList.Add(field.Name);
				}
			}
		}

		[SettingsProvider]
		public static SettingsProvider CreateSettingsProvider() {
			return new CharacterSettingsProvider(
				path: "Skyclad/Character", 
				scopes: SettingsScope.Project,
				keywords: new string[] { "skyclad", }
			);
		}

		public override void OnGUI(string searchContext) {
			serializedObject.Update();

			CharacterToolbar();

			using (serializedObject.ChangeCheckScope()) {
				using (SkycladEditorGUI.Layout.Box(new RectOffset(20, 20, 20, 20))) {
					if (_tabIndex == GENERAL_TAB) {
						GeneralView();
					} else {
						SerializedProperty unitsProperty = serializedObject.FindProperty("_character._units");
						for (int i = 0, n = 1; i < unitsProperty.arraySize; ++i) {
							SerializedProperty unitProperty = unitsProperty.Of(i);
							if (!string.IsNullOrEmpty(unitProperty.Of("name").stringValue)) {
								if (_tabIndex == n) {
									CharacterContentView(unitProperty);
									break;
								} else {
									n++;
								}
							}
						}
					}
				}
			}
		}

		private void CharacterToolbar() {
			List<string> tabContents = new List<string>();
			tabContents.Add("General");

			SerializedProperty unitsProperty = serializedObject.FindProperty("_character._units");
			for (int i = 0; i < unitsProperty.arraySize; ++i) {
				string name = unitsProperty.Of(i).Of("name").stringValue;
				if (!string.IsNullOrEmpty(name)) {
					tabContents.Add(name);
				}
			}
			Toolbar(ref _tabIndex, ref _tabScroll, tabContents.ToArray());
		}

		private void ControlToolbar() {
			List<string> tabContents = new List<string>();
			tabContents.Add("General");

			SerializedProperty controllersProperty = serializedObject.FindProperty("_character._controllers");
			for (int i = 0; i < controllersProperty.arraySize; ++i) {
				string name = controllersProperty.Of(i).Of("name").stringValue;
				if (!string.IsNullOrEmpty(name)) {
					tabContents.Add(name);
				}
			}

			Toolbar(ref _controlTabIndex, ref _controlTabScroll, tabContents.ToArray());
		}

		private void Toolbar(ref int tabIndex, ref float tabScroll, in string[] contents) {
			tabIndex = SkycladEditorGUI.Layout.Toolbar(tabIndex, ref tabScroll, Repaint, contents);
		}

		private void ParameterDefsEditView(SerializedProperty listProperty) {
			using (SkycladEditorGUI.Layout.Horizontal) {
				SkycladEditorGUI.Layout.Label("Type", 100.0f);
				SkycladEditorGUI.Layout.Label("Name", 160.0f);
			}
			for (int i = 0; i < listProperty.arraySize; ++i) {
				SerializedProperty property = listProperty.Of(i);
				SerializedProperty statusTypeProperty = property.Of("parameterType");
				SerializedProperty nameProperty = property.Of("name");

				using (SkycladEditorGUI.Layout.Horizontal) {
					EditorGUILayout.PropertyField(statusTypeProperty, new GUIContent(""), GUILayout.Width(100.0f));

					SkycladEditorGUI.Layout.TextField(nameProperty, 160.0f);
					if (SkycladEditorGUI.Layout.MinusButton()) {
						listProperty.Delete(i);
						break;
					}
				}
			}
			if (SkycladEditorGUI.Layout.PlusButton()) {
				listProperty.Add(p => {
					p.Of("parameterType").intValue = (int)CharacterProjectSettings.ParameterType.Int;
					p.Of("name").stringValue = "";
				});
			}
		}

		private void GeneralView() {
			SerializedProperty characterProperty = serializedObject.FindProperty("_character");

			using (SkycladEditorGUI.Layout.Horizontal) {
				using (SkycladEditorGUI.Layout.Box()) {
					SerializedProperty parameterUnitsProperty = characterProperty.Of("_statusParameterUnits");

					using (SkycladEditorGUI.Layout.Horizontal) {
						SkycladEditorGUI.Layout.Label("Parameters");
						SkycladEditorGUI.Layout.Space(width: 30);
						if (SkycladEditorGUI.Layout.Button("Update Script")) {
							CharacterStatusScriptGenerator.Generate(serializedObject);
						}
						SkycladEditorGUI.Layout.Space(width: 30);
						if (SkycladEditorGUI.Layout.Button("Update Binary")) {
							CharacterStatusTableBinaryGenerator.Generate(serializedObject, "characterStatus.bytes");
						}
					}
					SkycladEditorGUI.Layout.Space(height: 10);

					EditorGUI.indentLevel++; {
						ParameterDefsEditView(parameterUnitsProperty);
					} EditorGUI.indentLevel--;

					SkycladEditorGUI.Layout.Space(height: 50);

					using (SkycladEditorGUI.Layout.Horizontal) {
						SkycladEditorGUI.Layout.Label("Colliders");
						SkycladEditorGUI.Layout.Space(width: 30);
						if (SkycladEditorGUI.Layout.Button("Update Scripts")) {
							CharacterColliderScriptGenerator.Generate(serializedObject);
						}
					}

					SkycladEditorGUI.Layout.Space(height: 10);

					SerializedProperty colliderUnitsProperty = characterProperty.Of("_colliderUnits");
					EditorGUI.indentLevel++; {
						using (SkycladEditorGUI.Layout.Horizontal) {
							SkycladEditorGUI.Layout.Label("name", 120.0f);
							SkycladEditorGUI.Layout.Label("radius", 60.0f);
							SkycladEditorGUI.Layout.Label("height", 60.0f);
						}

						for (int i = 0; i < colliderUnitsProperty.arraySize; ++i) {
							using (SkycladEditorGUI.Layout.Horizontal) {
								SerializedProperty colliderUnitProperty = colliderUnitsProperty.Of(i);
								SkycladEditorGUI.Layout.TextField(colliderUnitProperty.Of("name"), 120.0f);

								SerializedProperty radiusProperty = colliderUnitProperty.Of("radius");
								radiusProperty.floatValue = EditorGUILayout.DelayedFloatField(radiusProperty.floatValue, GUILayout.Width(60.0f));
								
								SerializedProperty heightProperty = colliderUnitProperty.Of("height");
								heightProperty.floatValue = EditorGUILayout.DelayedFloatField(heightProperty.floatValue, GUILayout.Width(60.0f));
								
								if (SkycladEditorGUI.Layout.MinusButton()) {
									colliderUnitsProperty.Delete(i);
									break;
								}
							}
						}

						if (SkycladEditorGUI.Layout.PlusButton()) {
							colliderUnitsProperty.Add((p) => {
								p.Of("guid").stringValue = System.Guid.NewGuid().ToString();
								p.Of("name").stringValue = "";
								p.Of("radius").floatValue = 0.0f;
								p.Of("height").floatValue = 0.0f;
							});
						}

						colliderGuidList.Clear();
						colliderNameList.Clear();
						for (int i = 0; i < colliderUnitsProperty.arraySize; ++i) {
							SerializedProperty colliderProperty = colliderUnitsProperty.Of(i);
							string guid = colliderProperty.Of("guid").stringValue;
							string name = colliderProperty.Of("name").stringValue;
							if (!string.IsNullOrEmpty(name)) {
								colliderGuidList.Add(guid);
								colliderNameList.Add(name);
							}
						}
					} EditorGUI.indentLevel--;
				}
			
				SkycladEditorGUI.Layout.Space(width: 60);

				using (SkycladEditorGUI.Layout.Box()) {
					using (SkycladEditorGUI.Layout.Horizontal) {
						EditorGUILayout.LabelField("Characters");
						if (SkycladEditorGUI.Layout.Button("Update Scripts")) {
							CharacterIdScriptGenerator.Generate(serializedObject);
						}
					}

					EditorGUI.indentLevel++; {
						SerializedProperty unitsProperty = characterProperty.Of("_units");
						using (SkycladEditorGUI.Layout.Horizontal) {
							SkycladEditorGUI.Layout.Label("Name", 160.0f);
							SkycladEditorGUI.Layout.Label("Collide", 60.0f);
							SkycladEditorGUI.Layout.Label("Control", 60.0f);
							SkycladEditorGUI.Layout.Label("Status", 60.0f);
						}
						for (int i = 0; i < unitsProperty.arraySize; ++i) {
							SerializedProperty unitProperty = unitsProperty.Of(i);
							SerializedProperty hasColliderProperty = unitProperty.Of("type.hasCollider");
							SerializedProperty hasControllerProperty = unitProperty.Of("type.hasController");
							SerializedProperty hasStatusProperty = unitProperty.Of("type.hasStatus");
							using (SkycladEditorGUI.Layout.Horizontal) {
								SkycladEditorGUI.Layout.TextField(unitProperty.Of("name"), 160.0f);
								hasColliderProperty.boolValue = EditorGUILayout.Toggle(hasColliderProperty.boolValue, GUILayout.Width(60.0f));
								hasControllerProperty.boolValue = EditorGUILayout.Toggle(hasControllerProperty.boolValue, GUILayout.Width(60.0f));
								hasStatusProperty.boolValue = EditorGUILayout.Toggle(hasStatusProperty.boolValue, GUILayout.Width(60.0f));
								if (SkycladEditorGUI.Layout.MinusButton()) {
									unitsProperty.Delete(i);
									break;
								}
							}
						}
						if (SkycladEditorGUI.Layout.PlusButton()) {
							unitsProperty.Add(p => {
								p.Of("guid").stringValue = System.Guid.NewGuid().ToString();
								p.Of("name").stringValue = "";
								p.Of("isPlayerCharacter").boolValue = false;
								p.Of("colliderGuid").stringValue = "";
								p.Of("controllerGuid").stringValue = "";
							});
						}
					} EditorGUI.indentLevel--;
				}
			}

			SkycladEditorGUI.Layout.Space(height: 24);

			SkycladEditorGUI.Layout.Label("Character Controller");
			ControlToolbar();

			if (_controlTabIndex == GENERAL_TAB) {
				EditorGUI.indentLevel++; {
					CharacterControllerGeneralView();
				} EditorGUI.indentLevel--;
			} else {
				SerializedProperty controllersProperty = characterProperty.Of("_controllers");
				for (int i = 0, n = 1; i < controllersProperty.arraySize; ++i) {
					SerializedProperty controllerProperty = controllersProperty.Of(i);
					if (!string.IsNullOrEmpty(controllerProperty.Of("name").stringValue)) {
						if (_controlTabIndex == n) {
							CharacterControllerView(controllerProperty);
							break;
						} else {
							n++;
						}
					}
				}

			}
		}

		private void CharacterControllerGeneralView() {
			if (SkycladEditorGUI.Layout.Button("Update Scripts")) {
				CharacterControllerScriptGenerator.Generate(serializedObject);
			}

			SerializedProperty controllersProperty = serializedObject.FindProperty("_character._controllers");
			using (SkycladEditorGUI.Layout.Horizontal) {
				SkycladEditorGUI.Layout.Label("name", 160.0f);
			}

			for (int i = 0; i < controllersProperty.arraySize; ++i) {
				SerializedProperty controllerProperty = controllersProperty.Of(i);
				SerializedProperty nameProperty = controllerProperty.Of("name");

				using (SkycladEditorGUI.Layout.Horizontal) {
					SkycladEditorGUI.Layout.TextField(nameProperty, 160.0f);
					if (SkycladEditorGUI.Layout.MinusButton()) {
						controllersProperty.Delete(i);
						break;
					}
				}
			}
			if (SkycladEditorGUI.Layout.PlusButton()) {
				controllersProperty.Add(p => {
					p.Of("guid").stringValue = System.Guid.NewGuid().ToString();
					p.Of("name").stringValue = "";
					p.Of("_parameters").arraySize = 0;
				});
			}

			controllerGuidList.Clear();
			controllerNameList.Clear();
			for (int i = 0; i < controllersProperty.arraySize; ++i) {
				SerializedProperty controllerProperty = controllersProperty.Of(i);
				string guid = controllerProperty.Of("guid").stringValue;
				string name = controllerProperty.Of("name").stringValue;
				if (!string.IsNullOrEmpty(name)) {
					controllerGuidList.Add(guid);
					controllerNameList.Add(name);
				}
			}

		}

		private void CharacterControllerView(SerializedProperty controllerProperty) {
			SerializedProperty nameProperty = controllerProperty.Of("name");

			SkycladEditorGUI.Layout.TextField(nameProperty, "Name");

			ParameterDefsEditView(controllerProperty.Of("_parameters"));
		}

		private void CharacterContentView(SerializedProperty unitProperty) {
			using(SkycladEditorGUI.Layout.Horizontal) {
				SkycladEditorGUI.Layout.TextField(unitProperty.Of("name"), "Character Name");
			}

			SerializedProperty isPlayerCharacterProperty = unitProperty.Of("isPlayerCharacter");
			isPlayerCharacterProperty.boolValue = EditorGUILayout.Toggle("Player Character", isPlayerCharacterProperty.boolValue);

			if (unitProperty.Of("type.hasCollider").boolValue) {
				SerializedProperty colliderProperty = unitProperty.Of("colliderGuid");

				int selectedCollider = colliderGuidList.FindIndex(guid => colliderProperty.stringValue == guid);
				selectedCollider = EditorGUILayout.Popup(new GUIContent("Collider"), selectedCollider, colliderNameList.ToArray());
				if (0 <= selectedCollider && selectedCollider < colliderGuidList.Count) {
					colliderProperty.stringValue = colliderGuidList[selectedCollider];
				} else {
					colliderProperty.stringValue = "";
				}
			}

			if (unitProperty.Of("type.hasController").boolValue) {
				SerializedProperty controllerProperty = unitProperty.Of("controllerGuid");

				int selectedController = controllerGuidList.FindIndex(guid => controllerProperty.stringValue == guid);
				selectedController = EditorGUILayout.Popup(new GUIContent("Controller"), selectedController, controllerNameList.ToArray());
				if (0 <= selectedController && selectedController < controllerGuidList.Count) {
					controllerProperty.stringValue = controllerGuidList[selectedController];

					SerializedProperty controllersProperty = serializedObject.FindProperty("_character._controllers");
					for(int i = 0; i < controllersProperty.arraySize; ++i) {
						SerializedProperty controlProperty = controllersProperty.Of(i);
						if (controlProperty.Of("name").stringValue == controllerNameList[selectedController]) {
							using (SkycladEditorGUI.Layout.Box(new RectOffset(32, 32, 12, 0))) {
								ParameterInfoEditView(unitProperty.Of("controller"), controlProperty.Of("_parameters"));
							}
						}
					}

				} else {
					controllerProperty.stringValue = "";
				}
			}

			if (unitProperty.Of("type.hasStatus").boolValue) {
				SkycladEditorGUI.Layout.Label("Status");
				using (SkycladEditorGUI.Layout.Box(new RectOffset(32, 32, 12, 0))) {
					ParameterInfoEditView(
						unitProperty.Of("status"),
						serializedObject.FindProperty("_character._statusParameterUnits")
					);
				}
			}
		}

		private void ParameterInfoEditView(
			SerializedProperty property,
			SerializedProperty parametersProperty
		) {
			SerializedProperty namesProperty = property.Of("names");
			SerializedProperty valuesProperty = property.Of("values");

			Regex regexF2 = new Regex(@"\((?<x>[0-9.]+),(?<y>[0-9.]+)\)");
			Regex regexF3 = new Regex(@"\((?<x>[0-9.]+),(?<y>[0-9.]+),(?<z>[0-9.]+)\)");

			for(int i = 0; i < parametersProperty.arraySize; ++i) {
				SerializedProperty parameterProperty = parametersProperty.Of(i);
				string name = parameterProperty.Of("name").stringValue;

				int index = 0;
				for(; index < namesProperty.arraySize; ++index) {
					SerializedProperty nameProperty = namesProperty.Of(index);
					if (nameProperty.stringValue == name) {
						break;
					}
				}

				if (index == namesProperty.arraySize) {
					namesProperty.arraySize++;
					namesProperty.Of(index).stringValue = name;
					valuesProperty.arraySize++;
					valuesProperty.Of(index).stringValue = "";
				}

				switch((CharacterProjectSettings.ParameterType)parameterProperty.Of("parameterType").intValue) {
					case CharacterProjectSettings.ParameterType.Int: {
						int value = int.TryParse(valuesProperty.Of(index).stringValue, out int v) ? v : 0;
						value = EditorGUILayout.DelayedIntField(name, value);
						valuesProperty.Of(index).stringValue = value.ToString();
						break;
					}
					case CharacterProjectSettings.ParameterType.Bool: {
						bool value = bool.TryParse(valuesProperty.Of(index).stringValue, out bool v) ? v : false;
						value = EditorGUILayout.Toggle(name, value);
						valuesProperty.Of(index).stringValue = value.ToString();
						break;
					}
					case CharacterProjectSettings.ParameterType.Float: {
						float value = float.TryParse(valuesProperty.Of(index).stringValue, out float v) ? v : 0.0f;
						value = EditorGUILayout.DelayedFloatField(name, value);
						valuesProperty.Of(index).stringValue = value.ToString();
						break;
					}
					case CharacterProjectSettings.ParameterType.Float2: {
						Match match = regexF2.Match(valuesProperty.Of(index).stringValue);
						Vector2 value = match.Success 
							? new Vector2(
								float.Parse(match.Groups["x"].Captures[0].Value), 
								float.Parse(match.Groups["y"].Captures[0].Value)
							) : Vector2.zero;
						value = EditorGUILayout.Vector2Field(name, value);
						valuesProperty.Of(index).stringValue = $"({value.x},{value.y})";
						break;
					}
					case CharacterProjectSettings.ParameterType.Float3: {
						Match match = regexF3.Match(valuesProperty.Of(index).stringValue);
						Vector3 value = match.Success 
							? new Vector3(
								float.Parse(match.Groups["x"].Captures[0].Value), 
								float.Parse(match.Groups["y"].Captures[0].Value),
								float.Parse(match.Groups["z"].Captures[0].Value)
							) : Vector3.zero;
						value = EditorGUILayout.Vector3Field(name, value);
						valuesProperty.Of(index).stringValue = $"({value.x},{value.y},{value.z})";
						break;
					}
				}
			}
		}
	}
}