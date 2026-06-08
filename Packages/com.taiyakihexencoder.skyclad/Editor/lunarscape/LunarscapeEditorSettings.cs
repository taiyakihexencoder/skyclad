using UnityEditor;
using UnityEngine;

namespace skyclad.lunarscape.editor {
	[FilePath("ProjectSettings/LunarscapeEditorSettings.json", FilePathAttribute.Location.ProjectFolder)]
	internal sealed class LunarscapeEditorSettings : ScriptableSingleton<LunarscapeEditorSettings> {
		internal static void Save() { instance.Save(true); }

		internal static LunarscapeEditorSettings of => instance;

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
	}
}