using System.IO;
using UnityEditor;
using UnityEngine;

namespace skyclad.editor {
	public static class OpenStreamingAssetPathMenuItem {
 		[MenuItem("Skyclad/Utility/Open Streaming Asset Path")]
		public static void OpenStreamingAssetPath() {
			EditorUtility.RevealInFinder(Application.streamingAssetsPath + Path.DirectorySeparatorChar);
		}
	}
}