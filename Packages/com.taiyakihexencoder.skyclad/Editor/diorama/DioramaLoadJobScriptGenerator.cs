using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace skyclad.editor {
	internal static class DioramaLoadJobScriptGenerator {
		private static Dictionary<string, CharacterProjectSettings.Unit> _characterTable;
		private static Dictionary<string, int> _statusIndexList;
		private static Dictionary<string, CharacterProjectSettings.ColliderUnit> _colliderTable;
		private static Dictionary<string, CharacterProjectSettings.CharacterController> _controllerTable;

		internal static void Generate(SerializedObject serializedObject) {
			SerializedProperty dioramasProperty = serializedObject.FindProperty("_diorama._units");
			_characterTable = GetCharacterTable(serializedObject);
			_colliderTable = GetColliderTable(serializedObject);
			_controllerTable = GetControllerTable(serializedObject);
			_statusIndexList = GetStatusIndexList(serializedObject);

			SourceCodeGenerator gen = new SourceCodeGenerator();
			gen.AppendLine($"using Unity.Entities;");
			gen.AppendLine($"using Unity.Mathematics;");
			gen.AppendLine($"using Unity.Physics;");
			gen.AppendLine($"");
			gen.AppendLine($"namespace skyclad {{");
			
			using(gen.IndentBlock) {
				gen.AppendLine("using character;");
				gen.AppendLine("using field;");
				gen.AppendLine($"public partial struct LoadDioramaJob {{");

				using(gen.IndentBlock) {

					gen.AppendLine($"partial void Load(EntityCommandBuffer commandBuffer, int dioramaId) {{");

					using(gen.IndentBlock) {
						gen.AppendLine($"switch (dioramaId) {{");

						using(gen.IndentBlock) {
							for(int i = 0; i < dioramasProperty.arraySize; ++i) {
								SerializedProperty dioramaProperty = dioramasProperty.Of(i);
								string name = dioramaProperty.Of("name").stringValue;
								string visibleName = string.IsNullOrEmpty(name) ? $"Diorama{i.ToString("000")}" : name;
								gen.AppendLine($"case DioramaId.{visibleName}: {{");

								using (gen.IndentBlock) {
									gen.AppendLine($"Load{visibleName}(commandBuffer);");
									gen.AppendLine($"break;");
								}
								gen.AppendLine($"}}");
							}
						}
						
						gen.AppendLine($"}}");
					}

					gen.AppendLine($"}}");

					gen.AppendLine($"");

					List<string> characterPrefabGenerated = new List<string>();
					for(int i = 0; i < dioramasProperty.arraySize; ++i) {
						characterPrefabGenerated.Clear();
						SerializedProperty dioramaProperty = dioramasProperty.Of(i);
						Vector3 basis = dioramaProperty.Of("basis").vector3Value;
						string name = dioramaProperty.Of("name").stringValue;
						string visibleName = string.IsNullOrEmpty(name) ? $"Diorama{i.ToString("000")}" : name;
						gen.AppendLine($"void Load{visibleName}(EntityCommandBuffer commandBuffer) {{");

						using (gen.IndentBlock) {
							SerializedProperty charactersProperty = dioramaProperty.Of("characters");
							for (int j = 0; j < charactersProperty.arraySize; ++j) {
								SerializedProperty characterProperty = charactersProperty.Of(j);
								string guid = characterProperty.Of("guid").stringValue;
								Vector3 position = characterProperty.Of("position").vector3Value;
								Quaternion rotation = characterProperty.Of("rotation").quaternionValue;
					
								if (_characterTable.TryGetValue(guid, out CharacterProjectSettings.Unit character)) {
									string characterName = character.name;
									// Prefabの生成は重複させない
									if (!characterPrefabGenerated.Exists(_ => _ == guid)) {
										characterPrefabGenerated.Add(guid);
										CharacterLoadScript(gen, character, position, rotation, visibleName);
									}

									// 生成位置指定, DynamicBufferで追加してSpawnが終わったら生成
									gen.AppendLine($"{characterName}SpawnBuffer.Add(");
									using (gen.IndentBlock) {
										gen.AppendLine($"new SpawnAfterCreatePrefabBufferElement {{");
										using (gen.IndentBlock) {
											gen.AppendLine($"position = new float3({position.x}f, {position.y}f, {position.z}f),");
											gen.AppendLine($"rotation = new quaternion({rotation.x}f, {rotation.y}f, {rotation.z}f, {rotation.w}f),");
										}
										gen.AppendLine($"}}");
									}
									gen.AppendLine($");");
									gen.AppendLine($"");
								}
							}

							SerializedProperty fieldAssetsProperty = dioramaProperty.Of("fieldAssets");
							for (int j = 0; j < fieldAssetsProperty.arraySize; ++j) {
								FieldLoadScript(gen, fieldAssetsProperty.Of(i));
							}
						}
						gen.AppendLine($"}}");
					}
				}
				gen.AppendLine($"}}");
			}

			gen.AppendLine($"}}");
			gen.Generate($"diorama{Path.DirectorySeparatorChar}LoadDioramaJob.cs");

			_characterTable.Clear();
			_statusIndexList.Clear();
			_colliderTable.Clear();
		}

		private static void CharacterLoadScript(
			SourceCodeGenerator gen, 
			CharacterProjectSettings.Unit character,
			Vector3 position,
			Quaternion rotation,
			string dioramaName
		) {
			string characterName = character.name;
			gen.AppendLine($"Entity entityLoadCharacter{characterName}PrefabRequest = CreateRequestEntity(");
			using (gen.IndentBlock) {
				gen.AppendLine($"new RequestLoadCharacterPrefabComponent {{");
				using (gen.IndentBlock){
					gen.AppendLine($"characterId = CharacterId.{characterName},");
					gen.AppendLine($"name = \"{characterName}\",");

					if (_statusIndexList.TryGetValue(character.guid, out int statusIndex)) {
						gen.AppendLine($"statusIndex = {statusIndex},");
					} else {
						gen.AppendLine($"statusIndex = -1,");
					}

					if (character.type.hasCollider) {
						if (_colliderTable.TryGetValue(character.colliderGuid, out CharacterProjectSettings.ColliderUnit collider)) {
							gen.AppendLine($"collider = CharacterCollider.Blob{collider.name},");
						}
					} else {
						gen.AppendLine($"collider = BlobAssetReference<Collider>.Null,");
					}

					gen.AppendLine($"dioramaId = DioramaId.{dioramaName}");
				}
				gen.AppendLine($"}}");
			}
			gen.AppendLine($");");

			gen.AppendLine($"DynamicBuffer<SpawnAfterCreatePrefabBufferElement> {characterName}SpawnBuffer");
			using (gen.IndentBlock) {
				gen.AppendLine($" = commandBuffer.AddBuffer<SpawnAfterCreatePrefabBufferElement>(entityLoadCharacter{characterName}PrefabRequest);");
			}
			gen.AppendLine($"#if UNITY_EDITOR");
			gen.AppendLine($"commandBuffer.SetName(entityLoadCharacter{characterName}PrefabRequest, \"Create-Prefab-Request({characterName})\");");
			gen.AppendLine($"#endif");
		}

		private static void FieldLoadScript(SourceCodeGenerator gen, SerializedProperty fieldAssetProperty) {
			string guid = fieldAssetProperty.Of("m_AssetGUID").stringValue;
			string address = SkycladEditorUtility.Resource.GetAddress(guid);
			if (address != null) {
				string fieldAssetName = address[6..];
				gen.AppendLine($"Entity entityLoadField{fieldAssetName}Request = CreateRequestEntity(");
				using (gen.IndentBlock) {
					gen.AppendLine($"new RequestLoadFieldMeshComponent {{");
					using (gen.IndentBlock) {
						gen.AppendLine($"meshId = FieldMeshId.{fieldAssetName},");
					}
					gen.AppendLine($"}}");
				}
				gen.AppendLine($");");
				gen.AppendLine($"#if UNITY_EDITOR");
				gen.AppendLine($"commandBuffer.SetName(entityLoadField{fieldAssetName}Request, \"Create-Request({fieldAssetName})\");");
				gen.AppendLine($"#endif");
			}
		}

		private static Dictionary<string, CharacterProjectSettings.Unit> GetCharacterTable(SerializedObject serializedObject) {
			Dictionary<string, CharacterProjectSettings.Unit> table = new Dictionary<string, CharacterProjectSettings.Unit>();
			SerializedProperty characterUnitsProperty = serializedObject.FindProperty("_character._units");
			for(int i = 0; i < characterUnitsProperty.arraySize; ++i) {
				SerializedProperty characterUnitProperty = characterUnitsProperty.Of(i);
				string name = characterUnitProperty.Of("name").stringValue;
				string guid = characterUnitProperty.Of("guid").stringValue;
				if (!string.IsNullOrEmpty(name) && !table.ContainsKey(guid)) {
					SerializedProperty controllerProperty = characterUnitProperty.Of("controller");
					SerializedProperty controllerNamesProperty = controllerProperty.Of("names");
					string[] controllerParameterNames = new string[controllerNamesProperty.arraySize];

					SerializedProperty controllerValuesProperty = controllerProperty.Of("values");
					string[] controllerParameters = new string[controllerValuesProperty.arraySize];

					for(int n = 0; n < controllerNamesProperty.arraySize; ++n) {
						controllerParameterNames[n] = controllerNamesProperty.Of(n).stringValue;
						controllerParameters[n] = controllerValuesProperty.Of(n).stringValue;
					}

					table.Add(
						guid,
						new CharacterProjectSettings.Unit {
							guid = guid,
							name = name,
							colliderGuid = characterUnitProperty.Of("colliderGuid").stringValue,
							isPlayerCharacter = characterUnitProperty.Of("isPlayerCharacter").boolValue,
							status = new CharacterProjectSettings.ParameterInfo {
								// ステータスは使わないので入れない
								names = new string[0],
								values = new string[0],
							},
							type = new CharacterProjectSettings.CharacterType {
								hasCollider = characterUnitProperty.Of("type.hasCollider").boolValue,
								hasStatus = characterUnitProperty.Of("type.hasStatus").boolValue,
								hasController = characterUnitProperty.Of("type.hasController").boolValue,
							},
							controllerGuid = characterUnitProperty.Of("controllerGuid").stringValue,
							controller = new CharacterProjectSettings.ParameterInfo {
								names = controllerParameterNames,
								values = controllerParameters,
							}
						}
					);
				}
			}
			return table;
		}

		private static Dictionary<string, CharacterProjectSettings.ColliderUnit> GetColliderTable(SerializedObject serializedObject) {
			Dictionary<string, CharacterProjectSettings.ColliderUnit> table = new Dictionary<string, CharacterProjectSettings.ColliderUnit>();
			SerializedProperty colliderUnitsProperty = serializedObject.FindProperty("_character._colliderUnits");
			for(int i = 0; i < colliderUnitsProperty.arraySize; ++i) {
				SerializedProperty colliderUnitProperty = colliderUnitsProperty.Of(i);
				string name = colliderUnitProperty.Of("name").stringValue;
				string guid = colliderUnitProperty.Of("guid").stringValue;
				if (!string.IsNullOrEmpty(name) && !table.ContainsKey(guid)) {
					table.Add(
						guid,
						new CharacterProjectSettings.ColliderUnit {
							guid = guid,
							name = name,
							height = colliderUnitProperty.Of("height").floatValue,
							radius = colliderUnitProperty.Of("radius").floatValue,
						}
					);
				}
			}
			return table;
		}

		private static Dictionary<string, CharacterProjectSettings.CharacterController> GetControllerTable(SerializedObject serializedObject) {
			Dictionary<string, CharacterProjectSettings.CharacterController> table = new Dictionary<string, CharacterProjectSettings.CharacterController>();
			SerializedProperty controllersProperty = serializedObject.FindProperty("_character._controllers");
			for(int i = 0; i < controllersProperty.arraySize; ++i) {
				SerializedProperty controllerProperty = controllersProperty.Of(i);
				string name = controllerProperty.Of("name").stringValue;
				string guid = controllerProperty.Of("guid").stringValue;
				SerializedProperty parametersProperty = controllerProperty.Of("_parameters");
				CharacterProjectSettings.ParameterDefs[] parameters = new CharacterProjectSettings.ParameterDefs[parametersProperty.arraySize];

				if (!string.IsNullOrEmpty(name) && !table.ContainsKey(guid)) {
					for(int n = 0; n < parameters.Length; ++n) {
						SerializedProperty parameterProperty = parametersProperty.Of(n);
						parameters[n] = new CharacterProjectSettings.ParameterDefs{
							name = parameterProperty.Of("name").stringValue,
							parameterType = (CharacterProjectSettings.ParameterType)parameterProperty.Of("parameterType").intValue,
						};
					}

					table.Add(
						guid,
						CharacterProjectSettings.CharacterController.Create(guid, name, parameters)
					);
				}
			}
			return table;
		}


		private static Dictionary<string, int> GetStatusIndexList(SerializedObject serializedObject) {
			Dictionary<string, int> table = new Dictionary<string, int>();
			SerializedProperty characterUnitsProperty = serializedObject.FindProperty("_character._units");
			for(int i = 0, statusIndex = 0; i < characterUnitsProperty.arraySize; ++i) {
				SerializedProperty characterUnitProperty = characterUnitsProperty.Of(i);
				bool hasStatus = characterUnitProperty.Of("type.hasStatus").boolValue;
				if (hasStatus) {
					string name = characterUnitProperty.Of("name").stringValue;
					string guid = characterUnitProperty.Of("guid").stringValue;
					if (!string.IsNullOrEmpty(name) && !table.ContainsKey(guid)) {
						table.Add(guid, statusIndex);
						statusIndex++;
					}
				}
			}
			return table;
		}
	}
}