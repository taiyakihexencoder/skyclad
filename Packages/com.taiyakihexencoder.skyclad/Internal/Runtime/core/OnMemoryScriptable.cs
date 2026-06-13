#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

using UnityEngine;

namespace skyclad.internalProc {
	public abstract class OnMemoryScriptable<T> : ScriptableObject where T : OnMemoryScriptable<T> {
		private static T _instance = null;
		protected abstract string assetPath { get; }

		public static T Get() {
			if (_instance == null) {
#if UNITY_EDITOR
				Object[] preloads = PlayerSettings.GetPreloadedAssets();
				foreach(Object asset in PlayerSettings.GetPreloadedAssets()) {
					if (asset is T instance) {
						_instance = instance;
						return _instance;
					}
				}
				_instance = CreateInstance<T>();
				CreateAsset(_instance);
				RegisterAsPreload(_instance);
				AssetDatabase.SaveAssets();
#else
				_instance = CreateInstance<T>();
#endif
			}
			return _instance;
		}

		private void OnEnable() {
			_instance = this as T;
		}
#if UNITY_EDITOR

		private static void CreateAsset(T asset) {
			string basePath = Application.dataPath;
			string path = asset.assetPath;
			string[] splits = path.Split(new char[] { '/', '\\', ':' });
			for (int i = 0; i < splits.Length-1; ++i) {
				basePath += $"{Path.DirectorySeparatorChar}{splits[i]}";
				if (!Directory.Exists(basePath)) {
					Directory.CreateDirectory(basePath);
				}
			}
			Debug.Log($"Assets{Path.DirectorySeparatorChar}{path}");
			AssetDatabase.CreateAsset(asset, $"Assets{Path.DirectorySeparatorChar}{path}");
		}

		private static void RegisterAsPreload(T asset) {
			Object[] preloads = PlayerSettings.GetPreloadedAssets();
			Object[] updatedPreloads = new Object[preloads.Length+1];
			System.Array.Copy(preloads, updatedPreloads, preloads.Length);

			updatedPreloads[preloads.Length] = asset;
			PlayerSettings.SetPreloadedAssets(updatedPreloads);
		}
#endif
	}
}