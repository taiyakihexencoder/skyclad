using UnityEditor;
using UnityEngine.UIElements;

namespace skyclad.lunarscape.editor {
	using skyclad.lunarscape.internalProc;
	internal sealed class LunarscapeEditorAvatarTab : VisualElement {
		private SerializedObject _runtimeObj;

		public LunarscapeEditorAvatarTab() {
			_runtimeObj = new SerializedObject(LunarscapeSettings.Get());
			CreateView();
		}

		private void CreateView() {
			LunarscapeEditorSettings settings = LunarscapeEditorSettings.of;
			LunarscapeSettings runtime = LunarscapeSettings.Get();

			VisualElement mainFrame = LunarscapeCommonDesign.MainFrame();
			mainFrame.Add(LunarscapeCommonDesign.Title("Avatar"));

			TabView tabView = new TabView();
			tabView.Add(AvatarTab());
			tabView.Add(ColliderTab());

			mainFrame.Add(tabView);

			Add(mainFrame);
		}

		private Tab AvatarTab() {
			Tab tab = LunarscapeCommonDesign.TabFrame("Avatar Settings");

			string[] guids = AssetDatabase.FindAssets($"t:{typeof(AvatarEditorAsset).Name}");
			SerializedObject[] assets = new SerializedObject[guids.Length];
			for(int i = 0; i < guids.Length; ++i) {
				string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
				AvatarEditorAsset scriptable = AssetDatabase.LoadAssetAtPath<AvatarEditorAsset>(assetPath);
				assets[i] = new SerializedObject(scriptable);
			}

			Button addButton = new Button(
				clickEvent: () => {

				}
			);
			addButton.text = "+";
			addButton.style.width = 24.0f;
			tab.Add(addButton);

			return tab;
		}

		private Tab ColliderTab() {
			Tab tab = LunarscapeCommonDesign.TabFrame("Collider Settings");

			string[] guids = AssetDatabase.FindAssets($"t:{typeof(AvatarEditorColliderAsset).Name}");
			SerializedObject[] assets = new SerializedObject[guids.Length];
			for(int i = 0; i < guids.Length; ++i) {
				string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
				AvatarEditorColliderAsset scriptable = AssetDatabase.LoadAssetAtPath<AvatarEditorColliderAsset>(assetPath);
				assets[i] = new SerializedObject(scriptable);
			}

			Button addButton = new Button(
				clickEvent: () => {

				}
			);
			addButton.text = "+";
			addButton.style.width = 24.0f;
			tab.Add(addButton);

			return tab;
		}
	}
}