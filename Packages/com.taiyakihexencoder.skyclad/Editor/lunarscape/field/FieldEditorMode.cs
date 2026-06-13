namespace skyclad.lunarscape.editor {
	internal enum FieldEditorMode {
		/// <summary>
		/// サイドビュー用フィールド
		/// </summary>
		SideView,
	}

	internal static class FieldEditorModeExtensions {
		internal static System.Type EditorAssetType(this FieldEditorMode mode) {
			switch (mode) {
				case FieldEditorMode.SideView: return typeof(FieldSideViewAsset);
				default: return null;
			}
		}
	}
}