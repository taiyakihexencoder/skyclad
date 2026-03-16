using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace skyclad.editor {
	[CreateAssetMenu(fileName="FieldScriptable2DSideView", menuName = "Skyclad/Field Scriptable")]
	public sealed class FieldScriptable2DSideView : ScriptableObject {
		[SerializeField]
		private Vector2 _rootPosition = Vector2.zero;
		public Vector2 RootPosition => _rootPosition;

		[SerializeField]
		private FieldElement[] _fieldElements = new FieldElement[0];
		public FieldElement[] FieldElements => _fieldElements;

		[System.Serializable]
		public sealed class FieldElement {
			[SerializeField]
			private string _name = "";
			public string Name => _name;

			[SerializeField]
			private bool _visible = true;
			public bool Visible => _visible;

			[SerializeField]
			private Vector2[] _points;
			public Vector2[] Points => _points;
		}
	}

	[CustomEditor(typeof(FieldScriptable2DSideView))]
	internal class FieldScriptable2DSideViewEditor : Editor {
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

		public override void OnInspectorGUI(){
			using (var scope = new EditorGUI.ChangeCheckScope()) {
				base.OnInspectorGUI();
				if (scope.changed) {
					UpdateMesh();
				}
			}
		}

		private void SceneGUI(SceneView sceneView) {
			FieldScriptable2DSideView scriptable = target as FieldScriptable2DSideView;

			Vector2 rootPosition = scriptable.RootPosition;
			Handles.SphereHandleCap(0, rootPosition, Quaternion.identity, 1.0f, EventType.Repaint);

			for(int i = 0; i < scriptable.FieldElements.Length; ++i) {
				if (scriptable.FieldElements[i].Visible) {
					Graphics.DrawMeshNow(meshes[i], Matrix4x4.Translate(rootPosition), -1);
				}
			}
		}

		private void UpdateMesh() {
			FieldScriptable2DSideView scriptable = target as FieldScriptable2DSideView;

			List<Mesh> meshList = new List<Mesh>();
			foreach(FieldScriptable2DSideView.FieldElement element in scriptable.FieldElements) {
				CreateVertexList(element.Points, out Vector3[] vertices, out int[] indices);
				Mesh mesh = new Mesh();
				mesh.SetVertices(vertices);
				mesh.SetIndices(indices, MeshTopology.Triangles, 0);
				mesh.RecalculateNormals();
				meshList.Add(mesh);
			}
			
			for (int i = 0; i < meshes.Length; ++i) {
				DestroyImmediate(meshes[i]);
			}
			meshes = meshList.ToArray();
		}

		internal static void CreateVertexList(Vector2[] points, out Vector3[] vertices, out int[] indices) {
			if (points.Length < 2) {
				vertices = new Vector3[0];
				indices = new int[0];
				return;
			} else {
				int vertexCount = (points.Length-1) * 6;
				vertices = new Vector3[vertexCount];
				indices = new int[vertexCount];

				FieldProjectSettings settings = SkycladProjectSettings.Instance.Field;

				float halfWidth = settings.SideView.Width * 0.5f;
				float zOffset = settings.SideView.ZOffset;
				for(int i = 1, n = 0; i < points.Length; ++i, n += 6) {
					vertices[n] = new Vector3(points[i-1].x, points[i-1].y, -halfWidth + zOffset);
					vertices[n+1] = new Vector3(points[i-1].x, points[i-1].y, halfWidth + zOffset);
					vertices[n+2] = new Vector3(points[i].x, points[i].y, halfWidth + zOffset);
					vertices[n+3] = new Vector3(points[i].x, points[i].y, halfWidth + zOffset);
					vertices[n+4] = new Vector3(points[i].x, points[i].y, -halfWidth + zOffset);
					vertices[n+5] = new Vector3(points[i-1].x, points[i-1].y, -halfWidth + zOffset);

					indices[n] = n;
					indices[n+1] = n+1;
					indices[n+2] = n+2;
					indices[n+3] = n+3;
					indices[n+4] = n+4;
					indices[n+5] = n+5;
				}
			}
		}
	}

}
