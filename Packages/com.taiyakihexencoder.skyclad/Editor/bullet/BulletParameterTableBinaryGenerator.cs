using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace skyclad.editor {
	internal static class BulletParameterTableBinaryGenerator {
		internal static void Generate(SerializedObject serializedObject, string path) {
			BinaryFileGenerator.Generate(
				(writer) => WriteBinary(writer, serializedObject),
				$"{GlobalProjectSettings.ApplicationExternalDataPath}{Path.DirectorySeparatorChar}{path}"
			);
		}

		private static void WriteBinary(BinaryWriter writer, SerializedObject serializedObject) {
			SerializedProperty parameterDefsProperty = serializedObject.FindProperty("_bullet._parameterDefs");

			List<ParameterDefs> parameters = ParameterDefs.AsList(parameterDefsProperty);

			List<System.Action<BinaryWriter, string>> writeAction = new List<System.Action<BinaryWriter, string>>();
			for (int i = 0; i < parameters.Count; ++i) {
				switch(parameters[i].parameterType) {
					case ParameterType.Int: {
						writeAction.Add(IntegerWriter);
						break;
					}
					case ParameterType.Bool: {
						writeAction.Add(BooleanWriter);
						break;
					}
					case ParameterType.Float: {
						writeAction.Add(FloatWriter);
						break;
					}
					case ParameterType.Float2: {
						writeAction.Add(Float2Writer);
						break;
					}
					case ParameterType.Float3: {
						writeAction.Add(Float3Writer);
						break;
					}
				}
			}

			SerializedProperty unitsProperty, parameterProperty, namesProperty, valuesProperty;
			SerializedProperty groupsProperty = serializedObject.FindProperty("_bullet._groups");

			int dataCount = 0;
			for(int groupIndex = 0; groupIndex < groupsProperty.arraySize; ++groupIndex) {
				unitsProperty = groupsProperty.Of(groupIndex).Of("units");
				dataCount += unitsProperty.arraySize;
			}
			writer.Write(dataCount);

			for (int groupIndex = 0; groupIndex < groupsProperty.arraySize; ++groupIndex) {
				unitsProperty = groupsProperty.Of(groupIndex).Of("units");
				for (int unitIndex = 0; unitIndex < unitsProperty.arraySize; ++unitIndex) {
					parameterProperty = unitsProperty.Of(unitIndex).Of("parameter");
					namesProperty = parameterProperty.Of("names");
					valuesProperty = parameterProperty.Of("values");

					for (int parameterIndex = 0; parameterIndex < parameters.Count; ++parameterIndex) {
						string value = "";
						for (int unitParameterIndex = 0; unitParameterIndex < namesProperty.arraySize; ++unitParameterIndex) {
							if (namesProperty.Of(unitParameterIndex).stringValue == parameters[parameterIndex].name) {
								value = valuesProperty.Of(unitParameterIndex).stringValue;
							}
						}
						writeAction[parameterIndex](writer, value);
					}
				}
			}
		}

		private static void IntegerWriter(BinaryWriter writer, string value) {
			writer.Write(int.TryParse(value, out int v) ? v : 0);
		}

		private static void BooleanWriter(BinaryWriter writer, string value) {
			writer.Write(bool.TryParse(value, out bool v) && v);
		}

		private static void FloatWriter(BinaryWriter writer, string value) {
			writer.Write(float.TryParse(value, out float v) ? v : 0.0f);
		}

		private static void Float2Writer(BinaryWriter writer, string value) {
			Regex regex = new Regex(@"\((?<x>[0-9.]+),(?<y>[0-9.]+)\)");
			Match match = regex.Match(value);
			Vector2 v = match.Success 
				? new Vector2(
					float.Parse(match.Groups["x"].Captures[0].Value), 
					float.Parse(match.Groups["y"].Captures[0].Value)
				) : Vector2.zero;
			writer.Write(v.x);
			writer.Write(v.y);
		}

		private static void Float3Writer(BinaryWriter writer, string value) {
			Regex regex = new Regex(@"\((?<x>[0-9.]+),(?<y>[0-9.]+),(?<z>[0-9.]+)\)");
			Match match = regex.Match(value);
			Vector3 v = match.Success 
				? new Vector3(
					float.Parse(match.Groups["x"].Captures[0].Value), 
					float.Parse(match.Groups["y"].Captures[0].Value), 
					float.Parse(match.Groups["z"].Captures[0].Value)
				) : Vector3.zero;
			writer.Write(v.x);
			writer.Write(v.y);
			writer.Write(v.z);
		}

	}
}