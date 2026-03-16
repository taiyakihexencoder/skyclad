using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace skyclad.editor {
	internal static class DioramaIdScriptGenerator {
		internal static void Generate(SerializedObject serializedObject) {
			SerializedProperty dioramasProperty = serializedObject.FindProperty("_diorama._units");
			SourceCodeGenerator gen = new SourceCodeGenerator();
			
			List<string> loadOnLaunchList = new List<string>();

			gen.AppendLine($"namespace skyclad {{");

			using(gen.IndentBlock) {
				gen.AppendLine($"public static partial class DioramaId {{");

				using(gen.IndentBlock) {
					for(int i = 0; i < dioramasProperty.arraySize; ++i) {
						SerializedProperty dioramaProperty = dioramasProperty.Of(i);
						string name = dioramaProperty.Of("name").stringValue;
						string visibleName = string.IsNullOrEmpty(name) ? $"Diorama{i.ToString("000")}" : name;
						gen.AppendLine($"public const int {visibleName} = {i};");

						if (dioramaProperty.Of("loadOnLaunch").boolValue) {
							loadOnLaunchList.Add(visibleName);
						}
					}

					gen.AppendLine($"");

					gen.AppendLine($"static partial void SetLaunchDioramas() {{");
					using (gen.IndentBlock) {
						gen.AppendLine($"_launchDioramas = new int[] {{");
						using (gen.IndentBlock) {
							foreach(string loadOnLaunch in loadOnLaunchList) {
								gen.AppendLine($"{loadOnLaunch},");
							}
						}
						gen.AppendLine($"}};");
					}
					gen.AppendLine($"}}");
				}

				gen.AppendLine($"}}");
			}

			gen.AppendLine($"}}");
			gen.Generate($"diorama{Path.DirectorySeparatorChar}DioramaId.cs");
		}
	}
}