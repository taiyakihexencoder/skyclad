using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace skyclad.editor {
	internal sealed class DioramaSettingsProvider : SettingsProvider {
		private SerializedObject serializedObject;

		private Vector2 _scrollPosition;
		private int _selectedIndex;
		private float _tabScroll;

		string[] characterNames;
		string[] characterGuids;

		private BulletSettingsModel bulletSettingsModel;

		internal DioramaSettingsProvider(
			string path,
			SettingsScope scopes,
			IEnumerable<string> keywords = null
		) : base(path, scopes, keywords) {
		}

		public override void OnActivate(string searchContext, VisualElement rootElement)
		{
			base.OnActivate(searchContext, rootElement);

			serializedObject = new SerializedObject(SkycladProjectSettings.Instance);
			_scrollPosition = Vector2.zero;
			_selectedIndex = -1;
			_tabScroll = 0.0f;

			bulletSettingsModel = new BulletSettingsModel(serializedObject);

			List<string> characterNameList = new List<string>();
			List<string> characterGuidList = new List<string>();

			SerializedProperty characterUnitsProperty = serializedObject.FindProperty("_character._units");
			for(int i = 0; i < characterUnitsProperty.arraySize; ++i) {
				SerializedProperty characterUnitProperty = characterUnitsProperty.Of(i);
				string name = characterUnitProperty.Of("name").stringValue;
				if (!string.IsNullOrEmpty(name)) {
					characterNameList.Add(name);
					characterGuidList.Add(characterUnitProperty.Of("guid").stringValue);
				}
			}
			characterNames = characterNameList.ToArray();
			characterGuids = characterGuidList.ToArray();
		}

		[SettingsProvider]
		public static SettingsProvider CreateSettingsProvider() {
			return new DioramaSettingsProvider(
				path: "Skyclad/Diorama",
				scopes: SettingsScope.Project,
				keywords: new string[] { "skyclad", }
			);
		}

		public override void OnGUI(string searchContext) {
			serializedObject.Update();

			using (serializedObject.ChangeCheckScope()) {
				if (SkycladEditorGUI.Layout.Button("Generate Script")) {
					DioramaIdScriptGenerator.Generate(serializedObject);
					DioramaLoadJobScriptGenerator.Generate(serializedObject);
					DioramaUnloadJobScriptGenerator.Generate(serializedObject);
				}

				SkycladEditorGUI.Layout.Space(height: 20);
				
				List<string> tabs = new List<string>();
				SerializedProperty dioramasProperty = serializedObject.FindProperty("_diorama._units");
				for(int i = 0; i < dioramasProperty.arraySize; ++i) {
					SerializedProperty dioramaProperty = dioramasProperty.Of(i);
					SerializedProperty nameProperty = dioramaProperty.Of("name");
					string visibleName = string.IsNullOrEmpty(nameProperty.stringValue) ? $"Diorama{i.ToString("000")}" : nameProperty.stringValue;
					tabs.Add(visibleName);
				}

				using (SkycladEditorGUI.Layout.Horizontal) {
					if (SkycladEditorGUI.Layout.Button("General")) {
						_selectedIndex = -1;
						_scrollPosition = Vector2.zero;
					}

					int selectedIndex = _selectedIndex;
					_selectedIndex = SkycladEditorGUI.Layout.Toolbar(
						_selectedIndex, 
						ref _tabScroll,
						repaint: Repaint,
						tabs.ToArray()
					);
					if (selectedIndex != _selectedIndex) {
						_scrollPosition = Vector2.zero;
					}
				}

				if (_selectedIndex < 0) {
					GeneralEditDrawer();
				} else if(_selectedIndex < dioramasProperty.arraySize) {
					DioramaEditDrawer(dioramasProperty.Of(_selectedIndex), _selectedIndex);
				}
			}
		}

		private void GeneralEditDrawer() {
			SerializedProperty dioramasProperty = serializedObject.FindProperty("_diorama._units");
			using(EditorGUILayout.ScrollViewScope scroll = new EditorGUILayout.ScrollViewScope(_scrollPosition)) {
				using(SkycladEditorGUI.Layout.Box(new RectOffset(20, 20, 12, 12))) {
					
					for(int i = 0; i < dioramasProperty.arraySize; ++i) {
						SerializedProperty dioramaProperty = dioramasProperty.Of(i);
						SerializedProperty nameProperty = dioramaProperty.Of("name");
						string visibleName = string.IsNullOrEmpty(nameProperty.stringValue) ? $"Diorama{i.ToString("000")}" : nameProperty.stringValue;

						using (SkycladEditorGUI.Layout.Horizontal) {
							SkycladEditorGUI.Layout.Label(visibleName, width: 250.0f);
							if (SkycladEditorGUI.Layout.Button("Edit")) {
								_selectedIndex = i;
								_scrollPosition = Vector2.zero;
							}

							if (SkycladEditorGUI.Layout.MinusButton()) {
								dioramasProperty.DeleteArrayElementAtIndex(i);
								break;
							}
						}
					}

					if (SkycladEditorGUI.Layout.PlusButton()) {
						dioramasProperty.Add(p => {
							p.Of("name").stringValue = "";
							p.Of("guid").stringValue = System.Guid.NewGuid().ToString();
							p.Of("characters").arraySize = 0;
							p.Of("loadOnLaunch").boolValue = false;
							p.Of("basis").vector3Value = Vector3.zero;
						});
					}
				}
				_scrollPosition = scroll.scrollPosition;
			}
		}

		private void DioramaEditDrawer(SerializedProperty property, int index) {
			using(EditorGUILayout.ScrollViewScope scroll = new EditorGUILayout.ScrollViewScope(_scrollPosition)) {
				using(SkycladEditorGUI.Layout.Box(new RectOffset(20, 20, 12, 12))) {
					SerializedProperty nameProperty = property.Of("name");
					SkycladEditorGUI.Layout.TextField(nameProperty, "Name");
					SkycladEditorGUI.Layout.Space(height: 12);
					
					SerializedProperty loadOnLaunchProperty = property.Of("loadOnLaunch");
					loadOnLaunchProperty.boolValue = EditorGUILayout.Toggle("Load on launch", loadOnLaunchProperty.boolValue);

					SerializedProperty basisProperty = property.Of("basis");
					basisProperty.vector3Value = EditorGUILayout.Vector3Field("basis", basisProperty.vector3Value);

					SkycladEditorGUI.Layout.Space(height: 30);


					SkycladEditorGUI.Layout.Label("Characters");
					SerializedProperty charactersProperty = property.Of("characters");

					using (SkycladEditorGUI.Layout.Horizontal) {
						SkycladEditorGUI.Layout.Label("character", 200.0f);
						SkycladEditorGUI.Layout.Space(width: 20);
						SkycladEditorGUI.Layout.Label("position", 200.0f);
						SkycladEditorGUI.Layout.Space(width: 20);
						SkycladEditorGUI.Layout.Label("rotation", 200.0f);
						SkycladEditorGUI.Layout.Space(width: 20);
					}
					for (int i = 0; i < charactersProperty.arraySize; ++i) {
						CharacterEditDrawer(charactersProperty, i);
					}

					if (SkycladEditorGUI.Layout.PlusButton()) {
						charactersProperty.Add(p => {
							p.Of("guid").stringValue = "";
							p.Of("position").vector3Value = Vector3.zero;
							p.Of("rotation").quaternionValue = Quaternion.identity;
						});
					}

					SkycladEditor.GUI.Layout.Space(height: 30);

					SkycladEditor.GUI.Layout.Label("Bullets");
					using(SkycladEditor.GUI.Layout.Box(new RectOffset(20,20,0,0))) {
						SerializedProperty bulletGroupsProperty = property.Of("bulletGroups");
						List<string> groupNames = bulletSettingsModel.GroupNames;
						List<string> groupGuids = bulletSettingsModel.GroupGuids;

						for(int i = 0; i < bulletGroupsProperty.arraySize; ++i) {
							using(SkycladEditor.GUI.Layout.Horizontal) {
								SerializedProperty bulletGroupProperty = bulletGroupsProperty.Of(i);
								string selected = bulletGroupProperty.stringValue;
								int selectedIndex = groupGuids.FindIndex(_ => selected == _);
								selectedIndex = EditorGUILayout.Popup(selectedIndex, groupNames.ToArray());
								
								if (0 <= selectedIndex && selectedIndex < groupGuids.Count) {
									bulletGroupProperty.stringValue = groupGuids[selectedIndex];
								}

								if (SkycladEditor.GUI.Layout.MinusButton()) {
									bulletGroupsProperty.DeleteArrayElementAtIndex(i);
									break;
								}
							}
						}
						
						if (SkycladEditor.GUI.Layout.PlusButton()){
							bulletGroupsProperty.Add((p) => p.stringValue = "");
						}
					}


					SkycladEditor.GUI.Layout.Space(height: 30);

					SkycladEditorGUI.Layout.Label("Field");

					using (SkycladEditorGUI.Layout.Box(new RectOffset(20, 20, 0, 0))) {
						SerializedProperty fieldAssetsProperty = property.Of("fieldAssets");
						for(int i = 0; i < fieldAssetsProperty.arraySize; ++i) {
							SerializedProperty fieldAssetProperty = fieldAssetsProperty.Of(i);
							using (SkycladEditorGUI.Layout.Horizontal) {
								EditorGUILayout.PropertyField(fieldAssetProperty, new GUIContent($"Element {i}"), GUILayout.Width(400.0f));
								if (SkycladEditorGUI.Layout.MinusButton()) {
									fieldAssetsProperty.arraySize--;
									break;
								}
							}
						}
						if (SkycladEditorGUI.Layout.PlusButton()) {
							fieldAssetsProperty.Add((p) => { p.Of("m_AssetGUID").stringValue = ""; });
						}
					}

					SkycladEditorGUI.Layout.Space(height: 30);

					SkycladEditorGUI.Layout.Label("Events");
				}
			}
		}

		private void CharacterEditDrawer(SerializedProperty parentProperty, int index) {
			SerializedProperty property = parentProperty.Of(index);
			using (SkycladEditorGUI.Layout.Horizontal) {
				SerializedProperty guidProperty = property.Of("guid");
				SerializedProperty positionProperty = property.Of("position");
				SerializedProperty rotationProperty = property.Of("rotation");

				int selectedCharacter = -1;
				for(int i = 0; i < characterGuids.Length; ++i) {
					if (guidProperty.stringValue == characterGuids[i]) {
						selectedCharacter = i;
						break;
					}
				}

				selectedCharacter = EditorGUILayout.Popup(selectedCharacter, characterNames, GUILayout.Width(200.0f));
				if (0 <= selectedCharacter && selectedCharacter < characterGuids.Length) {
					guidProperty.stringValue = characterGuids[selectedCharacter];
				} else {
					guidProperty.stringValue = "";
				}

				SkycladEditorGUI.Layout.Space(width: 20);

				positionProperty.vector3Value = EditorGUILayout.Vector3Field("", positionProperty.vector3Value, GUILayout.Width(200.0f));
				Vector3 angle = rotationProperty.quaternionValue.eulerAngles;
				
				
				SkycladEditorGUI.Layout.Space(width: 20);

				angle = EditorGUILayout.Vector3Field("", angle, GUILayout.Width(200.0f));
				rotationProperty.quaternionValue = Quaternion.Euler(angle);

				SkycladEditorGUI.Layout.Space(width: 20);

				if (SkycladEditorGUI.Layout.MinusButton()) {
					parentProperty.Delete(index);
				}
			}
		}
	}
}