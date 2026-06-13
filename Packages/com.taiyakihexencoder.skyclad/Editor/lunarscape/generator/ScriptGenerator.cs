using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace skyclad.lunarscape.editor {
	public abstract class ScriptGenerator {
		protected IGeneratorStream stream;

		public ScriptGenerator() {
			stream = new GeneratorStream();
		}

		protected IndentScope Indent {
			get {
				stream.AddIndent();
				return new IndentScope(() => stream.RemoveIndent());
			}
		}
		protected IndentScope Class(
			string name,
			bool isPartial = false,
			bool isStatic = false
		) {
			string header = "";
			header += "public ";
			if (isStatic) {
				header += "static ";
			}
			if (isPartial) {
				header += "partial ";
			}
			stream.AppendLine($"{header}class {name} {{");
			stream.AddIndent();
			return new IndentScope(() => {
				stream.RemoveIndent();
				stream.AppendLine($"}}");
			});
		}

		protected IndentScope Struct(
			string name,
			bool isPartial = false,
			bool isStatic = false,
			bool isReadonly = false
		) {
			string header = "";
			header += "public ";
			if (isStatic) {
				header += "static ";
			}
			if (isReadonly) {
				header += "readonly ";
			}
			if (isPartial) {
				header += "partial ";
			}
			stream.AppendLine($"{header}struct {name} {{");
			stream.AddIndent();
			return new IndentScope(() => {
				stream.RemoveIndent();
				stream.AppendLine($"}}");
			});
		}

		protected IndentScope Namespace(string name) {
			stream.AppendLine($"namespace {name} {{");
			stream.AddIndent();
			return new IndentScope(() => {
				stream.RemoveIndent();
				stream.AppendLine($"}}");
			});
		}

		protected IndentScope Function(string name) {
			stream.AppendLine($"{name} {{");
			stream.AddIndent();
			return new IndentScope(() => {
				stream.RemoveIndent();
				stream.AppendLine($"}}");
			});
		}

		protected void Append(string text, bool indent = true) {
			stream.Append(text, indent);
		}

		protected void AppendLine(string text, bool indent = true) {
			stream.AppendLine(text, indent);
		}

		protected void AppendLine() {
			stream.AppendLine("", false);
		}

		public void Generate(string pathFromAssets) {
			WriteScript();
			stream.Generate(pathFromAssets);

			if (!IsExistAsmref()) {
				CreateAsmref();
			}
		}

		protected abstract void WriteScript();

		protected class IndentScope : System.IDisposable {
			public System.Action onClose;

			public IndentScope(System.Action onClose) {
				this.onClose = onClose;
			}

			void System.IDisposable.Dispose() {
				onClose();
			}
		}

		protected interface IGeneratorStream {
			void AddIndent();
			void RemoveIndent();
			void Append(string text, bool indent = true);
			void AppendLine(string text, bool indent = true);
			void Generate(string pathFromAssets);
		}

		private class GeneratorStream : IGeneratorStream{
			private int _indent;
			private StringBuilder _sb;

			public GeneratorStream() {
				_indent = 0;
				_sb = new StringBuilder();
			}

			void IGeneratorStream.AddIndent() {
				_indent++;
			}

			void IGeneratorStream.RemoveIndent() {
				_indent--;
				if (_indent < 0) { _indent = 0; }
			}

			void IGeneratorStream.Append(string text, bool indent) {
				if (indent && _indent > 0) {
					_sb.Append(new string('\t', _indent));
				}
				_sb.Append(text);
			}

			void IGeneratorStream.AppendLine(string text, bool indent) {
				if (indent && _indent > 0) {
					_sb.Append(new string('\t', _indent));
				}
				_sb.Append(text);
				_sb.Append(System.Environment.NewLine);
			}

			void IGeneratorStream.Generate(string pathFromAssets) {
				string absPath = Application.dataPath + Path.DirectorySeparatorChar + pathFromAssets;
				try {
					// フォルダ作成
					string[] splits = pathFromAssets.Split(Path.DirectorySeparatorChar);
					string basePath = Application.dataPath;
					for (int i = 0; i < splits.Length -1; ++i) {
						basePath += $"{Path.DirectorySeparatorChar}{splits[i]}";
						if (!Directory.Exists(basePath)) {
							Directory.CreateDirectory(basePath);
						}
					}

					using (FileStream stream = new FileStream(absPath, FileMode.Create, FileAccess.Write)) {
						using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8)) {
							writer.Write(_sb.ToString());
						}
					}
					AssetDatabase.ImportAsset("Assets" + Path.DirectorySeparatorChar + pathFromAssets);
				} catch (System.Exception e) {
					EditorUtility.DisplayDialog(
						title: "Error",
						message: "Failed: Script Generator stopped.",
						ok: "Ok"
					);
					Debug.LogError(e);
				}
			}

		}

		protected bool IsExistAsmref() {
			return File.Exists(
				Application.dataPath + Path.DirectorySeparatorChar + 
				LunarEditorConst.ASMREF_PATH + Path.DirectorySeparatorChar + 
				"skyclad.asmref"
			);
		}

		protected void CreateAsmref() {
			string genPath = LunarEditorConst.ASMREF_PATH +
				Path.DirectorySeparatorChar +
				"skyclad.asmref";
			string asmrefPath = Application.dataPath +
				Path.DirectorySeparatorChar +
				genPath;
			
			string[] splits = asmrefPath.Split(Path.DirectorySeparatorChar);
			string basePath = Application.dataPath;
			for (int i = 0; i < splits.Length -1; ++i) {
				basePath += $"{Path.DirectorySeparatorChar}{splits[i]}";
				if (!Directory.Exists(basePath)) {
					Directory.CreateDirectory(basePath);
				}
			}

			try {
				using (FileStream stream = new FileStream(asmrefPath, FileMode.Create, FileAccess.Write)) {
					using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8)) {
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