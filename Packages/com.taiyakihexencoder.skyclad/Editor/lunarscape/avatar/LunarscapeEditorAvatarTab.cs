using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace skyclad.lunarscape.editor {
	using System.Collections.Generic;
	using skyclad.editor;
	using skyclad.lunarscape.internalProc;

	internal sealed class LunarscapeEditorAvatarTab : VisualElement {
		private SerializedObject _avatarTableObj;
		private SerializedObject _physicsColliderTableObj;

		private DictionaryPopupBuilder<int> _colliderPopupBuilder;

		public LunarscapeEditorAvatarTab() {
			_avatarTableObj = new SerializedObject(GetRuntimeAvatarTable());
			_physicsColliderTableObj = new SerializedObject(GetRuntimeColliderTable());

			_colliderPopupBuilder = new DictionaryPopupBuilder<int>();

			CreateView();
		}

		private void CreateView() {
			LunarscapeEditorSettings settings = LunarscapeEditorSettings.of;
			LunarscapeSettings runtime = LunarscapeSettings.Get();

			VisualElement mainFrame = LunarscapeCommonDesign.MainFrame();
			mainFrame.Add(LunarscapeCommonDesign.Title("Avatar"));

			Button generateButton = new Button(
				clickEvent: () => {
					AvatarGenerator generator = new AvatarGenerator();
					if (generator.Validation(out string message)) {
						generator.Generate($"{LunarEditorConst.AUTO_GENERATE_PATH}{Path.DirectorySeparatorChar}Avatar.cs");
					} else {
						EditorUtility.DisplayDialog("Validation Error", message, "Ok");
					}
				}
			);
			generateButton.text = "Generate Script";
			generateButton.style.width = 160f;

			mainFrame.Add(generateButton);

			mainFrame.Add(new Spacer(height: 20f));

			TabView tabView = new TabView();
			tabView.Add(AvatarTab());
			tabView.Add(ColliderTab());

			mainFrame.Add(tabView);

			Add(mainFrame);

			OnUpdateColliderList();
		}

		private Tab AvatarTab() {
			Tab tab = LunarscapeCommonDesign.TabFrame("Avatar Settings");
			CreateAvatarTab(tab);
			return tab;
		}

		private void CreateAvatarTab(Tab tab) {
			Column pane = new Column();
			
			Row header = new Row()
				.Align(Align.Stretch);

			Label headerName = new Label("Name");
			headerName.style.flexBasis = 0f;
			headerName.style.flexGrow = 2f;

			Label headerCollider = new Label("Collider");
			headerCollider.style.flexBasis = 0f;
			headerCollider.style.flexGrow = 2f;

			header.AddChildren(
				headerName, 
				headerCollider,
				new Spacer(50f),
				new Spacer(50f)
			);
			pane.Add(header);

			SerializedProperty settingsProperty = _avatarTableObj.FindProperty("_settings");
			for (int i = 0; i < settingsProperty.arraySize; ++i) {
				SerializedProperty settingProperty = settingsProperty.Of(i);
				int index = i;

				Row row = new Row()
					.Align(Align.Stretch);

				TextField nameField = new TextField();
				nameField.style.flexBasis = 0f;
				nameField.style.flexGrow = 2f;
				nameField.SetValueWithoutNotify(settingProperty.Of("name").stringValue);
				nameField.RegisterValueChangedCallback(v => {
					settingProperty.Of("name").stringValue = v.newValue;
					_avatarTableObj.ApplyModifiedProperties();
				});

				SerializedProperty colliderProperty = settingProperty.Of("collider");
				PopupField<int> colliderField = _colliderPopupBuilder.Generate(colliderProperty.intValue);
				colliderField.RegisterValueChangedCallback(v => {
					settingProperty.Of("collider").intValue = v.newValue;
					_avatarTableObj.ApplyModifiedProperties();
				});
				colliderField.SetValueWithoutNotify(colliderProperty.intValue);
				colliderField.style.flexBasis = 0f;
				colliderField.style.flexGrow = 2f;

				Button moveUpButton = new Button(
					clickEvent: () => {
						if (index > 0) {
							settingsProperty.MoveArrayElement(index, index-1);
							_physicsColliderTableObj.ApplyModifiedProperties();

							tab.Remove(pane);
							CreateColliderTab(tab);
						}
					}
				);
				moveUpButton.enabledSelf = index > 0;
				moveUpButton.text = "↑";
				moveUpButton.style.marginLeft = 0f;
				moveUpButton.style.marginRight = 0f;
				moveUpButton.style.width = 50.0f;

				Button deleteButton = new Button(
					clickEvent: () => {
						settingsProperty.Delete(index);
						_physicsColliderTableObj.ApplyModifiedProperties();

						tab.Remove(pane);
						CreateColliderTab(tab);
					}
				);
				deleteButton.text = "×";
				deleteButton.style.marginLeft = 0f;
				deleteButton.style.marginRight = 0f;
				deleteButton.style.width = 50.0f;

				row.AddChildren(
					nameField,
					colliderField,
					moveUpButton,
					deleteButton
				);
				pane.Add(row);
			}

			Button addButton = new Button(
				clickEvent: () => {
					_avatarTableObj.Update();
					string name = "newAvatar";
					List<int> idList = new List<int>();
					List<string> nameList = new List<string>();

					SerializedProperty settingsProperty = _avatarTableObj.FindProperty("_settings");
					for(int i = 0; i < settingsProperty.arraySize; ++i) {
						SerializedProperty settingProperty = settingsProperty.Of(i);
						idList.Add(settingProperty.Of("id").intValue);
						nameList.Add(settingProperty.Of("name").stringValue);
					}
					idList.Sort();

					int n = 0;
					while(nameList.Contains(name)) {
						++n;
						name = $"newAvatar{n.ToString("00")}";
					}

					int newId = 1;
					foreach(int id in idList) {
						if (id > newId) { break; }
						newId = id + 1;
					}

					settingsProperty.Add(p => {
						p.Of("id").intValue = newId;
						p.Of("name").stringValue = name;
						p.Of("collider").intValue = -1;
					});
					_avatarTableObj.ApplyModifiedProperties();

					tab.Remove(pane);
					CreateAvatarTab(tab);
				}
			);
			addButton.text = "+";
			addButton.style.width = 24.0f;
			pane.Add(addButton);
			tab.Add(pane);
		}

		private LunarscapeAvatarTable GetRuntimeAvatarTable() {
			string[] guids = AssetDatabase.FindAssets($"t:{typeof(LunarscapeAvatarTable)}");
			if (guids.Length > 0) {
				string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
				return AssetDatabase.LoadAssetAtPath<LunarscapeAvatarTable>(assetPath);
			} else {
				LunarscapeAvatarTable table = ScriptableObject.CreateInstance<LunarscapeAvatarTable>();
				string assetPath = $"{LunarEditorConst.AUTO_GENERATE_RESOURCE_PATH}{Path.DirectorySeparatorChar}{typeof(LunarscapeAvatarTable).Name}.asset";
				SkycladEditorUtility.Asset.Create(table, $"Assets{Path.DirectorySeparatorChar}{assetPath}");
				SkycladEditorUtility.Resource.SetAddress(table, LunarscapeAvatarTable.resourceAddress);
				return table;
			}
		}

		private Tab ColliderTab() {
			Tab tab = LunarscapeCommonDesign.TabFrame("Collider Settings");
			CreateColliderTab(tab);
			return tab;
		}

		private void CreateColliderTab(Tab tab) {
			OnUpdateColliderList();

			Column pane = new Column();

			Row header = new Row();
			Label headerName = new Label("Name");
			headerName.style.flexGrow = 3f;
			headerName.style.flexBasis = 0f;
			Label headerRadius = new Label("Radius");
			headerRadius.style.flexGrow = 2f;
			headerRadius.style.flexBasis = 0f;
			Label headerHeight = new Label("Height");
			headerHeight.style.flexGrow = 2f;
			headerHeight.style.flexBasis = 0f;
			header.AddChildren(
				headerName,
				headerRadius,
				headerHeight,
				new Spacer(width: 50f),
				new Spacer(width: 50f)
			);
			pane.Add(header);

			SerializedProperty collidersProperty = _physicsColliderTableObj.FindProperty("_colliders");

			for(int i = 0; i < collidersProperty.arraySize; ++i) {
				int index = i;
				SerializedProperty colliderProperty = collidersProperty.Of(index);
				Row row = new Row()
					.Align(Align.Stretch);

				SerializedProperty nameProperty = colliderProperty.Of("name");
				TextField nameField = new TextField();
				nameField.isDelayed = true;
				nameField.style.flexGrow = 3f;
				nameField.style.flexBasis = 0f;
				nameField.SetValueWithoutNotify(nameProperty.stringValue);
				nameField.RegisterValueChangedCallback(v => {
					nameProperty.stringValue = v.newValue;
					_physicsColliderTableObj.ApplyModifiedProperties();
					OnUpdateColliderList();
				});

				SerializedProperty radiusProperty = colliderProperty.Of("radius");
				FloatField radiusField = new FloatField();
				radiusField.style.flexGrow = 2f;
				radiusField.style.flexBasis = 0f;
				radiusField.SetValueWithoutNotify(radiusProperty.floatValue);
				radiusField.RegisterValueChangedCallback(v => {
					radiusProperty.floatValue = v.newValue;
					_physicsColliderTableObj.ApplyModifiedProperties();
				});

				SerializedProperty heightProperty = colliderProperty.Of("height");
				FloatField heightField = new FloatField();
				heightField.style.flexGrow = 2f;
				heightField.style.flexBasis = 0f;
				heightField.SetValueWithoutNotify(heightProperty.floatValue);
				heightField.RegisterValueChangedCallback(v => {
					heightProperty.floatValue = v.newValue;
					_physicsColliderTableObj.ApplyModifiedProperties();
				});

				Button moveUpButton = new Button(
					clickEvent: () => {
						if (index > 0) {
							collidersProperty.MoveArrayElement(index, index-1);
							_physicsColliderTableObj.ApplyModifiedProperties();

							tab.Remove(pane);
							CreateColliderTab(tab);
						}
					}
				);
				moveUpButton.enabledSelf = index > 0;
				moveUpButton.text = "↑";
				moveUpButton.style.marginLeft = 0f;
				moveUpButton.style.marginRight = 0f;
				moveUpButton.style.width = 50.0f;

				Button deleteButton = new Button(
					clickEvent: () => {
						collidersProperty.Delete(index);
						_physicsColliderTableObj.ApplyModifiedProperties();

						tab.Remove(pane);
						CreateColliderTab(tab);
					}
				);
				deleteButton.text = "×";
				deleteButton.style.marginLeft = 0f;
				deleteButton.style.marginRight = 0f;
				deleteButton.style.width = 50.0f;

				row.AddChildren(
					nameField, 
					radiusField, 
					heightField,
					moveUpButton,
					deleteButton
				);
				pane.Add(row);
			}

			Button addButton = new Button(
				clickEvent: () => {
					_physicsColliderTableObj.Update();
					collidersProperty = _physicsColliderTableObj.FindProperty("_colliders");

					List<int> idList = new List<int>();
					List<string> nameList = new List<string>();
					for(int i = 0; i < collidersProperty.arraySize; ++i) {
						idList.Add(collidersProperty.Of(i).Of("id").intValue);
						nameList.Add(collidersProperty.Of(i).Of("name").stringValue);
					}
					idList.Sort();
					int newId = 1;
					foreach(int id in idList) {
						if (id > newId) { break; }
						newId = id+1;
					}

					string newName = "newCollider";
					int n = 1;
					while(nameList.Contains(newName)) {
						newName = $"newCollider_{n.ToString("00")}";
					}

					collidersProperty.Add(p => {
						p.Of("id").intValue = newId;
						p.Of("name").stringValue = newName;
						p.Of("radius").floatValue = 0.5f;
						p.Of("height").floatValue = 1.5f;
					});
					_physicsColliderTableObj.ApplyModifiedProperties();

					tab.Remove(pane);
					CreateColliderTab(tab);
				}
			);
			addButton.text = "+";
			addButton.style.width = 24.0f;

			pane.Add(addButton);

			tab.Add(pane);
		}

		private LunarscapeAvatarPhysicsColliderTable GetRuntimeColliderTable() {
			string[] guids = AssetDatabase.FindAssets($"t:{typeof(LunarscapeAvatarPhysicsColliderTable)}");
			if (guids.Length > 0) {
				string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
				return AssetDatabase.LoadAssetAtPath<LunarscapeAvatarPhysicsColliderTable>(assetPath);
			} else {
				LunarscapeAvatarPhysicsColliderTable table = ScriptableObject.CreateInstance<LunarscapeAvatarPhysicsColliderTable>();
				string assetPath = $"{LunarEditorConst.AUTO_GENERATE_RESOURCE_PATH}{Path.DirectorySeparatorChar}{typeof(LunarscapeAvatarPhysicsColliderTable).Name}.asset";
				SkycladEditorUtility.Asset.Create(table, $"Assets{Path.DirectorySeparatorChar}{assetPath}");
				SkycladEditorUtility.Resource.SetAddress(table, LunarscapeAvatarPhysicsColliderTable.resourceAddress);
				return table;
			}
		}

		private void OnUpdateColliderList() {
			_physicsColliderTableObj.Update();

			// ColliderPopupの更新
			SerializedProperty collidersProperty = _physicsColliderTableObj.FindProperty("_colliders");
			Dictionary<int, string> table = new Dictionary<int, string> {
				{ LunarscapeAvatarPhysicsColliderTable.EMPTY_COLLIDER, "empty" }
			};
			for(int i = 0; i < collidersProperty.arraySize; ++i) {
				SerializedProperty colliderProperty = collidersProperty.Of(i);
				table.Add(
					colliderProperty.Of("id").intValue,
					colliderProperty.Of("name").stringValue
				);
			}
			_colliderPopupBuilder.SetKeys(new List<int>(table.Keys));
			_colliderPopupBuilder.SetConverter(key => table.TryGetValue(key, out string name) ? name : " - ");
		}
	}
}