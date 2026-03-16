using System.IO;
using UnityEditor;

namespace skyclad.editor {
	internal static class CharacterIdScriptGenerator {
		internal static void Generate(SerializedObject serializedObject) {
			SourceCodeGenerator gen = new SourceCodeGenerator();
			SerializedProperty characterUnitsProperty = serializedObject.FindProperty("_character._units");

			gen.AppendLine($"namespace skyclad.character {{");

			using (gen.IndentBlock) {
				gen.AppendLine($"public static partial class CharacterId {{");

				using (gen.IndentBlock) {
					for (int i = 0; i < characterUnitsProperty.arraySize; ++i) {
						SerializedProperty characterUnitProperty = characterUnitsProperty.Of(i);
						string name = characterUnitProperty.Of("name").stringValue;
						if (!string.IsNullOrEmpty(name)) {
							gen.AppendLine($"public const int {name} = {i};");
						}
					}
				}

				gen.AppendLine($"}}");
			}

			gen.AppendLine($"}}");

			gen.Generate($"character{Path.DirectorySeparatorChar}CharacterId.cs");
		}
	}
}
