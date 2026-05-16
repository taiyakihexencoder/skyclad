using System;
using System.Diagnostics;
using UnityEngine;

namespace skyclad {
	public class DebugScreen : MonoBehaviour {
#if UNITY_EDITOR
		private const int SLOTS = 10;
		private const uint DEFAULT_COLOR = 0x80000000;

		private static DebugScreen _instance = null;

		private GUIContent[] _slots = new GUIContent[SLOTS];
		private Color[] _colors = new Color[SLOTS];
		private GUIStyle _style;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void CreateInstance() {
			GameObject obj = new GameObject("Debug Screen");
			_instance = obj.AddComponent<DebugScreen>();
			_instance.ClearInternal();
			DontDestroyOnLoad(obj);
		}

		/// <summary>
		/// デバッグテキストを表示
		/// </summary>
		public static void SetVisible() {
			if (_instance == null) {
				GameObject obj = new GameObject("Debug Screen");
				_instance = obj.AddComponent<DebugScreen>();
				_instance.ClearInternal();
				DontDestroyOnLoad(obj);
			}
		}

		/// <summary>
		/// デバッグテキストを非表示
		/// </summary>
		public static void SetInvisible() {
			if (_instance != null) {
				Destroy(_instance.gameObject);
				_instance = null;
			}
		}

		private void OnGUI() {
			if (_style == null) {
				_style = new GUIStyle(GUI.skin.label);
				_style.fontSize = 20;
			}
			for(int i = 0; i < SLOTS; ++i) {
				GUI.color = _colors[i];
				GUI.Label(new Rect(20, 20 + i * 30, Screen.width - 40, 30), _slots[i], _style);
			}
		}

		private void SetTextInternal(int index, string text) {
			if (0 <= index && index < SLOTS) {
				_slots[index] = new GUIContent(text);
			}
		}

		private void SetColorInternal(int index, uint color) {
			if (0 <= index && index < SLOTS) {
				_colors[index] = new Color32(
					(byte)((color >> 16) & 255),
					(byte)((color >> 8) & 255),
					(byte)(color & 255),
					(byte)(color >> 24)
				);
			}
		}

		private void ClearInternal() {
			byte a = (byte)(DEFAULT_COLOR >> 24);
			byte r = (byte)((DEFAULT_COLOR >> 16) & 255);
			byte g = (byte)((DEFAULT_COLOR >> 8) & 255);
			byte b = (byte)(DEFAULT_COLOR & 255);

			for(int i = 0; i < SLOTS; ++i) {
				_slots[i] = new GUIContent();
				_colors[i] = new Color32(r, g, b, a);
			}
		}
#endif

		[Conditional("UNITY_EDITOR")]
		public static void SetText(int index, string text) {
#if UNITY_EDITOR
			_instance?.SetTextInternal(index, text);
#endif
		}

		[Conditional("UNITY_EDITOR")]
		public static void SetColor(int index, uint color) {
#if UNITY_EDITOR
			_instance?.SetColorInternal(index, color);
#endif
		}

		[Conditional("UNITY_EDITOR")]
		public static void Clear() {
#if UNITY_EDITOR
			_instance?.ClearInternal();
#endif
		}
	}

	
}