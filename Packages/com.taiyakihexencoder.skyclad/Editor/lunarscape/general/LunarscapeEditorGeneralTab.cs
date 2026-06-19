using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace skyclad.lunarscape.editor {
	using System.IO;
	using skyclad.editor;
	using skyclad.lunarscape.internalProc;
	using UnityEngine;

	internal class LunarscapeEditorGeneralTab : VisualElement {
		private VisualElement _contentPane;
		private SerializedObject _loadTableObj;

		private DictionaryPopupBuilder<int> _playerPopupBuilder;

		public LunarscapeEditorGeneralTab() {
			VisualElement mainFrame = LunarscapeCommonDesign.MainFrame();
			mainFrame.Add(LunarscapeCommonDesign.Title("General"));
			Add(mainFrame);

			List<int> keys = new List<int>();
			for (int i = 0; i < 16; ++i) {
				keys.Add(i);
			}
			_playerPopupBuilder = new DictionaryPopupBuilder<int>()
				.SetConverter(index => $"Player {index.ToString("00")}")
				.SetKeys(keys);


			if (TryGetLoadTable(out LunarscapeLoadTable loadTable)) {
				_loadTableObj = new SerializedObject(loadTable);

				Row pane = new Row();

				_contentPane = new Column()
					.Padding(horizontal: 16f);
				_contentPane.style.flexBasis = 0f;
				_contentPane.style.flexGrow = 5f;

				pane.Add(LoadGroupView());
				pane.Add(_contentPane);

				mainFrame.Add(pane);
			}
		}

		private bool TryGetLoadTable(out LunarscapeLoadTable table) {
			table = null;
			string[] guids = AssetDatabase.FindAssets($"t:{typeof(LunarscapeLoadTable).Name}");
			if (guids.Length > 0) {
				string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
				table = AssetDatabase.LoadAssetAtPath<LunarscapeLoadTable>(assetPath);
			}

			if (table == null) {
				table = ScriptableObject.CreateInstance<LunarscapeLoadTable>();
				SkycladEditorUtility.Asset.Create(table, $"Assets{Path.DirectorySeparatorChar}{LunarEditorConst.AUTO_GENERATE_RESOURCE_PATH}{Path.DirectorySeparatorChar}{typeof(LunarscapeLoadTable).Name}.asset");
				SkycladEditorUtility.Resource.SetAddress(table, LunarscapeLoadTable.resourceAddress);
			}
			return table != null;
		}

		private VisualElement LoadGroupView() {
			SelectableListView<string> rootView = new SelectableListView<string>()
				.Border(new UnityEngine.Color(0f,0f,0f));
			rootView.style.flexBasis = 0f;
			rootView.style.flexGrow = 1f;
			rootView.selectionChanged += UpdateContentPane;

			Dictionary<string, string> list = GetLoadBlockList();

			List<string> guidList = new List<string>(list.Keys);
			List<string> nameList = new List<string>();
			List<int> order = new List<int>();
			for(int i = 0; i < guidList.Count; ++i) {
				nameList.Add(list[guidList[i]]);
				order.Add(i);
			}
			order.Sort((a, b) => nameList[a].CompareTo(nameList[b]));

			for(int i = 0; i < order.Count; ++i) {
				string guid = guidList[order[i]];
				string name = nameList[order[i]];

				Row row = new Row()
					.Padding(vertical: 16f, horizontal: 8f);

				Label nameLabel = new Label(name);
				nameLabel.style.whiteSpace = WhiteSpace.Normal;
				row.AddChildren(nameLabel);

				rootView.AddSelection(guid, row);
			}
			if (order.Count > 0) {
				rootView.Select(guidList[order[0]]);
			}
			return rootView;
		}

		private Dictionary<string, string> GetLoadBlockList() {
			string[] guids = AssetDatabase.FindAssets($"t:{typeof(LunarscapeFieldTable)}");
			Dictionary<string, string> table = new Dictionary<string, string>
			{
				{ LunarscapeLoadTable.GLOBAL_GUID, "Global" }
			};
			if (guids.Length > 0) {
				string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
				LunarscapeFieldTable asset = AssetDatabase.LoadAssetAtPath<LunarscapeFieldTable>(assetPath);

				SerializedObject obj = new SerializedObject(asset);
				SerializedProperty rowsProperty = obj.FindProperty("_rows");
				for(int i = 0; i < rowsProperty.arraySize; ++i) {
					SerializedProperty rowProperty = rowsProperty.Of(i);
					table.Add(rowProperty.Of("guid").stringValue, rowProperty.Of("address").stringValue);
				}
			}
			return table;
		}

		private void UpdateContentPane(string guid) {
			_contentPane.Clear();

			if (guid != null) {
				TryGetAvatarIdList(out List<int> avatarIds, out List<string> avatarNames);

				SerializedProperty groupsProperty = _loadTableObj.FindProperty("_groups");
				SerializedProperty groupProperty = null;
				for (int i = 0; i < groupsProperty.arraySize; ++i) {
					SerializedProperty property = groupsProperty.Of(i);
					if (property.Of("fieldGuid").stringValue == guid) {
						groupProperty = property;
						break;
					}
				}

				if (groupProperty == null) {
					groupsProperty.Add(p => {
						p.Of("fieldGuid").stringValue = guid;
						p.Of("avatarSpawns").arraySize = 0;
						groupProperty = p;
					});
					_loadTableObj.ApplyModifiedProperties();
				}


				// Avatar spawns

				VisualElement avatarHeader = LunarscapeCommonDesign.Header("Avatars");
				_contentPane.Add(avatarHeader);
				_contentPane.Add(new Spacer(height: 16.0f));
				SerializedProperty avatarSpawnsProperty = groupProperty.Of("avatarSpawns");

				Row header = new Row()
					.Padding(horizontal: 24.0f);

				Label headerAvatar = new Label("Avatar");
				headerAvatar.style.flexBasis = 0f;
				headerAvatar.style.flexGrow = 1f;

				Label headerPosition = new Label("Position");
				headerPosition.style.flexBasis = 0f;
				headerPosition.style.flexGrow = 2f;

				Label headerRotation = new Label("Rotation");
				headerRotation.style.flexBasis = 0f;
				headerRotation.style.flexGrow = 2f;

				Label headerFieldLoad = new Label("Player");
				headerFieldLoad.style.width = 50.0f;

				header.AddChildren(headerAvatar, headerPosition, headerRotation, headerFieldLoad, new Spacer(width:60f));
				_contentPane.Add(header);

				for (int i = 0; i < avatarSpawnsProperty.arraySize; ++i) {
					int index = i;
					SerializedProperty avatarSpawnProperty = avatarSpawnsProperty.Of(index);
					SerializedProperty avatarIdProperty = avatarSpawnProperty.Of("avatarId");
					SerializedProperty positionProperty = avatarSpawnProperty.Of("position");
					SerializedProperty rotationProperty = avatarSpawnProperty.Of("rotation");
					SerializedProperty playerIndexProperty = avatarSpawnProperty.Of("playerIndex");

					Row row = new Row()
						.Padding(horizontal: 24.0f);

					PopupField<int> avatarField = new PopupField<int>(
						avatarIds, 
						0,
						formatListItemCallback: (selectedId) => { 
							int index = avatarIds.FindIndex(_ => _ == selectedId);
							return $"{selectedId}:" + (index >= 0 ? avatarNames[index] : " - ");
						},
						formatSelectedValueCallback: (selectedId) => {
							int index = avatarIds.FindIndex(_ => _ == selectedId);
							return index >= 0 ? avatarNames[index] : " - ";
						}
					);
					avatarField.style.flexBasis = 0f;
					avatarField.style.flexGrow = 1f;
					avatarField.SetValueWithoutNotify(avatarIdProperty.intValue);
					avatarField.RegisterValueChangedCallback(v => {
						avatarIdProperty.intValue = v.newValue;
						_loadTableObj.ApplyModifiedProperties();
					});
					row.AddChildren(avatarField);

					if (playerIndexProperty.intValue < 0) {
						Vector3Field positionField = new Vector3Field();
						positionField.style.flexBasis = 0f;
						positionField.style.flexGrow = 2f;
						positionField.SetValueWithoutNotify(positionProperty.vector3Value);
						positionField.RegisterValueChangedCallback(v => {
							positionProperty.vector3Value = v.newValue;
							_loadTableObj.ApplyModifiedProperties();
						});

						Vector3Field rotationField = new Vector3Field();
						rotationField.style.flexBasis = 0f;
						rotationField.style.flexGrow = 2f;
						rotationField.SetValueWithoutNotify(rotationProperty.quaternionValue.eulerAngles);
						rotationField.RegisterValueChangedCallback(v => {
							rotationProperty.quaternionValue = Quaternion.Euler(v.newValue);
							_loadTableObj.ApplyModifiedProperties();
						});

						row.AddChildren(positionField, rotationField);
					} else {
						PopupField<int> playerIndexPopupField = _playerPopupBuilder.Generate(playerIndexProperty.intValue);
						playerIndexPopupField.style.flexBasis = 0f;
						playerIndexPopupField.style.flexGrow = 4f;
						playerIndexPopupField.RegisterValueChangedCallback(v => {
							playerIndexProperty.intValue = v.newValue;
							_loadTableObj.ApplyModifiedProperties();
						});
						row.AddChildren(playerIndexPopupField);
					}

					VisualElement fieldLoaderToggle = new VisualElement();
					fieldLoaderToggle.style.flexDirection = FlexDirection.Row;
					fieldLoaderToggle.style.justifyContent = Justify.Center;
					fieldLoaderToggle.style.alignContent = Align.Center;
					fieldLoaderToggle.style.width = 50.0f;

					Toggle fieldLoaderField = new Toggle();
					fieldLoaderField.SetValueWithoutNotify(playerIndexProperty.intValue >= 0);
					fieldLoaderField.RegisterValueChangedCallback(v => {
						if (v.newValue) {
							playerIndexProperty.intValue = 0;
						} else {
							playerIndexProperty.intValue = -1;
							positionProperty.vector3Value = Vector3.zero;
							rotationProperty.quaternionValue = Quaternion.identity;
						}
						_loadTableObj.ApplyModifiedProperties();
						UpdateContentPane(guid);
					});
					fieldLoaderToggle.Add(fieldLoaderField);

					Button upButton = new Button(
						clickEvent: () => {
							avatarSpawnsProperty.MoveArrayElement(index, index - 1);
							_loadTableObj.ApplyModifiedProperties();
							UpdateContentPane(guid);
						}
					);
					upButton.text = "↑";
					upButton.enabledSelf = index > 0;
					upButton.style.width = 30.0f;

					Button deleteButton = new Button(
						clickEvent: () => {
							avatarSpawnsProperty.Delete(index);
							_loadTableObj.ApplyModifiedProperties();
							UpdateContentPane(guid);
						}
					);
					deleteButton.text = "×";
					deleteButton.style.width = 30.0f;


					row.AddChildren(fieldLoaderToggle, upButton, deleteButton);

					_contentPane.Add(row);
				}

				Button addButton = new Button(
					clickEvent: () => {
						avatarSpawnsProperty.Add(p => {
							p.Of("avatarId").intValue = -1;
							p.Of("position").vector3Value = Vector3.zero;
							p.Of("rotation").quaternionValue = Quaternion.identity;
							p.Of("playerIndex").intValue = -1;
						});
						_loadTableObj.ApplyModifiedProperties();
						UpdateContentPane(guid);
					}
				);
				addButton.text = "+";
				addButton.style.width = 30.0f;
				_contentPane.Add(addButton);
			}
		}

		private bool TryGetAvatarIdList(out List<int> ids, out List<string> names) {
			ids = new List<int>();
			names = new List<string>();

			string[] guids = AssetDatabase.FindAssets($"t:{typeof(LunarscapeAvatarTable)}");
			if (guids.Length > 0) {
				string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
				LunarscapeAvatarTable table = AssetDatabase.LoadAssetAtPath<LunarscapeAvatarTable>(assetPath);
				foreach(LunarscapeAvatarTable.AvatarSettings setting in table.Settings) {
					ids.Add(setting.id);
					names.Add(setting.name);
				}
			}
			return true;
		}
	}
}