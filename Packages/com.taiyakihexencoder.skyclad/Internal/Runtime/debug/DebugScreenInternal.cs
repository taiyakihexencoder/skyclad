using System.Diagnostics;
using UnityEngine;

namespace skyclad.internalProc {
	public class DebugScreenInternal : MonoBehaviour {
		private const int SLOTS = 10;
		private const uint DEFAULT_COLOR = 0x80000000;

		public static DebugScreenInternal instance = null;

		private GUIContent[] _slots = new GUIContent[SLOTS];
		private Color[] _colors = new Color[SLOTS];
		private GUIStyle _style;

		public static void CreateInstance() {
#if UNITY_EDITOR
			GameObject obj = new GameObject("Debug Screen");
			instance = obj.AddComponent<DebugScreenInternal>();
			instance.Clear();
			DontDestroyOnLoad(obj);
#endif
		}

		public static void SetVisible() {
#if UNITY_EDITOR
			if (instance == null) { CreateInstance(); }
#endif
		}

		public static void SetInvisible() {
#if UNITY_EDITOR
			if (instance != null) {
				Destroy(instance.gameObject);
				instance = null;
			}
#endif
		}

		[Conditional("UNITY_EDITOR")]
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

		public void SetText(int index, string text) {
#if UNITY_EDITOR
			if (0 <= index && index < SLOTS) {
				_slots[index] = new GUIContent(text);
			}
#endif
		}

		public void SetColor(int index, uint color) {
#if UNITY_EDITOR
			if (0 <= index && index < SLOTS) {
				_colors[index] = new Color32(
					(byte)((color >> 16) & 255),
					(byte)((color >> 8) & 255),
					(byte)(color & 255),
					(byte)(color >> 24)
				);
			}
#endif
		}

		public void Clear() {
#if UNITY_EDITOR
			byte a = (byte)(DEFAULT_COLOR >> 24);
			byte r = (byte)((DEFAULT_COLOR >> 16) & 255);
			byte g = (byte)((DEFAULT_COLOR >> 8) & 255);
			byte b = (byte)(DEFAULT_COLOR & 255);

			for(int i = 0; i < SLOTS; ++i) {
				_slots[i] = new GUIContent();
				_colors[i] = new Color32(r, g, b, a);
			}
#endif
		}
	}
}