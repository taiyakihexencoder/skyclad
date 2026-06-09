using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace skyclad.lunarscape.editor {
	public sealed class LayerScriptGenerator : ScriptGenerator {
		public bool Validation(out string message) {
			LunarscapeEditorSettings._Layer settings = LunarscapeEditorSettings.of.Layer;

			StringBuilder sb = new StringBuilder();
			List<string> validated = new List<string>();
			Regex regex = new Regex(@"^[a-zA-Z_]+[a-zA-Z0-9_]*$");
			bool result = true;
			for (int i = 0; i < 32; ++i) {
				int idx = settings.IndexTable[i];
				if (string.IsNullOrEmpty(settings.Names[idx])) {
					// 対象外
				} else if (validated.Contains(settings.Names[idx])) {
					// 名前の重複
					result = false;
					sb.Append($"Duplicated:{settings.Names[idx]}{System.Environment.NewLine}");
				} else if (regex.Match(settings.Names[idx]).Success) {
					validated.Add(settings.Names[idx]);
				} else {
					// 使用できない名前
					sb.Append($"Invalid Name:{settings.Names[idx]}{System.Environment.NewLine}");
					result = false;
				}
			}
			message = sb.ToString();
			return result;
		}

		protected override void WriteScript() {
			LunarscapeEditorSettings._Layer settings = LunarscapeEditorSettings.of.Layer;

			using (Namespace("skyclad.lunarscape")) {
				using (Class("SceneLayer", isPartial: true)) {
					for (int i = 0; i < 32; ++i) {
						int idx = settings.IndexTable[i];

						// 0と1は固定レイヤー
						if (idx < 2) { continue; }
						if (string.IsNullOrEmpty(settings.Names[idx])) { continue; }

						uint value = 1u << i;
						AppendLine($"public const uint {settings.Names[idx]} = {value}u;");
					}
				}
				AppendLine();
				using (Class("SceneLayerCollidesWith", isPartial: true)) {
					List<string> layers = new List<string>();
					for (int i = 0; i < 32; ++i) {
						int idx = settings.IndexTable[i];

						if (idx < 2) { continue; }
						if (string.IsNullOrEmpty(settings.Names[idx])) { continue; }

						int leftOffset = idx * 32;
						layers.Clear();
						for (int j = 0; j < 32; ++j) {
							int idx2 = settings.IndexTable[j];

							if (!string.IsNullOrEmpty(settings.Names[idx2]) && 
								settings.CollideTable[leftOffset + idx2]) {
								layers.Add($"SceneLayer.{settings.Names[idx2]}");
							}
						}
						AppendLine($"public const uint {settings.Names[idx]} = {string.Join(" | ", layers)};");
					}
				}
			}
		}
	}
}