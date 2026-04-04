using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace skyclad.editor {
	/// <summary>
	/// パラメーター種別
	/// </summary>
	internal enum ParameterType {
		Int,
		Bool,
		Float,
		Float2,
		Float3,
	}

	internal static class ParameterTypeExtension {
		internal static System.Type GetParameterType(this ParameterType type) {
			switch(type) {
				case ParameterType.Int: return typeof(int);
				case ParameterType.Bool: return typeof(bool);
				case ParameterType.Float: return typeof(float);
				case ParameterType.Float2: return typeof(float2);
				case ParameterType.Float3: return typeof(float3);
				default: return null;
			}
		}

		internal static string GetParameterTypeName(this ParameterType type) {
			switch(type) {
				case ParameterType.Int: return "int";
				case ParameterType.Bool: return "bool";
				case ParameterType.Float: return "float";
				case ParameterType.Float2: return "float2";
				case ParameterType.Float3: return "float3";
				default: return "undefined";
			}
		}
	}

	/// <summary>
	/// パラメーターの定義
	/// </summary>
	[System.Serializable]
	internal struct ParameterDefs {
		/// <summary>
		/// 識別子
		/// </summary>
		public string guid;

		/// <summary>
		/// パラメーター種別
		/// </summary>
		public ParameterType parameterType;

		/// <summary>
		/// パラメーター名称
		/// </summary>
		public string name;

		internal static void Editor(SerializedProperty listProperty) {
			using (SkycladEditor.GUI.Layout.Horizontal) {
				SkycladEditor.GUI.Layout.Space(32);
				SkycladEditor.GUI.Layout.Label("name", SkycladEditor.Modifier.ExpandWidth);
				SkycladEditor.GUI.Layout.Label("type", SkycladEditor.Modifier.ExpandWidth);
			}
			for (int i = 0; i < listProperty.arraySize; ++i) {
				using (SkycladEditor.GUI.Layout.Horizontal) {
					SerializedProperty property = listProperty.Of(i);
					SerializedProperty nameProperty = property.Of("name");
					SerializedProperty parameterTypeProperty = property.Of("parameterType");

					if (SkycladEditor.GUI.Layout.MinusButton(width: 32)) {
						listProperty.DeleteArrayElementAtIndex(i);
						break;
					}

					nameProperty.stringValue = SkycladEditor.GUI.Layout.TextField(
						nameProperty.stringValue,
						SkycladEditor.Modifier.ExpandWidth
					);

					parameterTypeProperty.intValue = SkycladEditor.GUI.Layout.EnumField<ParameterType>(
						parameterTypeProperty.intValue,
						SkycladEditor.Modifier.ExpandWidth
					);
				}
			}

			if (SkycladEditor.GUI.Layout.PlusButton()) {
				listProperty.Add(
					p => {
						p.Of("guid").stringValue = System.Guid.NewGuid().ToString();
						p.Of("name").stringValue = "";
						p.Of("parameterType").intValue = (int)(ParameterType)default;
					}
				);
			}
		}

		internal static List<ParameterDefs> AsList(SerializedProperty listProperty) {
			List<ParameterDefs> parameterList = new List<ParameterDefs>();
			SerializedProperty property;
			for (int i = 0; i < listProperty.arraySize; ++i) {
				property = listProperty.Of(i);
				if (!string.IsNullOrEmpty(property.Of("name").stringValue)) {
					parameterList.Add(
						new ParameterDefs {
							guid = property.Of("guid").stringValue,
							parameterType = (ParameterType)property.Of("parameterType").intValue,
							name = property.Of("name").stringValue,
						}
					);
				}
			}
			return parameterList;
		}

		/// <summary>
		/// parameter定義クラスファイルを作成
		/// </summary>
		internal static void CreateParameterClass(
			SerializedProperty listProperty,
			string @namespace,
			string className,
			string path,
			bool partial = true
		) {
			SourceCodeGenerator gen = new SourceCodeGenerator();
			gen.AppendLine($"namespace {@namespace} {{");
			using (gen.IndentBlock) {
				gen.AppendLine($"public {(partial ? "partial" : "")} struct BulletParameter {{");
				using (gen.IndentBlock) {
					for (int i = 0; i < listProperty.arraySize; ++i) {
						SerializedProperty property = listProperty.Of(i);
						ParameterType parameterType = (ParameterType)property.Of("parameterType").intValue;
						gen.AppendLine($"[Order({i})]");
						gen.AppendLine($"public {parameterType.GetParameterTypeName()} {property.Of("name").stringValue};");
					}
				}
				gen.AppendLine($"}}");
			}
			gen.AppendLine($"}}");

			gen.Generate(path);

		}
	}

	/// <summary>
	/// 個別のユニットのパラメーター設定
	/// </summary>
	[System.Serializable]
	internal struct ParameterInfo {
		/// <summary>
		/// ParameterDefsのパラメーター名称
		/// </summary>
		public string[] names;
		/// <summary>
		/// 値
		/// </summary>
		public string[] values;

		public static void Editor(
			SerializedProperty property,
			List<ParameterDefs> parameters
		) {
			SerializedProperty namesProperty = property.Of("names");
			SerializedProperty valuesProperty = property.Of("values");

			foreach(ParameterDefs parameter in parameters) {
				EditorLine(parameter, namesProperty, valuesProperty);
			}
		}

		private static void EditorLine(
			ParameterDefs parameter,
			SerializedProperty namesProperty,
			SerializedProperty valuesProperty
		) {
			Regex regexF2 = new Regex(@"\((?<x>[0-9.]+),(?<y>[0-9.]+)\)");
			Regex regexF3 = new Regex(@"\((?<x>[0-9.]+),(?<y>[0-9.]+),(?<z>[0-9.]+)\)");

			SerializedProperty nameProperty, valueProperty;
			for(int i = 0; i < namesProperty.arraySize; ++i) {
				nameProperty = namesProperty.Of(i);
				if (nameProperty.stringValue != parameter.name) {
					continue;
				}

				valueProperty = valuesProperty.Of(i);

				using (SkycladEditor.GUI.Layout.Horizontal) {
					nameProperty.stringValue = SkycladEditor.GUI.Layout.TextField(
						nameProperty.stringValue,
						SkycladEditor.Modifier
							.ExpandWidth
					);

					string text = valueProperty.stringValue;
					switch(parameter.parameterType) {
						case ParameterType.Int: {
							int value = int.TryParse(valueProperty.stringValue, out int v) ? v : 0;
							value = SkycladEditor.GUI.Layout.IntField(value);
							valueProperty.stringValue = value.ToString();
							break;
						}
						case ParameterType.Bool: {
							bool value = bool.TryParse(valueProperty.stringValue, out bool v) && v;
							value = SkycladEditor.GUI.Layout.Toggle(value);
							valueProperty.stringValue = value.ToString();
							break;
						}
						case ParameterType.Float: {
							float value = float.TryParse(valueProperty.stringValue, out float v) ? v : 0.0f;
							value = SkycladEditor.GUI.Layout.FloatField(value);
							valueProperty.stringValue = value.ToString();
							break;
						}
						case ParameterType.Float2: {
							Match match = regexF2.Match(valueProperty.stringValue);
							Vector2 value = match.Success
								? new Vector2(
									float.Parse(match.Groups["x"].Captures[0].Value),
									float.Parse(match.Groups["y"].Captures[0].Value)
								) : Vector2.zero;
							value = SkycladEditor.GUI.Layout.Vector2Field(value);
							valueProperty.stringValue = value.ToString();
							break;
						}
						case ParameterType.Float3: {
							Match match = regexF3.Match(valueProperty.stringValue);
							Vector3 value = match.Success
								? new Vector3(
									float.Parse(match.Groups["x"].Captures[0].Value),
									float.Parse(match.Groups["y"].Captures[0].Value),
									float.Parse(match.Groups["z"].Captures[0].Value)
								) : Vector3.zero;
							value = SkycladEditor.GUI.Layout.Vector3Field(value);
							valueProperty.stringValue = value.ToString();
							break;
						}
					}
				}
				return;
			}

			// パラメーターがなければ追加
			valuesProperty.Add(
				p => {
					p.stringValue = "";
				}
			);
			namesProperty.Add(
				p => {
					p.stringValue = parameter.name;
				}
			);
		}
	}
}