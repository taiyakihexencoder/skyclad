using UnityEngine;

namespace skyclad.lifecycle {
	/// <summary>
	/// アプリケーション終了時の処理を実装
	/// </summary>
	internal static class OnQuit {
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void RegisterOnQuit() {
			Application.quitting += OnQuitHandler;
		}

		private static void OnQuitHandler() {
			SkycladDataTables.Dispose();
			character.CharacterCollider.DisposeColliders();

			SkycladUtility.Resource.UnloadAll();
			field.FieldMeshLoader.DisposeAllResource();

			Debug.Log("Quit.");
		}
	}
}