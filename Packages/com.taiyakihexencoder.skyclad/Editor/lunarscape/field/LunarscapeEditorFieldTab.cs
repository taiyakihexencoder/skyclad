using UnityEngine.UIElements;

namespace skyclad.lunarscape.editor {
	internal sealed class LunarscapeEditorFieldTab : VisualElement {
		public LunarscapeEditorFieldTab() {
			CreateView();
		}

		private void CreateView() {
			LunarscapeEditorSettings settings = LunarscapeEditorSettings.of;

			VisualElement mainFrame = LunarscapeCommonDesign.MainFrame();
			mainFrame.Add(LunarscapeCommonDesign.Title("Field"));

			EnumField fieldEditTypeField = new EnumField("Field Editor Mode", settings.Field.FieldEditorMode);
			fieldEditTypeField.RegisterValueChangedCallback(v => {
				LunarscapeEditorSettings.of.Field.FieldEditorMode = (FieldEditorMode)v.newValue;
				LunarscapeEditorSettings.Save();
			});
			mainFrame.Add(fieldEditTypeField);

			switch(LunarscapeEditorSettings.of.Field.FieldEditorMode) {
				case FieldEditorMode.SideView: {
					SideViewSettings(settings, mainFrame);
					break;
				}
			}

			Add(mainFrame);
		}

		private void SideViewSettings(LunarscapeEditorSettings settings, VisualElement mainFrame) {
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

			mainFrame.Add(pane);
		}
	}
}