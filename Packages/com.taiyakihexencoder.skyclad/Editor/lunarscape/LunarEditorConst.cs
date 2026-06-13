using System.IO;

namespace skyclad.lunarscape.editor {
	public readonly struct LunarEditorConst {
#if UNITY_EDITOR_WIN
		public const string ASMREF_PATH = "Skyclad\\auto-generated\\Scripts";
		public const string AUTO_GENERATE_PATH = "Skyclad\\auto-generated\\Scripts\\lunarscape";
		public const string AUTO_GENERATE_RESOURCE_PATH = "Skyclad\\auto-generated\\res\\lunarscape";
#else
		public const string ASMREF_PATH = "Skyclad/auto-generated/Scripts";
		public const string AUTO_GENERATE_PATH = "Skyclad/auto-generated/Scripts/lunarscape";
		public const string AUTO_GENERATE_RESOURCE_PATH = "Skyclad/auto-generated/res/lunarscape";
#endif
	}
}