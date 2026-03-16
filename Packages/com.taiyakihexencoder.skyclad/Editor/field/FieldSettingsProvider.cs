using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace skyclad.editor {
	internal sealed class FieldSettingsProvider : SettingsProvider {
		private SerializedObject serializedObject;
	
		internal FieldSettingsProvider(
			string path,
			SettingsScope scopes,
			IEnumerable<string> keywords = null
		) : base(path, scopes, keywords) {
			serializedObject = new SerializedObject(SkycladProjectSettings.Instance);
		}

		[SettingsProvider]
		public static SettingsProvider CreateSettingsProvider() {
			return new FieldSettingsProvider(
				path: "Skyclad/Field",
				scopes: SettingsScope.Project,
				keywords: new string[] { "skyclad", }
			);
		}

		public override void OnGUI(string searchContext) {
			using (serializedObject.ChangeCheckScope()) {
				SerializedProperty fieldProperty = serializedObject.FindProperty("_field");
				SerializedProperty fieldTypeProperty = fieldProperty.Of("_fieldType");
				EditorGUILayout.PropertyField(fieldTypeProperty, new GUIContent("Field Type"));

				if (SkycladEditorGUI.Layout.Button("Generate Script")) {
					FieldMeshGenerator.Generate((serializedObject.targetObject as SkycladProjectSettings).Field);
				}
			}
		}
	}
}