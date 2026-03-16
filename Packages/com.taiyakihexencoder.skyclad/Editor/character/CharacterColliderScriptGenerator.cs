using System.IO;
using UnityEditor;

namespace skyclad.editor {
	internal static class CharacterColliderScriptGenerator {
		internal static void Generate(SerializedObject serializedObject) {
			SerializedProperty colliderUnitsProperty = serializedObject.FindProperty("_character._colliderUnits");

			SourceCodeGenerator gen = new SourceCodeGenerator();
			gen.AppendLine($"using Unity.Entities;");
			gen.AppendLine($"using Unity.Mathematics;");
			gen.AppendLine($"using Unity.Physics;");
			gen.AppendLine($"");

			gen.AppendLine($"namespace skyclad.character {{");
			using (gen.IndentBlock) {
				gen.AppendLine($"public static partial class CharacterCollider {{");
				using (gen.IndentBlock) {
					for(int i = 0; i < colliderUnitsProperty.arraySize; ++i) {
						SerializedProperty colliderProperty = colliderUnitsProperty.Of(i);
						string name = colliderProperty.Of("name").stringValue;
						gen.AppendLine($"public static BlobAssetReference<Collider> Blob{name} {{ get; private set; }} = BlobAssetReference<Collider>.Null;");
					}
					gen.AppendLine($"");
					gen.AppendLine($"static partial void Init() {{");

					using (gen.IndentBlock) {
						for(int i = 0; i < colliderUnitsProperty.arraySize; ++i) {
							SerializedProperty colliderUnitProperty = colliderUnitsProperty.Of(i);
							string name = colliderUnitProperty.Of("name").stringValue;
							float radius = colliderUnitProperty.Of("radius").floatValue;
							float height = colliderUnitProperty.Of("height").floatValue;
							gen.AppendLine($"Blob{name} = CapsuleCollider.Create(");

							using (gen.IndentBlock) {
								gen.AppendLine($"new CapsuleGeometry {{");

								using (gen.IndentBlock) {
									gen.AppendLine($"Radius = {radius}f,");
									gen.AppendLine($"Vertex0 = new float3(0.0f, {radius}f, 0.0f),");
									gen.AppendLine($"Vertex1 = new float3(0.0f, {height - radius}f, 0.0f),");
								}

								gen.AppendLine($"}},");
								gen.AppendLine($"new CollisionFilter {{");

								gen.AddIndent(); {
									gen.AppendLine($"BelongsTo = Layer.PhysicsObject,");
									gen.AppendLine($"CollidesWith = Layer.CollidesWith.PhysicsObject,");
								} gen.RemoveIndent();

								gen.AppendLine($"}}");
							}

							gen.AppendLine($");");
							gen.AppendLine($"Blob{name}.Value.SetCollisionResponse(CollisionResponsePolicy.CollideRaiseCollisionEvents);");
							gen.AppendLine($"");
						}
					}

					gen.AppendLine($"}}");

					gen.AppendLine($"");
					gen.AppendLine($"static partial void Dispose() {{");

					gen.AddIndent(); {
						for (int i = 0; i < colliderUnitsProperty.arraySize; ++i) {
							SerializedProperty colliderUnitProperty = colliderUnitsProperty.Of(i);
							string name = colliderUnitProperty.Of("name").stringValue;
							gen.AppendLine($"if (Blob{name}.IsCreated) Blob{name}.Dispose();");
						}
					} gen.RemoveIndent();

					gen.AppendLine($"}}");
				}
				gen.AppendLine($"}}");
			}
			gen.AppendLine($"}}");
			gen.Generate($"character{Path.DirectorySeparatorChar}CharacterCollider.cs");
		}
	}
}