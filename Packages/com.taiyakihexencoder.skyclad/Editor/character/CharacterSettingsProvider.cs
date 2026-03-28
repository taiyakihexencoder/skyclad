using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using static skyclad.editor.SkycladEditor;

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
				using (SkycladEditor.GUI.Layout.Box(new RectOffset(20, 20, 20, 20))) {
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
			tabIndex = SkycladEditor.GUI.Layout.Toolbar(tabIndex, ref tabScroll, Repaint, contents);
		}

		private void StatusDefsEditView(SerializedProperty listProperty, SerializedProperty dynamicParametersProperty) {
			using (SkycladEditor.GUI.Layout.Horizontal) {
				SkycladEditor.GUI.Layout.Label("Type", Modifier.Width(100.0f));
				SkycladEditor.GUI.Layout.Label("Name", Modifier.Width(160.0f));
				SkycladEditor.GUI.Layout.Label("Dynamic", Modifier.Width(65.0f));
			}

			bool isDynamic;
			for (int i = 0; i < listProperty.arraySize; ++i) {
				SerializedProperty property = listProperty.Of(i);
				SerializedProperty guidProperty = property.Of("guid");
				SerializedProperty statusTypeProperty = property.Of("parameterType");
				SerializedProperty nameProperty = property.Of("name");

				using (SkycladEditor.GUI.Layout.Horizontal) {
					EditorGUILayout.PropertyField(statusTypeProperty, new GUIContent(""), GUILayout.Width(100.0f));

					nameProperty.stringValue = SkycladEditor.GUI.Layout.TextField(nameProperty.stringValue, Modifier.Width(160.0f));

					isDynamic = false;
					for (int j = 0; j < dynamicParametersProperty.arraySize; ++j) {
						if (dynamicParametersProperty.Of(j).stringValue == guidProperty.stringValue) {
							isDynamic = true;
							break;
						}
					}

					if (
						SkycladEditor.GUI.Layout.Toggle(
							isDynamic, 
							Modifier
								.Width(65.0f)
								.Align(StyleOption.HorizontalAlignment.Center)
							)
					) {
						if (!isDynamic) {
							// リストに追加
							dynamicParametersProperty.Add(
								(p) => {
									p.stringValue = guidProperty.stringValue;
								}
							);
						}
					} else {
						if (isDynamic) {
							// リストから削除
							for (int j = dynamicParametersProperty.arraySize-1; j >= 0; --j) {
								if (dynamicParametersProperty.Of(j).stringValue == guidProperty.stringValue) {
									dynamicParametersProperty.DeleteArrayElementAtIndex(j);
								}
							}
						}
					}

					if (SkycladEditor.GUI.Layout.MinusButton()) {
						listProperty.Delete(i);
						// リストから削除
						for (int j = dynamicParametersProperty.arraySize-1; j >= 0; --j) {
							if (dynamicParametersProperty.Of(j).stringValue == guidProperty.stringValue) {
								dynamicParametersProperty.DeleteArrayElementAtIndex(j);
							}
						}
						break;
					}
				}
			}
			if (SkycladEditor.GUI.Layout.PlusButton()) {
				listProperty.Add(p => {
					p.Of("parameterType").intValue = (int)ParameterType.Int;
					p.Of("name").stringValue = "";
				});
			}
		}

		private void ControlParameterDefsEditView(SerializedProperty listProperty) {
			using (SkycladEditor.GUI.Layout.Horizontal) {
				SkycladEditor.GUI.Layout.Label("Type", SkycladEditor.Modifier.Width(100.0f));
				SkycladEditor.GUI.Layout.Label("Name", SkycladEditor.Modifier.Width(160.0f));
			}
			for (int i = 0; i < listProperty.arraySize; ++i) {
				SerializedProperty property = listProperty.Of(i);
				SerializedProperty statusTypeProperty = property.Of("parameterType");
				SerializedProperty nameProperty = property.Of("name");

				using (SkycladEditor.GUI.Layout.Horizontal) {
					EditorGUILayout.PropertyField(statusTypeProperty, new GUIContent(""), GUILayout.Width(100.0f));

					nameProperty.stringValue = SkycladEditor.GUI.Layout.TextField(nameProperty.stringValue, SkycladEditor.Modifier.Width(160.0f));

					if (SkycladEditor.GUI.Layout.MinusButton()) {
						listProperty.Delete(i);
						break;
					}
				}
			}
			if (SkycladEditor.GUI.Layout.PlusButton()) {
				listProperty.Add(p => {
					p.Of("guid").stringValue = System.Guid.NewGuid().ToString();
					p.Of("parameterType").intValue = (int)ParameterType.Int;
					p.Of("name").stringValue = "";
				});
			}
		}

		private void GeneralView() {
			SerializedProperty characterProperty = serializedObject.FindProperty("_character");

			using (SkycladEditor.GUI.Layout.Box(new RectOffset(10,10,0,10))) {
				using (SkycladEditor.GUI.Layout.Horizontal) {
					if (SkycladEditor.GUI.Layout.Button("Update Script")) {
						CharacterStatusScriptGenerator.Generate(serializedObject);
						CharacterColliderScriptGenerator.Generate(serializedObject);
						CharacterControllerScriptGenerator.Generate(serializedObject);
						CharacterIdScriptGenerator.Generate(serializedObject);
					}
					
					SkycladEditor.GUI.Layout.Space(width: 20);
					
					if (SkycladEditor.GUI.Layout.Button("Update Binary")) {
						CharacterStatusTableBinaryGenerator.Generate(serializedObject, "characterStatus.bytes");
					}
				}
			}

			using (SkycladEditor.GUI.Layout.Horizontal) {
				using (SkycladEditor.GUI.Layout.Box()) {
					SerializedProperty parameterUnitsProperty = characterProperty.Of("_statusParameterUnits");

					SkycladEditor.GUI.Layout.Label("Parameters");
					
					SkycladEditor.GUI.Layout.Space(height: 10);

					EditorGUI.indentLevel++; {
						StatusDefsEditView(parameterUnitsProperty, characterProperty.Of("_dynamicParameters"));
					} EditorGUI.indentLevel--;

					SkycladEditor.GUI.Layout.Space(height: 50);

					SkycladEditor.GUI.Layout.Label("Colliders");
					SkycladEditor.GUI.Layout.Space(height: 10);

					SerializedProperty colliderUnitsProperty = characterProperty.Of("_colliderUnits");
					EditorGUI.indentLevel++; {
						using (SkycladEditor.GUI.Layout.Horizontal) {
							SkycladEditor.GUI.Layout.Label("name", Modifier.Width(120.0f));
							SkycladEditor.GUI.Layout.Label("radius", Modifier.Width(60.0f));
							SkycladEditor.GUI.Layout.Label("height", Modifier.Width(60.0f));
						}

						for (int i = 0; i < colliderUnitsProperty.arraySize; ++i) {
							using (SkycladEditor.GUI.Layout.Horizontal) {
								SerializedProperty colliderUnitProperty = colliderUnitsProperty.Of(i);
								colliderUnitProperty.Of("name").stringValue = SkycladEditor.GUI.Layout.TextField(colliderUnitProperty.Of("name").stringValue, Modifier.Width(120.0f));

								SerializedProperty radiusProperty = colliderUnitProperty.Of("radius");
								radiusProperty.floatValue = EditorGUILayout.DelayedFloatField(radiusProperty.floatValue, GUILayout.Width(60.0f));
								
								SerializedProperty heightProperty = colliderUnitProperty.Of("height");
								heightProperty.floatValue = EditorGUILayout.DelayedFloatField(heightProperty.floatValue, GUILayout.Width(60.0f));
								
								if (SkycladEditor.GUI.Layout.MinusButton()) {
									colliderUnitsProperty.Delete(i);
									break;
								}
							}
						}

						if (SkycladEditor.GUI.Layout.PlusButton()) {
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
			
				SkycladEditor.GUI.Layout.Space(width: 60);

				using (SkycladEditor.GUI.Layout.Box()) {
					EditorGUILayout.LabelField("Characters");

					EditorGUI.indentLevel++; {
						SerializedProperty unitsProperty = characterProperty.Of("_units");
						using (SkycladEditor.GUI.Layout.Horizontal) {
							SkycladEditor.GUI.Layout.Label("Name", Modifier.Width(160.0f));
							SkycladEditor.GUI.Layout.Label("Collide", Modifier.Width(60.0f));
							SkycladEditor.GUI.Layout.Label("Control", Modifier.Width(60.0f));
							SkycladEditor.GUI.Layout.Label("Status", Modifier.Width(60.0f));
						}
						for (int i = 0; i < unitsProperty.arraySize; ++i) {
							SerializedProperty unitProperty = unitsProperty.Of(i);
							SerializedProperty hasColliderProperty = unitProperty.Of("type.hasCollider");
							SerializedProperty hasControllerProperty = unitProperty.Of("type.hasController");
							SerializedProperty hasStatusProperty = unitProperty.Of("type.hasStatus");
							using (SkycladEditor.GUI.Layout.Horizontal) {
								unitProperty.Of("name").stringValue = SkycladEditor.GUI.Layout.TextField(unitProperty.Of("name").stringValue, Modifier.Width(160.0f));
								hasColliderProperty.boolValue = SkycladEditor.GUI.Layout.Toggle(
									hasColliderProperty.boolValue, 
									Modifier
										.Width(60.0f)
										.Align(StyleOption.HorizontalAlignment.Center)
								);

								hasControllerProperty.boolValue = SkycladEditor.GUI.Layout.Toggle(
									hasControllerProperty.boolValue,
									Modifier
										.Width(60.0f)
										.Align(StyleOption.HorizontalAlignment.Center)
								);

								hasStatusProperty.boolValue = SkycladEditor.GUI.Layout.Toggle(
									hasStatusProperty.boolValue,
									Modifier
										.Width(60.0f)
										.Align(StyleOption.HorizontalAlignment.Center)
								);

								if (SkycladEditor.GUI.Layout.MinusButton()) {
									unitsProperty.Delete(i);
									break;
								}
							}
						}
						if (SkycladEditor.GUI.Layout.PlusButton()) {
							unitsProperty.Add(p => {
								p.Of("guid").stringValue = System.Guid.NewGuid().ToString();
								p.Of("name").stringValue = "";
								p.Of("isPlayerCharacter").boolValue = false;
								p.Of("colliderGuid").stringValue = "";
								p.Of("controllerGuid").stringValue = "";
								p.Of("type").Of("hasCollider").boolValue = false;
								p.Of("type").Of("hasStatus").boolValue = false;
								p.Of("type").Of("hasController").boolValue = false;
								p.Of("status").Of("names").arraySize = 0;
								p.Of("status").Of("values").arraySize = 0;
								p.Of("controller").Of("names").arraySize = 0;
								p.Of("controller").Of("values").arraySize = 0;
								p.Of("hitBoxes").arraySize = 0;
								p.Of("hitBoxBelongsTo").uintValue = 0;
							});
						}
					} EditorGUI.indentLevel--;
				}
			}

			SkycladEditor.GUI.Layout.Space(height: 24);

			SkycladEditor.GUI.Layout.Label("Character Controller");
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
			SerializedProperty controllersProperty = serializedObject.FindProperty("_character._controllers");
			using (SkycladEditor.GUI.Layout.Horizontal) {
				SkycladEditor.GUI.Layout.Label("name", SkycladEditor.Modifier.Width(160.0f));
			}

			for (int i = 0; i < controllersProperty.arraySize; ++i) {
				SerializedProperty controllerProperty = controllersProperty.Of(i);
				SerializedProperty nameProperty = controllerProperty.Of("name");

				using (SkycladEditor.GUI.Layout.Horizontal) {
					nameProperty.stringValue = SkycladEditor.GUI.Layout.TextField(nameProperty.stringValue, SkycladEditor.Modifier.Width(160.0f));
					if (SkycladEditor.GUI.Layout.MinusButton()) {
						controllersProperty.Delete(i);
						break;
					}
				}
			}
			if (SkycladEditor.GUI.Layout.PlusButton()) {
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

			nameProperty.stringValue = SkycladEditor.GUI.Layout.TextField(nameProperty.stringValue, SkycladEditor.Modifier.Label("Name"));

			ControlParameterDefsEditView(controllerProperty.Of("_parameters"));
		}

		private void CharacterContentView(SerializedProperty unitProperty) {
			using(SkycladEditor.GUI.Layout.Horizontal) {
				unitProperty.Of("name").stringValue = SkycladEditor.GUI.Layout.TextField(unitProperty.Of("name").stringValue, SkycladEditor.Modifier.Label("Character Name"));
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

				SkycladEditor.GUI.Layout.Space(height:32);

				// HitBox
				using (SkycladEditor.GUI.Layout.Horizontal) {
					SerializedProperty hitBoxLayerProperty = unitProperty.Of("hitBoxLayer");
					SkycladEditor.GUI.Layout.Label("HitBox");
					SkycladEditor.GUI.Layout.Space(width: 50);
					SkycladEditor.GUI.Layout.Label("Layer:");
					hitBoxLayerProperty.uintValue = (uint) EditorGUILayout.IntPopup(
						(int)hitBoxLayerProperty.uintValue, 
						layerNameList.ToArray(), 
						layerValueList.ToArray(), 
						GUILayout.Width(120.0f)
					);
				}
				using (SkycladEditor.GUI.Layout.Box(new RectOffset(16,16,0,0))) {
					using(SkycladEditor.GUI.Layout.Horizontal) {
						SkycladEditor.GUI.Layout.Label("Extent", Modifier.Width(250.0f));
						SkycladEditor.GUI.Layout.Space(width:16);
						SkycladEditor.GUI.Layout.Label("Offset", Modifier.Width(250.0f));
					}

					SerializedProperty hitBoxesProperty = unitProperty.Of("hitBoxes");

					for(int i = 0; i < hitBoxesProperty.arraySize; ++i) {
						using (SkycladEditor.GUI.Layout.Horizontal) {
							SerializedProperty hitBoxProperty = hitBoxesProperty.Of(i);
							SerializedProperty extentProperty = hitBoxProperty.Of("extent");
							SerializedProperty offsetProperty = hitBoxProperty.Of("offset");

							extentProperty.vector3Value = EditorGUILayout.Vector3Field("", extentProperty.vector3Value, GUILayout.Width(250.0f));
							SkycladEditor.GUI.Layout.Space(width:16);
							offsetProperty.vector3Value = EditorGUILayout.Vector3Field("", offsetProperty.vector3Value, GUILayout.Width(250.0f));

							if (SkycladEditor.GUI.Layout.MinusButton()) {
								hitBoxesProperty.DeleteArrayElementAtIndex(i);
								break;
							}
						}
					}
					if (SkycladEditor.GUI.Layout.PlusButton()) {
						hitBoxesProperty.Add((p) => {
							p.Of("extent").vector3Value = Vector3.zero;
							p.Of("offset").vector3Value = Vector3.zero;
						});
					}

				}
			}
			
			SkycladEditor.GUI.Layout.Space(height:32);

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
							using (SkycladEditor.GUI.Layout.Box(new RectOffset(32, 32, 12, 0))) {
								ParameterInfoEditView(unitProperty.Of("controller"), controlProperty.Of("_parameters"));
							}
						}
					}

				} else {
					controllerProperty.stringValue = "";
				}
			}

			if (unitProperty.Of("type.hasStatus").boolValue) {
				SkycladEditor.GUI.Layout.Label("Status");
				using (SkycladEditor.GUI.Layout.Box(new RectOffset(32, 32, 12, 0))) {
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

				switch((ParameterType)parameterProperty.Of("parameterType").intValue) {
					case ParameterType.Int: {
						int value = int.TryParse(valuesProperty.Of(index).stringValue, out int v) ? v : 0;
						value = EditorGUILayout.DelayedIntField(name, value);
						valuesProperty.Of(index).stringValue = value.ToString();
						break;
					}
					case ParameterType.Bool: {
						bool value = bool.TryParse(valuesProperty.Of(index).stringValue, out bool v) ? v : false;
						value = EditorGUILayout.Toggle(name, value);
						valuesProperty.Of(index).stringValue = value.ToString();
						break;
					}
					case ParameterType.Float: {
						float value = float.TryParse(valuesProperty.Of(index).stringValue, out float v) ? v : 0.0f;
						value = EditorGUILayout.DelayedFloatField(name, value);
						valuesProperty.Of(index).stringValue = value.ToString();
						break;
					}
					case ParameterType.Float2: {
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
					case ParameterType.Float3: {
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