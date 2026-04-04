using System;
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace skyclad.editor {
	public static class UserDataScriptGenerator {
		public static void Generate(SerializedObject serializedObject) {
			UserDataSettingsModel model = new UserDataSettingsModel(serializedObject);

			SerializedProperty intProperties = model.IntProperties;
			SerializedProperty boolProperties = model.BoolProperties;
			SerializedProperty floatProperties = model.FloatProperties;
			SerializedProperty float2Properties = model.Vector2Properties;
			SerializedProperty float3Properties = model.Vector3Properties;

			SourceCodeGenerator gen = new SourceCodeGenerator();
			gen.AppendLine($"using System.Runtime.InteropServices;");
			gen.AppendLine($"using Unity.Mathematics;");
			gen.AppendLine($"");
			gen.AppendLine($"namespace skyclad.userData {{");
			using (gen.IndentBlock) {
				gen.AppendLine($"public partial struct UserDataComponent {{");
				
				List<string> names = new List<string>();

				using (gen.IndentBlock) {
					for (int i = 0; i < intProperties.arraySize; ++i) {
						string name = intProperties.Of(i).Of("name").stringValue;
						if (string.IsNullOrEmpty(name)) {
							throw new System.Exception("Empty name found.");
						}
						if (names.Contains(name)) {
							throw new System.Exception($"Cannot have parameter with duplicate names:{name}");
						}
						names.Add(name);
						gen.AppendLine($"public int {name};");
						
					}
					for (int i = 0; i < boolProperties.arraySize; ++i) {
						string name = boolProperties.Of(i).Of("name").stringValue;
						if (string.IsNullOrEmpty(name)) {
							throw new System.Exception("Empty name found.");
						}
						if (names.Contains(name)) {
							throw new System.Exception($"Cannot have parameter with duplicate names:{name}");
						}
						names.Add(name);
						gen.AppendLine($"public bool {name};");
					}
					for (int i = 0; i < floatProperties.arraySize; ++i) {
						string name = floatProperties.Of(i).Of("name").stringValue;
						if (string.IsNullOrEmpty(name)) {
							throw new System.Exception("Empty name found.");
						}
						if (names.Contains(name)) {
							throw new System.Exception($"Cannot have parameter with duplicate names:{name}");
						}
						names.Add(name);
						gen.AppendLine($"public float {name};");
					}
					for (int i = 0; i < float2Properties.arraySize; ++i) {
						string name = float2Properties.Of(i).Of("name").stringValue;
						if (string.IsNullOrEmpty(name)) {
							throw new System.Exception("Empty name found.");
						}
						if (names.Contains(name)) {
							throw new System.Exception($"Cannot have parameter with duplicate names:{name}");
						}
						names.Add(name);
						gen.AppendLine($"public float2 {name};");
					}
					for (int i = 0; i < float3Properties.arraySize; ++i) {
						string name = float3Properties.Of(i).Of("name").stringValue;
						if (string.IsNullOrEmpty(name)) {
							throw new System.Exception("Empty name found.");
						}
						if (names.Contains(name)) {
							throw new System.Exception($"Cannot have parameter with duplicate names:{name}");
						}
						names.Add(name);
						gen.AppendLine($"public float3 {name};");
					}

					gen.AppendLine($"");

					gen.AppendLine($"partial void SetDefault() {{");
					using (gen.IndentBlock) {
						int size = intProperties.arraySize * sizeof(int) + 
							boolProperties.arraySize * sizeof(bool) + 
							(floatProperties.arraySize + float2Properties.arraySize * 2 + float3Properties.arraySize * 3) * sizeof(float) +
							5 * sizeof(short);
						gen.AppendLine($"_dataSize = {size};");
						for (int i = 0; i < intProperties.arraySize; ++i) {
							SerializedProperty property = intProperties.Of(i);
							gen.AppendLine($"{property.Of("name").stringValue} = {property.Of("defaultValue").intValue};");
						}
						for (int i = 0; i < boolProperties.arraySize; ++i) {
							SerializedProperty property = boolProperties.Of(i);
							gen.AppendLine($"{property.Of("name").stringValue} = {(property.Of("defaultValue").boolValue ? "true" : "false")};");
						}
						for (int i = 0; i < floatProperties.arraySize; ++i) {
							SerializedProperty property = floatProperties.Of(i);
							gen.AppendLine($"{property.Of("name").stringValue} = {property.Of("defaultValue").floatValue}f;");
						}
						for (int i = 0; i < float2Properties.arraySize; ++i) {
							SerializedProperty property = float2Properties.Of(i);
							Vector2 vector2 = property.Of("defaultValue").vector2Value;
							gen.AppendLine($"{property.Of("name").stringValue} = new float2({vector2.x}f, {vector2.y}f);");
						}
						for (int i = 0; i < float3Properties.arraySize; ++i) {
							SerializedProperty property = float3Properties.Of(i);
							Vector3 vector3 = property.Of("defaultValue").vector3Value;
							gen.AppendLine($"{property.Of("name").stringValue} = new float3({vector3.x}f, {vector3.y}f, {vector3.z}f);");
						}
					}
					gen.AppendLine($"}}");
					gen.AppendLine($"");
					gen.AppendLine($"partial void LoadBinary(byte[] bytes) {{");
					using (gen.IndentBlock) {
						gen.AppendLine($"int startIndex = {5 * sizeof(short)};");
						gen.AppendLine($"System.Span<byte> data = System.MemoryExtensions.AsSpan(bytes);");

						gen.AppendLine($"System.Span<short> sizes = MemoryMarshal.Cast<byte, short>(data.Slice(0, {5 * sizeof(short)}));");

						if (intProperties.arraySize > 0) {
							gen.AppendLine($"");
							gen.AppendLine($"if (data.Length >= startIndex + sizes[0]) {{");
							using (gen.IndentBlock) {
								gen.AppendLine($"System.Span<int> intValues = MemoryMarshal.Cast<byte, int>(data.Slice(startIndex, sizes[0]));");
								for (int i = 0; i < intProperties.arraySize; ++i) {
									string name = intProperties.Of(i).Of("name").stringValue;
									gen.AppendLine($"if ({i} < intValues.Length) {name} = intValues[{i}];");
								}
							}
							gen.AppendLine($"}}");
							gen.AppendLine($"startIndex += sizes[0];");
						}

						if (boolProperties.arraySize > 0) {
							gen.AppendLine($"");
							gen.AppendLine($"if (data.Length >= startIndex + sizes[1]) {{");
							using (gen.IndentBlock) {
								gen.AppendLine($"System.Span<bool> boolValues = MemoryMarshal.Cast<byte, bool>(data.Slice(startIndex, sizes[1]));");
								for (int i = 0; i < boolProperties.arraySize; ++i) {
									string name = boolProperties.Of(i).Of("name").stringValue;
									gen.AppendLine($"if ({i} < boolValues.Length) {name} = boolValues[{i}];");
								}
							}
							gen.AppendLine($"}}");
							gen.AppendLine($"startIndex += sizes[1];");
						}

						if (floatProperties.arraySize > 0) {
							gen.AppendLine($"");
							gen.AppendLine($"if (data.Length >= startIndex + sizes[2]) {{");
							using (gen.IndentBlock) {
								gen.AppendLine($"System.Span<float> floatValues = MemoryMarshal.Cast<byte, float>(data.Slice(startIndex, sizes[2]));");
								for (int i = 0; i < floatProperties.arraySize; ++i) {
									string name = floatProperties.Of(i).Of("name").stringValue;
									gen.AppendLine($"if ({i} < floatValues.Length) {name} = floatValues[{i}];");
								}
							}
							gen.AppendLine($"}}");
							gen.AppendLine($"startIndex += sizes[2];");
						}

						if (float2Properties.arraySize > 0) {
							gen.AppendLine($"");
							gen.AppendLine($"if (data.Length >= startIndex + sizes[3]) {{");
							using (gen.IndentBlock) {
								gen.AppendLine($"System.Span<float> float2Values = MemoryMarshal.Cast<byte, float>(data.Slice(startIndex, sizes[3]));");
								for (int i = 0; i < float2Properties.arraySize; ++i) {
									string name = float2Properties.Of(i).Of("name").stringValue;
									gen.AppendLine($"if ({2*i+1} < float2Values.Length) {name} = new float2(float2Values[{2*i}], float2Values[{2*i+1}]);");
								}
							}
							gen.AppendLine($"}}");
							gen.AppendLine($"startIndex += sizes[3];");
						}

						if (float3Properties.arraySize > 0) {
							gen.AppendLine($"");
							gen.AppendLine($"if (data.Length >= startIndex + sizes[4]) {{");
							using (gen.IndentBlock) {
								gen.AppendLine($"System.Span<float> float3Values = MemoryMarshal.Cast<byte, float>(data.Slice(startIndex, sizes[4]));");
								for (int i = 0; i < float3Properties.arraySize; ++i) {
									string name = float3Properties.Of(i).Of("name").stringValue;
									gen.AppendLine($"if ({3*i+2} < float3Values.Length) {name} = new float3(float3Values[{3*i}], float3Values[{3*i+1}], float3Values[{3*i+2}]);");
								}
							}
							gen.AppendLine($"}}");
							gen.AppendLine($"startIndex += sizes[4];");
						}
					}
					gen.AppendLine($"}}");
					gen.AppendLine($"");
					gen.AppendLine($"partial void SaveBinary(byte[] bytes) {{");
					using (gen.IndentBlock) {
						int offset = 0;
						gen.AppendLine($"System.Span<byte> sizeValues = MemoryMarshal.Cast<short, byte>(");
						using (gen.IndentBlock) {
							gen.AppendLine($"new short[] {{ " +
							$"{intProperties.arraySize * sizeof(int)}, " +
							$"{boolProperties.arraySize * sizeof(bool)}, " + 
							$"{floatProperties.arraySize * sizeof(float)}, " +
							$"{float2Properties.arraySize * sizeof(float) * 2}, " +
							$"{float3Properties.arraySize * sizeof(float) * 3}, " +
							$"}}");
						}
						gen.AppendLine($");");
						gen.AppendLine($"System.Array.Copy(sizeValues.ToArray(), 0, bytes, {offset}, {5 * sizeof(short)});");
						offset += 5 * sizeof(short);

						if (intProperties.arraySize > 0) {
							gen.AppendLine($"");
							gen.AppendLine($"System.Span<byte> intValues = MemoryMarshal.Cast<int, byte>(");
							using (gen.IndentBlock) {
								gen.AppendLine($"new int[] {{");
								using (gen.IndentBlock) {
									for (int i = 0; i < intProperties.arraySize; ++i) {
										gen.AppendLine($"{intProperties.Of(i).Of("name").stringValue},");
									}
								}
								gen.AppendLine($"}}");
							}
							gen.AppendLine($");");
							gen.AppendLine($"System.Array.Copy(intValues.ToArray(), 0, bytes, {offset}, {intProperties.arraySize * sizeof(int)});");
							offset += intProperties.arraySize * sizeof(int);
						}

						if (boolProperties.arraySize > 0) {
							gen.AppendLine($"");
							gen.AppendLine($"System.Span<byte> boolValues = MemoryMarshal.Cast<bool, byte>(");
							using (gen.IndentBlock) {
								gen.AppendLine($"new bool[] {{");
								using (gen.IndentBlock) {
									for (int i = 0; i < boolProperties.arraySize; ++i) {
										gen.AppendLine($"{boolProperties.Of(i).Of("name").stringValue},");
									}
								}
								gen.AppendLine($"}}");
							}
							gen.AppendLine($");");
							gen.AppendLine($"System.Array.Copy(boolValues.ToArray(), 0, bytes, {offset}, {boolProperties.arraySize * sizeof(bool)});");
							offset += boolProperties.arraySize * sizeof(bool);
						}

						if (floatProperties.arraySize > 0) {
							gen.AppendLine($"");
							gen.AppendLine($"System.Span<byte> floatValues = MemoryMarshal.Cast<float, byte>(");
							using (gen.IndentBlock) {
								gen.AppendLine($"new float[] {{");
								using (gen.IndentBlock) {
									for (int i = 0; i < floatProperties.arraySize; ++i) {
										gen.AppendLine($"{floatProperties.Of(i).Of("name").stringValue},");
									}
								}
								gen.AppendLine($"}}");
							}
							gen.AppendLine($");");
							gen.AppendLine($"System.Array.Copy(floatValues.ToArray(), 0, bytes, {offset}, {floatProperties.arraySize * sizeof(float)});");
							offset += floatProperties.arraySize * sizeof(float);
						}
						
						if (float2Properties.arraySize > 0) {
							gen.AppendLine($"");
							gen.AppendLine($"System.Span<byte> float2Values = MemoryMarshal.Cast<float, byte>(");
							using (gen.IndentBlock) {
								gen.AppendLine($"new float[] {{");
								using (gen.IndentBlock) {
									for (int i = 0; i < float2Properties.arraySize; ++i) {
										string name = float2Properties.Of(i).Of("name").stringValue;
										gen.AppendLine($"{name}.x, {name}.y,");
									}
								}
								gen.AppendLine($"}}");
							}
							gen.AppendLine($");");
							gen.AppendLine($"System.Array.Copy(float2Values.ToArray(), 0, bytes, {offset}, {float2Properties.arraySize * sizeof(float) * 2});");
							offset += float2Properties.arraySize * sizeof(float) * 2;
						}

						if (float3Properties.arraySize > 0) {
							gen.AppendLine($"");
							gen.AppendLine($"System.Span<byte> float3Values = MemoryMarshal.Cast<float, byte>(");
							using (gen.IndentBlock) {
								gen.AppendLine($"new float[] {{");
								using (gen.IndentBlock) {
									for (int i = 0; i < float3Properties.arraySize; ++i) {
										string name = float3Properties.Of(i).Of("name").stringValue;
										gen.AppendLine($"{name}.x, {name}.y, {name}.z,");
									}
								}
								gen.AppendLine($"}}");
							}
							gen.AppendLine($");");
							gen.AppendLine($"System.Array.Copy(float3Values.ToArray(), 0, bytes, {offset}, {float3Properties.arraySize * sizeof(float) * 3});");
							offset += float3Properties.arraySize * sizeof(float) * 3;
						}
					}
					gen.AppendLine($"}}");
				}
				gen.AppendLine($"}}");
			}
			gen.AppendLine($"}}");

			gen.Generate($"userData{Path.DirectorySeparatorChar}UserDataComponent.cs");
		}
	}
}