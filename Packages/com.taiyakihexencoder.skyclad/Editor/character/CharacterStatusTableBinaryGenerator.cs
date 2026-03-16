using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace skyclad.editor {
	internal static class CharacterStatusTableBinaryGenerator {
		internal static void Generate(SerializedObject serializedObject, string path) {
			BinaryFileGenerator.Generate(
				(writer) => WriteBinary(writer, serializedObject),
				$"{GlobalProjectSettings.ApplicationExternalDataPath}{Path.DirectorySeparatorChar}{path}"
			);
		}

		private static void WriteBinary(BinaryWriter writer, SerializedObject serializedObject) {
			SerializedProperty parametersProperty = serializedObject.FindProperty("_character._statusParameterUnits");
			SerializedProperty unitsProperty = serializedObject.FindProperty("_character._units");

			writer.Write(unitsProperty.arraySize);

			List<CharacterProjectSettings.ParameterDefs> parameters = new List<CharacterProjectSettings.ParameterDefs>();
			List<System.Action<BinaryWriter, string>> writeAction = new List<System.Action<BinaryWriter, string>>();
			for (int i = 0; i < parametersProperty.arraySize; ++i) {
				SerializedProperty parameterProperty = parametersProperty.Of(i);
				CharacterProjectSettings.ParameterType statusType = (CharacterProjectSettings.ParameterType) parameterProperty.Of("parameterType").intValue;
				parameters.Add(
					new CharacterProjectSettings.ParameterDefs {
						name = parameterProperty.Of("name").stringValue,
						parameterType = statusType,
					}
				);

				switch(statusType) {
					case CharacterProjectSettings.ParameterType.Int: {
						writeAction.Add(IntegerWriter);
						break;
					}
					case CharacterProjectSettings.ParameterType.Bool: {
						writeAction.Add(BooleanWriter);
						break;
					}
					case CharacterProjectSettings.ParameterType.Float: {
						writeAction.Add(FloatWriter);
						break;
					}
					case CharacterProjectSettings.ParameterType.Float2: {
						writeAction.Add(Float2Writer);
						break;
					}
					case CharacterProjectSettings.ParameterType.Float3: {
						writeAction.Add(Float3Writer);
						break;
					}
				}
			}


			SerializedProperty unitProperty, statusProperty, statusNamesProperty, statusValuesProperty;
			int index;

			for (int i = 0; i < unitsProperty.arraySize; ++i) {
				unitProperty = unitsProperty.Of(i);
				if (! unitProperty.Of("type").Of("hasStatus").boolValue) {
					continue;
				}
				statusProperty = unitsProperty.Of(i).Of("status");
				statusNamesProperty = statusProperty.Of("names");
				statusValuesProperty = statusProperty.Of("values");

				for (int j = 0; j < parameters.Count; ++j) {
					CharacterProjectSettings.ParameterDefs parameter = parameters[j];
					for(index = 0; index < statusNamesProperty.arraySize; ++index) {
						if (statusNamesProperty.Of(index).stringValue == parameter.name) {
							break;
						}
					}

					string value;
					if (index == statusNamesProperty.arraySize) {
						value = "";
					} else {
						value = statusValuesProperty.Of(index).stringValue;
					}

					writeAction[j](writer, value);
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