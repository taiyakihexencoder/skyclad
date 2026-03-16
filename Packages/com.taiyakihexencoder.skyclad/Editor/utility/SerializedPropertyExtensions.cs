using UnityEditor;
using UnityEngine;

namespace skyclad.editor {
	public static class SerializedPropertyExtensions {
		public static SerializedProperty Of(this SerializedProperty property, string path) {
			return property.FindPropertyRelative(path);
		}

		public static SerializedProperty Of(this SerializedProperty property, int index) {
			return property.GetArrayElementAtIndex(index);
		}

		public static void Add(
			this SerializedProperty listProperty, 
			System.Action<SerializedProperty> initializer = null
		) {
			if (!listProperty.isArray) {
				Debug.LogWarning($"property is not array!, {listProperty.propertyPath}");
			} else {
				int index = listProperty.arraySize;
				listProperty.arraySize++;
				initializer?.Invoke(listProperty.GetArrayElementAtIndex(index));
			}
		}

		public static void Delete(
			this SerializedProperty listProperty,
			int index
		) {
			listProperty.DeleteArrayElementAtIndex(index);
		}
	}
}