using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace skyclad.editor {
	internal static class DioramaUnloadJobScriptGenerator {
		internal static void Generate(SerializedObject serializedObject) {
			SerializedProperty dioramasProperty = serializedObject.FindProperty("_diorama._units");
			Dictionary<string, string> characterTable = GetCharacterTable(serializedObject);
			SourceCodeGenerator gen = new SourceCodeGenerator();
			gen.AppendLine($"using Unity.Entities;");
			gen.AppendLine($"");
			gen.AppendLine($"namespace skyclad {{");

			using (gen.IndentBlock) {
				gen.AppendLine("using character;");
				gen.AppendLine("using field;");
				gen.AppendLine($"public partial struct UnloadDioramaJob {{");

				using (gen.IndentBlock) {
					gen.AppendLine($"partial void Unload(EntityCommandBuffer commandBuffer, int dioramaId) {{");

					using (gen.IndentBlock) {
						gen.AppendLine($"switch (dioramaId) {{");

						using (gen.IndentBlock) {
							for (int i = 0; i < dioramasProperty.arraySize; ++i) {
								SerializedProperty dioramaProperty = dioramasProperty.Of(i);
								string name = dioramaProperty.Of("name").stringValue;
								string visibleName = string.IsNullOrEmpty(name) ? $"Diorama{i.ToString("000")}" : name;
								gen.AppendLine($"case DioramaId.{visibleName}: {{");

								using (gen.IndentBlock) {
									gen.AppendLine($"Unload{visibleName}(commandBuffer);");
									gen.AppendLine($"break;");
								}
								gen.AppendLine($"}}");
							}
						}

						gen.AppendLine($"}}");
					}

					gen.AppendLine($"}}");

					gen.AppendLine($"");

					List<string> characterPrefabUnload = new List<string>();
					for(int i = 0; i < dioramasProperty.arraySize; ++i) {
						characterPrefabUnload.Clear();
						SerializedProperty dioramaProperty = dioramasProperty.Of(i);
						string name = dioramaProperty.Of("name").stringValue;
						string visibleName = string.IsNullOrEmpty(name) ? $"Diorama{i.ToString("000")}" : name;
						gen.AppendLine($"void Unload{visibleName}(EntityCommandBuffer commandBuffer) {{");

						using (gen.IndentBlock) {
							SerializedProperty charactersProperty = dioramaProperty.Of("characters");
							for (int j = 0; j < charactersProperty.arraySize; ++j) {
								SerializedProperty characterProperty = charactersProperty.Of(j);
								string guid = characterProperty.Of("guid").stringValue;
								if (!characterPrefabUnload.Contains(guid)) {
									characterPrefabUnload.Add(guid);
								}
							}

							for (int j = 0; j < characterPrefabUnload.Count; ++j) {
								if (characterTable.TryGetValue(characterPrefabUnload[j], out string characterName)) {
									gen.AppendLine($"Entity entityUnload{characterName}PrefabRequest = CreateRequestEntity(");
									using (gen.IndentBlock) {
										gen.AppendLine($"new RequestUnloadCharacterPrefabComponent {{");
										using (gen.IndentBlock) {
											gen.AppendLine($"characterId = CharacterId.{characterName},");
											gen.AppendLine($"dioramaId = DioramaId.{visibleName},");
										}
										gen.AppendLine($"}}");
									}
									gen.AppendLine($");");
									gen.AppendLine($"#if UNITY_EDITOR");
									gen.AppendLine($"commandBuffer.SetName(entityUnload{characterName}PrefabRequest, \"Unload-Prefab-Request({characterName})\");");
									gen.AppendLine($"#endif");
									gen.AppendLine($"");
								}
							}

							gen.AppendLine($"");

							SerializedProperty fieldAssetsProperty = dioramaProperty.Of("fieldAssets");
							for (int j = 0; j < fieldAssetsProperty.arraySize; ++j) {
								FieldUnloadScript(gen, fieldAssetsProperty.Of(j));
							}
						}

						gen.AppendLine($"}}");
					}
				}

				gen.AppendLine($"}}");
			}

			gen.AppendLine($"}}");
			gen.Generate($"diorama{Path.DirectorySeparatorChar}UnloadDioramaJob.cs");
		}

		private static void FieldUnloadScript(SourceCodeGenerator gen, SerializedProperty fieldAssetProperty) {
			string guid = fieldAssetProperty.Of("m_AssetGUID").stringValue;
			string address = SkycladEditorUtility.Resource.GetAddress(guid);
			if (address != null) {
				string fieldAssetName = address[6..];
				gen.AppendLine($"Entity entityUnload{fieldAssetName}Request = CreateRequestEntity(");
				using (gen.IndentBlock) {
					gen.AppendLine($"new RequestUnloadFieldMeshComponent {{");
					using (gen.IndentBlock) {
						gen.AppendLine($"meshId = FieldMeshId.{fieldAssetName},");
					}
					gen.AppendLine($"}}");
				}
				gen.AppendLine($");");
				gen.AppendLine($"#if UNITY_EDITOR");
				gen.AppendLine($"commandBuffer.SetName(entityUnload{fieldAssetName}Request, \"Unload-Request({fieldAssetName})\");");
				gen.AppendLine($"#endif");
			}
		}

		private static Dictionary<string, string> GetCharacterTable(SerializedObject serializedObject) {
			Dictionary<string, string> table = new Dictionary<string, string>();
			SerializedProperty characterUnitsProperty = serializedObject.FindProperty("_character._units");
			for(int i = 0; i < characterUnitsProperty.arraySize; ++i) {
				SerializedProperty characterUnitProperty = characterUnitsProperty.Of(i);
				string name = characterUnitProperty.Of("name").stringValue;
				string guid = characterUnitProperty.Of("guid").stringValue;
				if (!string.IsNullOrEmpty(name) && !table.ContainsKey(guid)) {
					table.Add(guid, name);
				}
			}
			return table;
		}

	}
}