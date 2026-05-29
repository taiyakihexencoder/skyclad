using UnityEngine;

namespace skyclad.internalProc {
	public static class D {
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void Log(object msg) {
			Debug.Log(msg);
		}

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void LogW(object msg) {
			Debug.LogWarning(msg);
		}

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void LogE(object msg) {
			Debug.LogError(msg);
		}
	}
}