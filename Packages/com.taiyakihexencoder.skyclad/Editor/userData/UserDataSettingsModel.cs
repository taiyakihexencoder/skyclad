using System.Collections.Generic;
using UnityEditor;

namespace skyclad.editor {
	internal sealed class UserDataSettingsModel {
		private SerializedObject _serializedObject;

		private SerializedProperty _rootProperty;

		internal SerializedProperty IntProperties => _rootProperty.Of("_intParameters");
		internal SerializedProperty BoolProperties => _rootProperty.Of("_boolParameters");
		internal SerializedProperty FloatProperties => _rootProperty.Of("_floatParameters");
		internal SerializedProperty Vector2Properties => _rootProperty.Of("_vector2Parameters");
		internal SerializedProperty Vector3Properties => _rootProperty.Of("_vector3Parameters");

		internal UserDataSettingsModel(SerializedObject serializedObject) {
			_serializedObject = serializedObject;
			_rootProperty = _serializedObject.FindProperty("_userData");
		}

		internal Dictionary<string, string> GetIntParameterList() {
			return GetParameterList(IntProperties);
		}

		internal Dictionary<string, string> GetBoolParameterList() {
			return GetParameterList(BoolProperties);
		}

		internal Dictionary<string, string> GetFloatParameterList() {
			return GetParameterList(FloatProperties);
		}

		internal Dictionary<string, string> GetVector2ParameterList() {
			return GetParameterList(Vector2Properties);
		}

		internal Dictionary<string, string> GetVector3ParameterList() {
			return GetParameterList(Vector3Properties);
		}

		private Dictionary<string, string> GetParameterList(SerializedProperty listProperty) {
			Dictionary<string, string> parameterList = new Dictionary<string, string>();
			for (int i = 0; i < listProperty.arraySize; ++i) {
				parameterList.Add(
					listProperty.Of(i).Of("guid").stringValue,
					listProperty.Of(i).Of("name").stringValue
				);
			}
			return parameterList;
		}

		internal void AddParameter(SerializedProperty listProperty) {
			listProperty.Add(
				(p) => {
					p.Of("guid").stringValue = System.Guid.NewGuid().ToString();
					p.Of("name").stringValue = "";
					SerializedProperty defaultValueProperty = p.Of("defaultValue");
					switch (defaultValueProperty.propertyType) {
						case SerializedPropertyType.Integer: {
							defaultValueProperty.intValue = default;
							break;
						}
						case SerializedPropertyType.Boolean: {
							defaultValueProperty.boolValue = default;
							break;
						}
						case SerializedPropertyType.Float: {
							defaultValueProperty.floatValue = default;
							break;
						}
						case SerializedPropertyType.Vector2: {
							defaultValueProperty.vector2Value = default;
							break;
						}
						case SerializedPropertyType.Vector3: {
							defaultValueProperty.vector3Value = default;
							break;
						}
					}
				}
			);
		}
	}
}