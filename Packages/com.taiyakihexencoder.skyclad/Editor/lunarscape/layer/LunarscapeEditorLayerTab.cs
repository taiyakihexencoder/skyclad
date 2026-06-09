using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace skyclad.lunarscape.editor {
	internal sealed class LunarscapeEditorLayerTab : VisualElement {
		public LunarscapeEditorLayerTab() {
			CreateLayout();
		}

		private void CreateLayout() {
			LunarscapeEditorSettings._Layer settings = LunarscapeEditorSettings.of.Layer;

			VisualElement mainFrame = LunarscapeCommonDesign.MainFrame();
			Add(mainFrame);

			mainFrame.Add(LunarscapeCommonDesign.Title("Layer"));

			mainFrame.Add(LunarscapeCommonDesign.Header("Collision"));

			Button button = new Button(
				clickEvent: () => {
					LayerScriptGenerator generator = new LayerScriptGenerator();
					if (generator.Validation(out string message)) {
						generator.Generate(LunarEditorConst.AUTO_GENERATE_PATH + Path.DirectorySeparatorChar + "SceneLayer.cs");
					} else {
						EditorUtility.DisplayDialog("Error", message, "ok");
					}
				}
			);
			button.text = "Generate Script";
			button.style.width = 120.0f;
			button.style.marginTop = 12f;
			button.style.marginBottom = 12f;
			mainFrame.Add(button);

			VisualElement table = new VisualElement();
			table.style.flexDirection = FlexDirection.Column;

			if (settings.Names.Length != 32 || settings.IndexTable.Length != 32 || settings.CollideTable.Length != 1024) {
				settings.Names = new string[32];
				settings.IndexTable = new int[32];
				settings.CollideTable = new bool[1024];
				for (int i = 0; i < 32; ++i) {
					settings.IndexTable[i] = i;
				}
				settings.Names[0] = "Terrain";
				settings.Names[1] = "PhysicsObject";
				settings.CollideTable[0] = true;
				settings.CollideTable[1] = true;
				settings.CollideTable[32] = true;
				LunarscapeEditorSettings.Save();
			}

			int[] reverseTable = new int[settings.IndexTable.Length];
			for(int i = 0; i < reverseTable.Length; ++i) {
				reverseTable[settings.IndexTable[i]] = i;
			}

			for (int i = 0; i < 32; ++i) {
				int leftIndex = reverseTable[i];
				int leftOffset = leftIndex * 32;

				VisualElement row = new VisualElement();
				row.style.flexDirection = FlexDirection.Row;
				TextField nameField = new TextField();
				nameField.enabledSelf = leftIndex >= 2;
				nameField.style.fontSize = 10f;
				nameField.style.width = 100f;
				nameField.style.height = 14f;
				nameField.style.paddingRight = 8f;
				nameField.style.marginTop = 0f;
				nameField.style.marginBottom = 0f;
				nameField.isDelayed = true;
				nameField.SetValueWithoutNotify(settings.Names[leftIndex]);
				nameField.RegisterValueChangedCallback(v => {
					settings.Names[leftIndex] = v.newValue;
					LunarscapeEditorSettings.Save();
					Clear();
					CreateLayout();
				});
				row.Add(nameField);

				Label label = new Label(i.ToString("00"));
				label.style.fontSize = 8f;
				label.style.width = 16f;
				label.style.unityTextAlign = TextAnchor.MiddleRight;
				row.Add(label);

				for(int j = 0; j <= i; ++j) {
					int rightIndex = reverseTable[j];
					int rightOffset = rightIndex * 32;
					Toggle toggle = new Toggle();
					toggle.style.flexGrow = 0f;
					toggle.enabledSelf = leftIndex >= 2 && rightIndex >= 2 && 
						!string.IsNullOrEmpty(settings.Names[leftIndex]) &&
						!string.IsNullOrEmpty(settings.Names[rightIndex]);
					toggle.style.marginLeft = 0f;
					toggle.style.marginRight = 0f;
					toggle.style.marginTop = 0f;
					toggle.style.marginBottom = 0f;
					toggle.SetValueWithoutNotify(settings.CollideTable[leftOffset + rightIndex]);
					toggle.RegisterValueChangedCallback(v => {
						settings.CollideTable[leftOffset + rightIndex] = v.newValue;
						settings.CollideTable[rightOffset + leftIndex] = v.newValue;
						LunarscapeEditorSettings.Save();
					});
					row.Add(toggle);
				}
				table.Add(row);
			}

			VisualElement nameRow = new VisualElement();
			nameRow.style.paddingTop = 4f;
			nameRow.style.paddingBottom = 80f;
			for (int i = 0; i < 32; ++i) {
				Label name = new Label(settings.Names[reverseTable[i]]);
				name.style.flexBasis = Length.Percent(0f);
				name.style.marginLeft = 123f + i * 14f;
				name.style.transformOrigin = new StyleTransformOrigin(new TransformOrigin(7.5f, 7.5f));
				name.style.rotate = new StyleRotate(new Rotate(90.0f, Vector3.forward));
				nameRow.Add(name);
			}
			table.Add(nameRow);
			mainFrame.Add(table);
		}
	}
}