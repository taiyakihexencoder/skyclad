using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace skyclad.editor {
	internal class BulletSettingsModel {
		private SerializedObject serializedObject;
		private SerializedProperty _bulletProperty;
		private SerializedProperty _groupsProperty => _bulletProperty.Of("_groups");

		internal SerializedProperty ParameterDefsProperty => _bulletProperty.Of("_parameterDefs");

		private int[] _layerValues;
		internal int[] LayerValues => _layerValues;

		private string[] _layerNames;
		internal string[] LayerNames => _layerNames;

		internal BulletSettingsModel(SerializedObject serializedObject) {
			this.serializedObject = serializedObject;
			_bulletProperty = serializedObject.FindProperty("_bullet");

			List<(int, string)> layers = new List<(int, string)>();
			foreach(FieldInfo field in typeof(Layer).GetFields(BindingFlags.Static | BindingFlags.Public)) {
				if (field.IsLiteral && field.FieldType == typeof(uint)) {
					layers.Add(((int)(uint)field.GetValue(null), field.Name));
				}
			}
			_layerNames = new string[layers.Count];
			_layerValues = new int[layers.Count];
			for(int i = 0; i < layers.Count; ++i) {
				_layerValues[i] = layers[i].Item1;
				_layerNames[i] = layers[i].Item2;
			}
		}

		internal System.IDisposable ChangeScope {
			get {
				serializedObject.Update();
				return serializedObject.ChangeCheckScope();
			}
		}

		internal List<string> GroupGuids {
			get {
				List<string> groups = new List<string>();
				SerializedProperty groupsProperty = _groupsProperty;
				for(int i = 0; i < groupsProperty.arraySize; ++i) {
					SerializedProperty groupProperty = groupsProperty.Of(i);
					string guid = groupProperty.Of("guid").stringValue;
					groups.Add(guid);
				}
				return groups;
			}
		}

		internal List<string> GroupNames {
			get {
				List<string> groups = new List<string>();
				SerializedProperty groupsProperty = _groupsProperty;
				for(int i = 0; i < groupsProperty.arraySize; ++i) {
					SerializedProperty groupProperty = groupsProperty.Of(i);
					string name = groupProperty.Of("name").stringValue;
					groups.Add(string.IsNullOrEmpty(name) ? $"Group{i}" : name);
				}
				return groups;
			}
		}

		internal List<ParameterDefs> ParameterList {
			get {
				return ParameterDefs.AsList(_bulletProperty.Of("_parameterDefs"));
			}
		}

		internal SerializedProperty GroupProperty(int groupIndex) {
			return _groupsProperty.Of(groupIndex);
		}

		internal void SetGroupName(int groupIndex, string name) {
			_groupsProperty.Of(groupIndex).Of("name").stringValue = name;
		}

		internal void DeleteGroup(int groupIndex) {
			_groupsProperty.DeleteArrayElementAtIndex(groupIndex);
		}

		internal BulletProjectSettings.Group GetGroup(string guid) {
			SerializedProperty bulletGroupsProperty = _groupsProperty;
			for (int i = 0; i < bulletGroupsProperty.arraySize; ++i) {
				SerializedProperty bulletGroupProperty = bulletGroupsProperty.Of(i);
				string groupGuid = bulletGroupProperty.Of("guid").stringValue;
				if (groupGuid == guid) {
					SerializedProperty unitsProperty = bulletGroupProperty.Of("units");
					BulletProjectSettings.Unit[] units = new BulletProjectSettings.Unit[unitsProperty.arraySize];
					for (int j = 0; j < units.Length; ++j) {
						SerializedProperty unitProperty = unitsProperty.Of(j);
						SerializedProperty parameterNamesProperty = unitProperty.Of("parameter.names");
						SerializedProperty parameterValuesProperty = unitProperty.Of("parameter.values");

						ParameterInfo parameterInfo = new ParameterInfo {
							names = new string[parameterNamesProperty.arraySize],
							values = new string[parameterValuesProperty.arraySize],
						};
						for (int k = 0; k < parameterNamesProperty.arraySize; ++k) {
							parameterInfo.names[k] = parameterNamesProperty.Of(k).stringValue;
							parameterInfo.values[k] = parameterValuesProperty.Of(k).stringValue;
						}

						units[j] = new BulletProjectSettings.Unit {
							name = unitProperty.Of("name").stringValue,
							hitBoxType = (BulletHitBoxType) unitProperty.Of("hitBoxType").intValue,
							extent = unitProperty.Of("extent").vector3Value,
							parameter = parameterInfo,
						};
					}

					return new BulletProjectSettings.Group{
						guid = guid,
						hitBoxLayer = bulletGroupProperty.Of("hitBoxLayer").uintValue,
						name = bulletGroupProperty.Of("name").stringValue,
						units = units,
					};
				}
			}

			return new BulletProjectSettings.Group{
				guid = "",
			};
		}

		internal void AddGroup() {
			_groupsProperty.Add(
				p => {
					p.Of("guid").stringValue = System.Guid.NewGuid().ToString();
					p.Of("name").stringValue = "";
					p.Of("units").arraySize = 0;
					p.Of("hitBoxLayer").intValue = 0;
				}
			);
		}

		internal void AddUnit(int groupIndex) {
			SerializedProperty parameterDefsProperty = _bulletProperty.Of("_parameterDefs");

			GroupProperty(groupIndex).Of("units").Add(
				p => {
					p.Of("name").stringValue = "";
					p.Of("hitBoxType").intValue = 0;
					p.Of("extent").vector3Value = Vector3.zero;
					p.Of("parameter.names").arraySize = parameterDefsProperty.arraySize;
					p.Of("parameter.values").arraySize = parameterDefsProperty.arraySize;
					for(int i = 0; i < parameterDefsProperty.arraySize; ++i) {
						p.Of("parameter.names").Of(i).stringValue = parameterDefsProperty.Of(i).Of("name").stringValue;
						p.Of("parameter.values").Of(i).stringValue = "";
					}
				}
			);
		}
	}

}