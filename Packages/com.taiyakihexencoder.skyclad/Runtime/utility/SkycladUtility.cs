using UnityEngine;

namespace skyclad {
	public static partial class SkycladUtility {
		/// <summary>
		/// アプリケーションを終了する。
		/// </summary>
		public static void QuitApp() {
#if UNITY_EDITOR
			Debug.Log("Quit Requested.");
			UnityEditor.EditorApplication.isPlaying = false;
#else
			Application.Quit();
#endif
		}
	}
}