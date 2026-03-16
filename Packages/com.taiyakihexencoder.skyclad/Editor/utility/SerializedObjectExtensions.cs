using UnityEditor;

namespace skyclad.editor {
	public static class SerializedObjectExtensions {
		public static System.IDisposable ChangeCheckScope(this SerializedObject serializedObject) {
			return new _ChangeCheckScope(serializedObject);
		}

		private class _ChangeCheckScope : System.IDisposable {
			private SerializedObject serializedObject;
			public _ChangeCheckScope(SerializedObject serializedObject) { 
				this.serializedObject = serializedObject;
				EditorGUI.BeginChangeCheck(); 
			}
			void System.IDisposable.Dispose(){ 
				if (EditorGUI.EndChangeCheck()) {
					serializedObject.ApplyModifiedProperties();
				}
			}
		}

	}
}