using UnityEditor;

namespace skyclad.lunarscape.editor {
	[FilePath("ProjectSettings/LunarscapeEditorSettings.json", FilePathAttribute.Location.ProjectFolder)]
	internal sealed class LunarscapeEditorSettings : ScriptableSingleton<LunarscapeEditorSettings> {
		internal static void Save() { instance.Save(true); }

		internal static LunarscapeEditorSettings of => instance;

		// -- Field -- //

		[System.Serializable]
		internal sealed class _Field {
			public FieldEditorMode FieldEditorMode { get; set; } = FieldEditorMode.SideView;

			[System.Serializable]
			public class _SideView {
				public float Width { get; set; } = 10.0f;
				public float ZOffset { get; set; } = 0.0f;
			}

			public _SideView SideView { get; set; } = new _SideView();
		}
		public _Field Field { get; set; } = new _Field();

		// -- Layer -- //

		[System.Serializable]
		internal sealed class _Layer {
			// レイヤー名
			public string[] Names { get; set; } = new string[0];
			// レイヤー番号とNamesの対応関係
			public int[] IndexTable { get; set; } = new int[0];
			// レイヤー番号と接触の対応関係
			public bool[] CollideTable { get; set; } = new bool[0];
		}
		public _Layer Layer { get; set; } = new _Layer();
	}
}