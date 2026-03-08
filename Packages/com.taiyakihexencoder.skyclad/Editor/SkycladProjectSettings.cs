using System.IO;
using UnityEditor;
using UnityEngine;

namespace skyclad.editor {
	/// <summary>
	/// Skycladの機能に関するプロジェクト設定
	/// </summary>
	internal sealed class SkycladProjectSettings : ScriptableObject {
		private static string _generatePath = 
			"Assets" +
			Path.DirectorySeparatorChar +
			GlobalProjectSettings.AutoGeneratePath + 
			Path.DirectorySeparatorChar +
			"SkycladProjectSettings.asset";

		private static SkycladProjectSettings _instance;
		internal static SkycladProjectSettings Instance {
			get {
				if (_instance == null) {
					string[] guids = AssetDatabase.FindAssets($"t:{typeof(SkycladProjectSettings).Name}");
					if (guids.Length > 0) {
						string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
						_instance = AssetDatabase.LoadAssetAtPath<SkycladProjectSettings>(assetPath);
					} else {
						_instance = CreateInstance<SkycladProjectSettings>();
						SkycladEditorUtility.CreateAsset(_instance, _generatePath);
					}
				}
				return _instance;
			}
		}

		[SerializeField]
		private GlobalProjectSettings _global;
		public GlobalProjectSettings Global => _global;

		[SerializeField]
		private DioramaProjectSettings _diorama;
		internal DioramaProjectSettings Diorama => _diorama;

		[SerializeField]
		private CharacterProjectSettings _character;
		internal CharacterProjectSettings Character => _character;
	}

	[CustomEditor(typeof(SkycladProjectSettings))]
	internal sealed class SkycladProjectSettingsEditor : Editor {
		public override void OnInspectorGUI(){
			// Inspectorから変更できないようにしておく
			using(new EditorGUI.DisabledScope(disabled: true)) {
				base.OnInspectorGUI();
			}
		}
	}
}
