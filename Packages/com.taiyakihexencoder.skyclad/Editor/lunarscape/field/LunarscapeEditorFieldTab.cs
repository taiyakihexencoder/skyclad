using UnityEditor;
using UnityEngine.UIElements;

namespace skyclad.lunarscape.editor {
	using System.Collections.Generic;
	using System.IO;
	using skyclad.editor;
	using skyclad.lunarscape.internalProc;
	using UnityEngine;

	internal sealed class LunarscapeEditorFieldTab : VisualElement {
		private SerializedObject _runtimeObj;

		public LunarscapeEditorFieldTab() {
			_runtimeObj = new SerializedObject(LunarscapeSettings.Get());
			CreateView();
		}

		private void CreateView() {
			LunarscapeEditorSettings settings = LunarscapeEditorSettings.of;
			LunarscapeSettings runtime = LunarscapeSettings.Get();

			VisualElement mainFrame = LunarscapeCommonDesign.MainFrame();
			mainFrame.Add(LunarscapeCommonDesign.Title("Field"));

			EnumField fieldEditTypeField = new EnumField("Field Editor Mode", settings.Field.FieldEditorMode);
			fieldEditTypeField.RegisterValueChangedCallback(v => {
				LunarscapeEditorSettings.of.Field.FieldEditorMode = (FieldEditorMode)v.newValue;
				LunarscapeEditorSettings.Save();
				Clear();
				CreateView();
			});
			mainFrame.Add(fieldEditTypeField);

			// フィールドを生成する距離
			FloatField loadDistanceField = new FloatField("Load Field Distance");
			loadDistanceField.SetValueWithoutNotify(runtime.Field.LoadFieldDistance);
			loadDistanceField.RegisterValueChangedCallback(v => {
				SerializedProperty property = _runtimeObj.FindProperty("_field._loadFieldDistance");
				property.floatValue = v.newValue;
				_runtimeObj.ApplyModifiedProperties();
			});
			mainFrame.Add(loadDistanceField);

			// フィールドを削除する距離
			FloatField unloadDistanceField = new FloatField("Unload Field Distance");
			unloadDistanceField.SetValueWithoutNotify(runtime.Field.UnloadFieldDistance);
			unloadDistanceField.RegisterValueChangedCallback(v => {
				SerializedProperty property = _runtimeObj.FindProperty("_field._unloadFieldDistance");
				property.floatValue = v.newValue;
				_runtimeObj.ApplyModifiedProperties();
			});
			mainFrame.Add(unloadDistanceField);

			// キャッシュするフィールドメッシュ数
			IntegerField cacheMeshSizeField = new IntegerField("Mesh Cache Count");
			cacheMeshSizeField.SetValueWithoutNotify(runtime.Field.FieldMeshCacheSize);
			cacheMeshSizeField.RegisterValueChangedCallback(v => {
				SerializedProperty property = _runtimeObj.FindProperty("_field._fieldMeshCacheSize");
				property.intValue = v.newValue;
				_runtimeObj.ApplyModifiedProperties();
			});
			mainFrame.Add(cacheMeshSizeField);

			switch(LunarscapeEditorSettings.of.Field.FieldEditorMode) {
				case FieldEditorMode.SideView: {
					mainFrame.Add(SideViewSettingsView(settings, mainFrame));
					break;
				}
			}
			mainFrame.Add(new Spacer(height: 20f));
			mainFrame.Add(FieldAssetListView(LunarscapeEditorSettings.of.Field.FieldEditorMode));

			Add(mainFrame);
		}

		private VisualElement SideViewSettingsView(LunarscapeEditorSettings settings, VisualElement mainFrame) {
			VisualElement pane = new VisualElement();
			pane.style.flexDirection = FlexDirection.Row;

			FloatField widthField = new FloatField("Width");
			widthField.SetValueWithoutNotify(settings.Field.SideView.Width);
			widthField.RegisterValueChangedCallback(v => {
				LunarscapeEditorSettings.of.Field.SideView.Width = v.newValue;
				LunarscapeEditorSettings.Save();
			});
			widthField.style.flexGrow = 1f;
			pane.Add(widthField);

			FloatField zOffsetField = new FloatField("Z Offset");
			zOffsetField.SetValueWithoutNotify(settings.Field.SideView.ZOffset);
			zOffsetField.RegisterValueChangedCallback(v => {
				LunarscapeEditorSettings.of.Field.SideView.ZOffset = v.newValue;
				LunarscapeEditorSettings.Save();
			});
			zOffsetField.style.flexGrow = 1f;
			pane.Add(zOffsetField);

			return pane;
		}

		private VisualElement FieldAssetListView(FieldEditorMode editMode) {
			Column view = new Column()
				.Padding(horizontal: 12f);

			Row titleRow = new Row();
			VisualElement title = LunarscapeCommonDesign.Header("Field Assets");
			Button button = new Button(
				clickEvent: () => {
					FieldAssetGenerator generator = new FieldAssetGenerator(editMode);
					if (generator.Validation(out string error)) {
						generator.Generate(LunarEditorConst.AUTO_GENERATE_PATH + Path.DirectorySeparatorChar + "FieldAssetAddress.cs");
						generator.GenerateResources();
					} else {
						EditorUtility.DisplayDialog("Error", error, "OK");
					}
				}
			);
			button.text = "Generate Resource And Script";
			titleRow.AddChildren(title, new Spacer(width: 40.0f), button);

			Row header = new Row()
				.Align(Align.FlexStart)
				.Background(Color.grey)
				.Border(Color.black);
			Label headerGuid = new Label("guid");
			headerGuid.style.width = 200.0f;
			Label headerAssetPath = new Label("asset path");
			headerAssetPath.style.flexBasis = 0f;
			headerAssetPath.style.flexGrow = 1f;
			Label headerAddress = new Label("address");
			headerAddress.style.flexBasis = 0f;
			headerAddress.style.flexGrow = 1f;
			header.AddChildren(headerGuid, headerAssetPath, headerAddress);

			ScrollView scrollView = new ScrollView(ScrollViewMode.Vertical);
			scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
			scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

			scrollView.style.flexDirection = FlexDirection.Column;
			scrollView.style.flexGrow = 1f;
			foreach(AssetInfo asset in GetFieldAssetList(editMode)) {
				Row row = new Row()
					.Height(20f)
					.Align(Align.FlexStart)
					.Border(Color.black);

				Label guidLabel = new Label(asset.guid);
				guidLabel.style.width = 200.0f;
				guidLabel.style.fontSize = 10f;
				guidLabel.style.height = 20f;
				guidLabel.style.marginBottom = 0f;
				guidLabel.style.marginTop = 0f;

				Label assetPathLabel = new Label(asset.assetPath);
				assetPathLabel.style.flexBasis = 0f;
				assetPathLabel.style.flexGrow = 1f;
				assetPathLabel.style.height = 20f;
				assetPathLabel.style.fontSize = 10f;
				assetPathLabel.style.marginBottom = 0f;
				assetPathLabel.style.marginTop = 0f;

				TextField runtimeAddressField = new TextField();
				runtimeAddressField.style.flexBasis = 0f;
				runtimeAddressField.style.flexGrow = 1f;
				runtimeAddressField.style.height = 20f;
				runtimeAddressField.style.fontSize = 10f;
				runtimeAddressField.style.marginBottom = 0f;
				runtimeAddressField.style.marginTop = 0f;
				runtimeAddressField.isDelayed = true;

				string assetPath = asset.assetPath;
				runtimeAddressField.RegisterValueChangedCallback(v => {
					SerializedObject obj = new SerializedObject(AssetDatabase.LoadAssetAtPath<Object>(assetPath));
					obj.FindProperty("_runtimeAssetAddress").stringValue = v.newValue;
					obj.ApplyModifiedProperties();
				});
				if (string.IsNullOrEmpty(asset.runtimeAssetAddress)) {
					// レイアウト後に実行しないとValueChangedCallbackは呼ばれない
					EditorApplication.delayCall += () => {
						string fileName = Path.GetFileNameWithoutExtension(asset.assetPath);
						runtimeAddressField.value = $"lunarscape/field/{fileName}";
					};
				} else {
					runtimeAddressField.SetValueWithoutNotify(asset.runtimeAssetAddress);
				}

				row.AddChildren(guidLabel, assetPathLabel, runtimeAddressField);
				scrollView.Add(row);
			}
			view.AddChildren(titleRow, header, scrollView);
			return view;
		}

		private AssetInfo[] GetFieldAssetList(FieldEditorMode editMode) {
			System.Type type = editMode.EditorAssetType();

			string[] guids = AssetDatabase.FindAssets($"t:{type.Name}");
			AssetInfo[] assets = new AssetInfo[guids.Length];
			List<int> idList = new List<int>();
			List<FieldEditorAsset> newAssets = new List<FieldEditorAsset>();
			for(int i = 0; i < guids.Length; ++i) {
				string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
				FieldEditorAsset asset = AssetDatabase.LoadAssetAtPath<FieldEditorAsset>(assetPath);
				assets[i] = new AssetInfo {
					guid = guids[i],
					assetPath = assetPath,
					runtimeAssetAddress = asset.RuntimeAssetAddress,
				};

				if (asset.Id == int.MaxValue || idList.Contains(asset.Id)) {
					newAssets.Add(asset);
				} else {
					idList.Add(asset.Id);
				}
			}

			// idが振られていなければ振っておく
			if (newAssets.Count > 0) {
				idList.Sort();
				int current = 0;
				foreach(FieldEditorAsset asset in newAssets) {
					int i;
					for (i = 0; i < idList.Count; ++i) {
						if (idList[i] - current > 1) {
							break;
						}
						current = idList[i];
					}
					current++;
					idList.Insert(i, current);

					SerializedObject obj = new SerializedObject(asset);
					obj.FindProperty("_id").intValue = current;
					obj.ApplyModifiedProperties();
				}
			}

			return assets;
		}

		private class AssetInfo {
			public string guid;
			public string assetPath;
			public string runtimeAssetAddress;
		}
	}
}