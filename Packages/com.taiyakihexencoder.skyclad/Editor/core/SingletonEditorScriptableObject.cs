using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace skyclad.editor {
	internal abstract class SingletonEditorScriptableObject<T> : ScriptableObject where T : SingletonEditorScriptableObject<T> {
		protected abstract string generatePath { get; }

		private static T _instance;
		internal static T Instance {
			get {
				if (_instance == null) {
					string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
					if (guids.Length > 0) {
						if (guids.Length > 1) {
							List<string> assetPaths = new List<string>();
							foreach(string guid in guids) {
								assetPaths.Add(AssetDatabase.GUIDToAssetPath(guid));
							}
							Debug.LogWarning($"Multiple SingletonEditorScriptableObject Found:\n\t - {string.Join("\n\t - ", assetPaths)}");
						}

						string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
						_instance = AssetDatabase.LoadAssetAtPath<T>(assetPath);
					} else {
						_instance = CreateInstance<T>();
						SkycladEditorUtility.Asset.Create(_instance, _instance.generatePath);
					}
				}
				return _instance;
			}
		}
	}
}