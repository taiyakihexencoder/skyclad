using System.IO;
using UnityEditor;

namespace skyclad.editor {
	internal static class BulletParameterScriptGenerator {
		internal static void Generate(SerializedObject serializedObject) {
			SerializedProperty parameterDefsProperty = serializedObject.FindProperty("_bullet._parameterDefs");
			ParameterDefs.CreateParameterClass(
				listProperty: parameterDefsProperty, 
				@namespace: "skyclad",
				className: "BulletParameter",
				path: $"bullet{Path.DirectorySeparatorChar}BulletParameter.cs",
				partial: true
			);
		}
	}
}