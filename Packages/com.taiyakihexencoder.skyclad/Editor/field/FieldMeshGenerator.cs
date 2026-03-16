using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace skyclad.editor {
	internal static class FieldMeshGenerator {
		internal static void Generate(FieldProjectSettings settings) {
			switch(settings.FieldType) {
				case FieldType.Field2DSideView: {
					Generate2DSideView(settings);
					break;
				}
			}
		}

		private static void Generate2DSideView(FieldProjectSettings settings) {
			List<string> tableNames = new List<string>();
			foreach(string guid in AssetDatabase.FindAssets($"t:{typeof(FieldScriptable2DSideView).Name}")) {
				string editAssetPath = AssetDatabase.GUIDToAssetPath(guid);
				FieldScriptable2DSideView scriptable = AssetDatabase.LoadAssetAtPath<FieldScriptable2DSideView>(editAssetPath);

				string assetPath = $"{FieldProjectSettings.AssetPath}{Path.DirectorySeparatorChar}{scriptable.name}.asset";
				field.FieldAsset mainAsset = ScriptableObject.CreateInstance<field.FieldAsset>();
				SkycladEditorUtility.Asset.Create(mainAsset, $"Assets{Path.DirectorySeparatorChar}{assetPath}");

				foreach(FieldScriptable2DSideView.FieldElement fieldElement in scriptable.FieldElements) {
					FieldScriptable2DSideViewEditor.CreateVertexList(fieldElement.Points, out Vector3[] vertices, out int[] indices);
					if (vertices.Length < 2) {
						continue;
					}
					Mesh mesh = new Mesh();
					mesh.name = string.IsNullOrEmpty(fieldElement.Name) ? "Unnamed" : fieldElement.Name;
					mesh.SetVertices(vertices);
					mesh.SetIndices(indices, MeshTopology.Triangles, 0);
					mesh.RecalculateNormals();
					AssetDatabase.AddObjectToAsset(mesh, mainAsset);
				}
				SkycladEditorUtility.Resource.SetAddress(mainAsset, $"Field/{scriptable.name}");
				tableNames.Add(scriptable.name);

				GenerateIdScript(tableNames);

				AssetDatabase.SaveAssets();
			}
		}

		private static void GenerateIdScript(IList<string> tableNames) {
			SourceCodeGenerator gen = new SourceCodeGenerator();
			gen.AppendLine($"namespace skyclad {{");
			using (gen.IndentBlock) {
				gen.AppendLine($"public static partial class FieldMeshId {{");
				using (gen.IndentBlock) {
					for(int i = 0; i < tableNames.Count; ++i) {
						gen.AppendLine($"public const int {tableNames[i]} = {i+1};");
					}
					gen.AppendLine($"");
					gen.AppendLine($"static partial void SetAddress(int id) {{");
					using (gen.IndentBlock) {
						gen.AppendLine($"switch(id) {{");
						using (gen.IndentBlock) {
							for (int i = 0; i < tableNames.Count; ++i) {
								gen.AppendLine($"case {tableNames[i]}: _lastSelected = \"Field/{tableNames[i]}\"; break;");
							}
							gen.AppendLine($"default: _lastSelected = null; break;");
						}
						gen.AppendLine($"}}");
					}
					gen.AppendLine($"}}");
				}
				gen.AppendLine($"}}");
			}
			gen.AppendLine($"}}");
			gen.Generate($"field{Path.DirectorySeparatorChar}FieldMeshId.cs");
		}
	}
}