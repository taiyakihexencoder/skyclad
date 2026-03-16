using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace skyclad.editor {
	internal static class CharacterControllerScriptGenerator {
		internal static void Generate(SerializedObject serializedObject) {
			SerializedProperty controllersProperty = serializedObject.FindProperty("_character._controllers");
			GenerateComponentScript(controllersProperty);
			GenerateLoadCharacterPrefabSystemScript(serializedObject);
		}

		private static void GenerateComponentScript(SerializedProperty controllersProperty) {
			SourceCodeGenerator gen = new SourceCodeGenerator();

			gen.AppendLine($"using Unity.Entities;");
			gen.AppendLine($"using Unity.Mathematics;");
			gen.AppendLine($"");

			gen.AppendLine($"namespace skyclad.control {{");
			using(gen.IndentBlock) {
				for (int i = 0; i < controllersProperty.arraySize; ++i) {
					SerializedProperty controllerProperty = controllersProperty.Of(i);

					string controllerName = controllerProperty.Of("name").stringValue;
					gen.AppendLine($"public struct {controllerName} : IComponentData {{");

					SerializedProperty parametersProperty = controllerProperty.Of("_parameters");
					using (gen.IndentBlock) {
						for(int j = 0; j < parametersProperty.arraySize; ++j) {
							SerializedProperty parameterProperty = parametersProperty.Of(j);
							CharacterProjectSettings.ParameterType parameterType = (CharacterProjectSettings.ParameterType)parameterProperty.Of("parameterType").intValue;
							string parameterName = parameterProperty.Of("name").stringValue;
							gen.AppendLine($"public {parameterType.ToString().ToLower()} {parameterName};");
						}
					}
					gen.AppendLine($"}}");
					gen.AppendLine($"");
				}
			}
			gen.AppendLine($"}}");
			gen.Generate($"controller{Path.DirectorySeparatorChar}CharacterControllers.cs");
		}

		private static void GenerateLoadCharacterPrefabSystemScript(SerializedObject serializedObject) {
			SerializedProperty controllersProperty = serializedObject.FindProperty("_character._controllers");
			Dictionary<string, CharacterProjectSettings.CharacterController> controllerTable = new Dictionary<string, CharacterProjectSettings.CharacterController>();

			for(int i = 0; i < controllersProperty.arraySize; ++i) {
				SerializedProperty controllerProperty = controllersProperty.Of(i);
				SerializedProperty controllerParametersProperty = controllerProperty.Of("_parameters");
				CharacterProjectSettings.ParameterDefs[] parameterList = new CharacterProjectSettings.ParameterDefs[controllerParametersProperty.arraySize];
				for(int j = 0; j < controllerParametersProperty.arraySize; ++j) {
					SerializedProperty controllerParameterProperty = controllerParametersProperty.Of(j);
					parameterList[j] = new CharacterProjectSettings.ParameterDefs {
						parameterType = (CharacterProjectSettings.ParameterType) controllerParameterProperty.Of("parameterType").intValue,
						name = controllerParameterProperty.Of("name").stringValue,
					};
				}
				string name = controllerProperty.Of("name").stringValue;
				string guid = controllerProperty.Of("guid").stringValue;
				controllerTable.Add(guid, CharacterProjectSettings.CharacterController.Create(guid, name, parameterList));
			}

			SerializedProperty characterUnitsProperty = serializedObject.FindProperty("_character._units");

			SourceCodeGenerator gen = new SourceCodeGenerator();

			gen.AppendLine($"using Unity.Entities;");
			gen.AppendLine($"using Unity.Mathematics;");
			
			gen.AppendLine($"namespace skyclad.character {{");
			using (gen.IndentBlock) {
				gen.AppendLine($"public partial struct LoadCharacterPrefabSystem {{");
				using (gen.IndentBlock) {
					gen.AppendLine($"partial void SetUniquePrefabParameter(EntityCommandBuffer commandBuffer, Entity prefab, int characterId) {{");
					using (gen.IndentBlock) {
						gen.AppendLine($"switch (characterId) {{");
						using (gen.IndentBlock) {
							for(int i = 0; i < characterUnitsProperty.arraySize; ++i) {
								SerializedProperty characterUnitProperty = characterUnitsProperty.Of(i);
								SerializedProperty characterControlProperty = characterUnitProperty.Of("controller");
								string controllerGuid = characterUnitProperty.Of("controllerGuid").stringValue;
								if (controllerTable.TryGetValue(controllerGuid, out CharacterProjectSettings.CharacterController controller)) {
									List<KeyValuePair<string, string>> characterParameterList = new List<KeyValuePair<string, string>>();
									SerializedProperty controllerParameterNamesProperty = characterControlProperty.Of("names");
									SerializedProperty controllerParametersProperty = characterControlProperty.Of("values");
									for (int j = 0; j < controllerParameterNamesProperty.arraySize; ++j) {
										string name = controllerParameterNamesProperty.Of(j).stringValue;
										string value = controllerParametersProperty.Of(j).stringValue;
										characterParameterList.Add(new KeyValuePair<string, string>(name, value));
									}

									string characterName = characterUnitProperty.Of("name").stringValue;
									gen.AppendLine($"case CharacterId.{characterName}: {{");
									using (gen.IndentBlock) {
										gen.AppendLine($"commandBuffer.AddComponent(");
										using (gen.IndentBlock) {
											gen.AppendLine($"prefab,");
											WriteControllerScript(gen, controller, characterParameterList);
										}
										gen.AppendLine($");");
										gen.AppendLine($"break;");
									}
									gen.AppendLine($"}}");
								}
							}
						}
						gen.AppendLine($"}}");
					}
					gen.AppendLine($"}}");
				}
				gen.AppendLine($"}}");
			}
			gen.AppendLine($"}}");
			gen.Generate($"character{Path.DirectorySeparatorChar}LoadCharacterPrefabSystem.cs", false);
		}

		private static void WriteControllerScript(
			SourceCodeGenerator gen,
			CharacterProjectSettings.CharacterController controller,
			List<KeyValuePair<string, string>> parameters
		) {
			gen.AppendLine($"new control.{controller.name} {{");
			using (gen.IndentBlock) {
				Regex regexF2 = new Regex(@"\((?<x>[0-9.]+),(?<y>[0-9.]+)\)");
				Regex regexF3 = new Regex(@"\((?<x>[0-9.]+),(?<y>[0-9.]+),(?<z>[0-9.]+)\)");
				foreach (CharacterProjectSettings.ParameterDefs parameter in controller.Parameters) {
					int index = parameters.FindIndex(_ => _.Key == parameter.name);
					if (index < 0) { break; }
					string valueString = parameters[index].Value;
					switch(parameter.parameterType) {
						case CharacterProjectSettings.ParameterType.Int:{
							int v = int.TryParse(valueString, out int val) ? val : 0;
							gen.AppendLine($"{parameter.name} = {v},");
							break;
						}
						case CharacterProjectSettings.ParameterType.Bool:{
							bool v = bool.TryParse(valueString, out bool val) && val;
							gen.AppendLine($"{parameter.name} = {v},");
							break;
						}
						case CharacterProjectSettings.ParameterType.Float:{
							float v = float.TryParse(valueString, out float val) ? val : 0.0f;
							gen.AppendLine($"{parameter.name} = {v}f,");
							break;
						}
						case CharacterProjectSettings.ParameterType.Float2:{
							Match match = regexF2.Match(valueString);
							Vector2 v = match.Success 
								? new Vector2(
									float.Parse(match.Groups["x"].Captures[0].Value), 
									float.Parse(match.Groups["y"].Captures[0].Value)
								) : Vector2.zero;
							gen.AppendLine($"{parameter.name} = new float2({v.x}f, {v.y}f),");
							break;
						}
						case CharacterProjectSettings.ParameterType.Float3:{
							Match match = regexF3.Match(valueString);
							Vector3 v = match.Success 
								? new Vector3(
									float.Parse(match.Groups["x"].Captures[0].Value), 
									float.Parse(match.Groups["y"].Captures[0].Value),
									float.Parse(match.Groups["z"].Captures[0].Value)
								) : Vector3.zero;
							gen.AppendLine($"{parameter.name} = new float3({v.x}f, {v.y}f, {v.z}f),");
							break;
						}
					}
				}
			}
			gen.AppendLine($"}}");
		}
	}
}