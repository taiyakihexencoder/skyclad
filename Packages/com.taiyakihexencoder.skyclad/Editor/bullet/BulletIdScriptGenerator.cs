using System.IO;
using UnityEditor;

namespace skyclad.editor {
	internal static class BulletIdScriptGenerator {
		internal static void Generate(SerializedObject serializedObject) {
			SourceCodeGenerator gen = new SourceCodeGenerator();
			SerializedProperty groupsProperty = serializedObject.FindProperty("_bullet._groups");

			gen.AppendLine($"namespace skyclad.bullet {{");

			using (gen.IndentBlock) {
				gen.AppendLine($"public static partial class BulletGroupId {{");
				using (gen.IndentBlock) {
					for (int groupIndex = 0; groupIndex < groupsProperty.arraySize; ++groupIndex) {
						SerializedProperty groupProperty = groupsProperty.Of(groupIndex);
						string groupName = groupProperty.Of("name").stringValue;
						gen.AppendLine($"public const int {groupName} = {groupIndex+1}000;");
					}
				}
				gen.AppendLine($"}}");
				gen.AppendLine($"");

				gen.AppendLine($"public static partial class BulletId {{");

				using (gen.IndentBlock) {
					for (int groupIndex = 0; groupIndex < groupsProperty.arraySize; ++groupIndex) {
						SerializedProperty groupProperty = groupsProperty.Of(groupIndex);
						string groupName = groupProperty.Of("name").stringValue;

						SerializedProperty unitsProperty = groupProperty.Of("units");
						for(int unitIndex = 0; unitIndex < unitsProperty.arraySize; ++unitIndex) {
							SerializedProperty unitProperty = unitsProperty.Of(unitIndex);
							string name = unitProperty.Of("name").stringValue;
							if (!string.IsNullOrEmpty(name)) {
								gen.AppendLine($"public const int {groupName}_{name} = {groupIndex+1}{unitIndex+1:000};");
							}
						}
					}
				}
				gen.AppendLine($"}}");

				gen.AppendLine($"");

				gen.AppendLine($"public static partial class BulletDataIndex {{");
				using (gen.IndentBlock) {
					int dataIndex = 0;
					for (int groupIndex = 0; groupIndex < groupsProperty.arraySize; ++groupIndex) {
						SerializedProperty groupProperty = groupsProperty.Of(groupIndex);
						string groupName = groupProperty.Of("name").stringValue;						
						SerializedProperty unitsProperty = groupProperty.Of("units");
						for(int unitIndex = 0; unitIndex < unitsProperty.arraySize; ++unitIndex) {
							SerializedProperty unitProperty = unitsProperty.Of(unitIndex);
							string name = unitProperty.Of("name").stringValue;
							if (!string.IsNullOrEmpty(name)) {
								gen.AppendLine($"public const int {groupName}_{name}_DataIndex = {dataIndex};");
								dataIndex++;
							}
						}
					}
				}
				gen.AppendLine($"}}");
			}

			gen.AppendLine($"}}");
			gen.Generate($"bullet{Path.DirectorySeparatorChar}BulletId.cs");
		}
	}
}