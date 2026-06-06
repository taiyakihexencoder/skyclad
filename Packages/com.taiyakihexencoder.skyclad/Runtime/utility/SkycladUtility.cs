using UnityEngine;

namespace skyclad {
	using internalProc;

	public static partial class SkycladUtility {
		/// <summary>
		/// アプリケーションを終了する。
		/// </summary>
		public static void QuitApp() {
			SystemUtilityInternal.QuitApp();
		}

		public static AppVersion CurrentApplicationVersion {
			get {
				return AppVersion.Parse(Application.version);
			}
		}
	}
}