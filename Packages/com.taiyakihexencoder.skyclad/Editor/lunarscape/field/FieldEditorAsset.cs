using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace skyclad.lunarscape.editor {
	internal abstract class FieldEditorAsset : ScriptableObject {
		[SerializeField]
		private string _runtimeAssetAddress = "";
		public string RuntimeAssetAddress => _runtimeAssetAddress;

		[SerializeField]
		private int _id = int.MaxValue;
		public int Id => _id;

		public abstract Vector3 Position { get; }
		public abstract Quaternion Rotation { get; }
		public abstract int MeshCount { get; }
		public abstract string GetName(int index);
		public abstract bool TryGetMesh(int index, out Vector3[] vertices, out int[] indices);
		public abstract bool IsVisible(int index);
	}

	[CustomEditor(typeof(FieldEditorAsset), editorForChildClasses: true)]
	internal class FieldEditorAssetEditor : Editor {
		private Mesh[] meshes = new Mesh[0];

		private void OnEnable() {
			SceneView.duringSceneGui += SceneGUI;
			UpdateMesh();
		}

		private void OnDisable() {
			SceneView.duringSceneGui -= SceneGUI;
			for(int i = 0; i < meshes.Length; ++i) {
				DestroyImmediate(meshes[i]);
			}
			meshes = new Mesh[0];
		}

		public override void OnInspectorGUI() {
			using (var scope = new EditorGUI.ChangeCheckScope()) {
				base.OnInspectorGUI();
				if (scope.changed) {
					UpdateMesh();
				}
			}
		}

		private void SceneGUI(SceneView sceneView) {
			FieldEditorAsset scriptable = target as FieldEditorAsset;

			Vector3 position = scriptable.Position;
			Handles.SphereHandleCap(0, position, Quaternion.identity, 1.0f, EventType.Repaint);
			int meshCount = scriptable.MeshCount;
			for(int i = 0; i < meshCount; ++i) {
				if (scriptable.IsVisible(i) && i < meshes.Length) {
					Graphics.DrawMeshNow(meshes[i], Matrix4x4.Translate(position), -1);
				}
			}
		}

		private void UpdateMesh() {
			FieldEditorAsset scriptable = target as FieldEditorAsset;
			List<Mesh> meshList = new List<Mesh>();
			int meshCount = scriptable.MeshCount;
			for(int i = 0; i < meshCount; ++i) {
				if (scriptable.TryGetMesh(i, out Vector3[] vertices, out int[] indices)) {
					Mesh mesh = new Mesh();
					mesh.SetVertices(vertices);
					mesh.SetIndices(indices, MeshTopology.Triangles, 0);
					mesh.RecalculateNormals();
					meshList.Add(mesh);
				}
			}

			for (int i = 0; i < meshes.Length; ++i) {
				DestroyImmediate(meshes[i]);
			}
			meshes = meshList.ToArray();

		}
	}
}