using System.Diagnostics;
using UnityEngine;

namespace skyclad {
	using internalProc;
	public class DebugScreen {
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void CreateInstance() { DebugScreenInternal.CreateInstance(); }

		/// <summary>
		/// デバッグテキストを表示
		/// </summary>
		[Conditional("UNITY_EDITOR")]
		public static void SetVisible() { DebugScreenInternal.SetVisible(); }

		/// <summary>
		/// デバッグテキストを非表示
		/// </summary>
		[Conditional("UNITY_EDITOR")]
		public static void SetInvisible() { DebugScreenInternal.SetInvisible(); }

		[Conditional("UNITY_EDITOR")]
		public static void SetText(int index, string text) { DebugScreenInternal.instance.SetText(index, text); }

		[Conditional("UNITY_EDITOR")]
		public static void SetColor(int index, uint color) { DebugScreenInternal.instance.SetColor(index, color); }

		[Conditional("UNITY_EDITOR")]
		public static void Clear() { DebugScreenInternal.instance.Clear(); }
	}

	
}