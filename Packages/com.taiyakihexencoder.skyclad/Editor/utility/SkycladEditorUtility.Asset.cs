using System.IO;
using UnityEditor;
using UnityEngine;

namespace skyclad.editor {
	public static partial class SkycladEditorUtility {
		public static class Asset {
			/// <summary>
			/// アセットを作成する。
			/// Starts with "Assets"
			/// </summary>
			/// <param name="asset"></param>
			/// <param name="path"></param>
			public static void Create(Object asset, string path) {
				string basePath = Application.dataPath;
				string[] splits = path.Split(new char[] { '/', '\\', ':' });
				if (splits.Length == 0 || splits[0] != "Assets") {
					Debug.LogWarning($"Path must start with \"Assets\". path=\"{path}\".");
					return;
				}

				for (int i = 1; i < splits.Length-1; ++i) {
					basePath += $"{Path.DirectorySeparatorChar}{splits[i]}";
					if (!Directory.Exists(basePath)) {
						Directory.CreateDirectory(basePath);
					}
				}
				AssetDatabase.CreateAsset(asset, path);
			}

			/// <summary>
			/// Unity Editorのアイコンをスプライトとして取得する
			/// </summary>
			/// <param name="name"></param>
			/// <returns></returns>
			public static Sprite GetDefaultIconSprite(string name) {
				Texture2D texture = EditorGUIUtility.IconContent(name).image as Texture2D;
				if (texture == null) {
					Debug.LogError($"Failed to get sprite:{name}");
					return null;
				} else {
					return Sprite.Create(
						texture,
						new Rect(0f, 0f, texture.width, texture.height),
						new Vector2(0.5f, 0.5f)
					);
				}
			}
		}
	}
}