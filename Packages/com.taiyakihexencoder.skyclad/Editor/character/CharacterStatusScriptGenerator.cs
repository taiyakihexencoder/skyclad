using System.IO;
using UnityEditor;

namespace skyclad.editor {
	internal static class CharacterStatusScriptGenerator {
		internal static void Generate(SerializedObject serializedObject) {
  			SerializedProperty statusParameterUnitsProperty = serializedObject.FindProperty("_character._statusParameterUnits");

			SourceCodeGenerator gen = new SourceCodeGenerator();
			gen.AppendLine($"namespace skyclad {{");
			using (gen.IndentBlock) {
				gen.AppendLine($"public partial struct CharacterStatus {{");
				using (gen.IndentBlock) {
					for (int i = 0; i < statusParameterUnitsProperty.arraySize; ++i) {
						SerializedProperty statusParameterUnitProperty = statusParameterUnitsProperty.GetArrayElementAtIndex(i);
						ParameterType statusType = (ParameterType) statusParameterUnitProperty.FindPropertyRelative("parameterType").intValue;
						string name = statusParameterUnitProperty.FindPropertyRelative("name").stringValue;
						string type = statusType.GetParameterTypeName();
						gen.AppendLine($"[Order({i})]");
						gen.AppendLine($"public {type} {name};");
					}
				}
				gen.AppendLine($"}}");
			}
			gen.AppendLine($"}}");

			gen.Generate($"character{Path.DirectorySeparatorChar}CharacterStatus.cs");
		}
	}
}