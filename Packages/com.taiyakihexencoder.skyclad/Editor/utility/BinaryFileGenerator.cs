using System.IO;
using UnityEditor;
using UnityEngine;

namespace skyclad.editor {
	public static class BinaryFileGenerator {
		public delegate void WriteFunction(BinaryWriter writer);

		public static void Generate(WriteFunction function, string path) {
			string[] splits = path.Split(Path.DirectorySeparatorChar);

			string basePath = Application.streamingAssetsPath;
			for (int i = 0; i < splits.Length -1; ++i) {
				basePath += $"{Path.DirectorySeparatorChar}{splits[i]}";
				if (!Directory.Exists(basePath)) {
					Directory.CreateDirectory(basePath);
				}
			}

			string absPath = Application.streamingAssetsPath +
				Path.DirectorySeparatorChar +
				path;

			try {
				using (FileStream stream = new FileStream(absPath, FileMode.Create, FileAccess.Write)) {
					using (BinaryWriter writer = new BinaryWriter(stream, System.Text.Encoding.UTF8)) {
						function(writer);
					}
				}
				Debug.Log($"Generated:{absPath}");

			} catch (System.Exception e) {
				EditorUtility.DisplayDialog(
					title: "Error",
					message: "BinaryFileGenerator.Generate stopped. Causes should be logged on console.",
					ok: "Ok"
				);
				Debug.LogError(e);
			}
		}
	}
}