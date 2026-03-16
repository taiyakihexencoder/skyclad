using System.IO;
using UnityEditor;
using UnityEngine;

namespace skyclad.editor {
	public sealed class SourceCodeGenerator {
		private class IndentScope : System.IDisposable {
			private SourceCodeGenerator gen;

			public IndentScope(SourceCodeGenerator gen) {
				this.gen = gen;
				gen.AddIndent();
			}

			void System.IDisposable.Dispose() {
				gen.RemoveIndent();
			}
		}

		private int _indent;

		private string script;

		public SourceCodeGenerator() {
			_indent = 0;
			script = "";
		}

		public void AddIndent() {
			_indent++;
		}

		public void RemoveIndent() {
			_indent--;
		}

		public System.IDisposable IndentBlock => new IndentScope(this);

		public void Append(string text, bool indent = false) {
			string indents = indent ? new string('\t', _indent) : "";
			script += indents + text.Replace(System.Environment.NewLine, $"{System.Environment.NewLine}{indents}");
		}

		public void AppendLine(string text) {
			Append(text, indent: true);
			script += System.Environment.NewLine;
		}

		public void Generate(string path, bool checkAssembly = true) {
			string genPath = GlobalProjectSettings.AutoGenerateScriptPath +
				Path.DirectorySeparatorChar +
				path;

			string absPath = Application.dataPath + Path.DirectorySeparatorChar + genPath;
			string relPath = "Assets" + Path.DirectorySeparatorChar + genPath;

			string basePath = Application.dataPath;
			string[] splits = genPath.Split(Path.DirectorySeparatorChar);
			for(int i = 0; i < splits.Length - 1; ++i) {
				basePath += $"{Path.DirectorySeparatorChar}{splits[i]}";
				if (!Directory.Exists(basePath)) {
					Directory.CreateDirectory(basePath);
				}
			}

			try {
				using (FileStream stream = new FileStream(absPath, FileMode.Create, FileAccess.Write)) {
					using (StreamWriter writer = new StreamWriter(stream, System.Text.Encoding.UTF8)) {
						writer.Write(script);
					}
				}
				AssetDatabase.ImportAsset(relPath);

				if (checkAssembly) {
					CreateAsmref();
				}

				Debug.Log($"Generated:{relPath}");
			} catch (System.Exception e){
				EditorUtility.DisplayDialog(
					title: "Error",
					message: "SourceCodeGenerator.Generate stopped. Causes should be logged on console.",
					ok: "Ok"
				);
				Debug.LogError(e);
			}
		}

		private void CreateAsmref() {
			string genPath = GlobalProjectSettings.AutoGenerateScriptPath +
				Path.DirectorySeparatorChar +
				"skyclad.asmref";
			string asmrefPath = Application.dataPath +
				Path.DirectorySeparatorChar +
				genPath;
				
			if (!File.Exists(asmrefPath)) {
				try {
					if (File.Exists(asmrefPath)) { return; }

					using (FileStream stream = new FileStream(asmrefPath, FileMode.Create, FileAccess.Write)) {
						using (StreamWriter writer = new StreamWriter(stream, System.Text.Encoding.UTF8)) {
							writer.WriteLine($"{{");
							writer.WriteLine($"\t\"reference\": \"skyclad\"");
							writer.WriteLine($"}}");
						}
					}

					AssetDatabase.ImportAsset($"Assets{Path.DirectorySeparatorChar}{genPath}");
				}
				catch (System.Exception e) {
					Debug.LogError(e);
				}
			}
		}
	}
}