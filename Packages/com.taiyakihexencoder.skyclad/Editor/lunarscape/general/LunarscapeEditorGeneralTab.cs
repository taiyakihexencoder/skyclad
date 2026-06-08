using UnityEngine.UIElements;

namespace skyclad.lunarscape.editor {
	public class LunarscapeEditorGeneralTab : VisualElement {
		public LunarscapeEditorGeneralTab() {
			VisualElement mainFrame = LunarscapeCommonDesign.MainFrame();
			mainFrame.Add(LunarscapeCommonDesign.Title("General"));
			Add(mainFrame);
		}
	}
}