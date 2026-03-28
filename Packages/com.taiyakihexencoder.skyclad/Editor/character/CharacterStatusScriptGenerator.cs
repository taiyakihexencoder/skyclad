using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace skyclad.editor {
	internal static class CharacterStatusScriptGenerator {
		internal static void Generate(SerializedObject serializedObject) {
  			SerializedProperty statusParameterUnitsProperty = serializedObject.FindProperty("_character._statusParameterUnits");

			SerializedProperty dynamicParametersProperty = serializedObject.FindProperty("_character._dynamicParameters");
			List<(string, string)> dynamicParameterNames = new List<(string, string)>();
			for(int i = 0; i < dynamicParametersProperty.arraySize; ++i) {
				string guid = dynamicParametersProperty.Of(i).stringValue;
				for (int j = 0; j < statusParameterUnitsProperty.arraySize; ++j) {
					SerializedProperty statusParameterUnitProperty = statusParameterUnitsProperty.Of(j);
					if (guid == statusParameterUnitProperty.Of("guid").stringValue) {
						ParameterType parameterType = (ParameterType) statusParameterUnitProperty.Of("parameterType").intValue;
						dynamicParameterNames.Add(
							(statusParameterUnitProperty.Of("name").stringValue, parameterType.GetParameterTypeName())
						);
						break;
					}
				}
			}

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

				gen.AppendLine($"");

				gen.AppendLine($"public partial struct CharacterStatusComponent {{");
				using (gen.IndentBlock) {
					foreach((string parameterName, string parameterType) in dynamicParameterNames) {
						gen.AppendLine($"public {parameterType} {parameterName};");
					}
				}
				gen.AppendLine($"}}");
			}
			gen.AppendLine($"}}");

			gen.Generate($"character{Path.DirectorySeparatorChar}CharacterStatus.cs");
		}
	}
}