using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace skyclad.lunarscape.editor {
	[CreateAssetMenu(fileName = "FieldSideViewAsset", menuName = "Lunarscape/FieldSideViewAsset")]
	internal sealed class FieldSideViewAsset : ScriptableObject {
		[SerializeField]
		private Vector2 _rootPosition = Vector2.zero;
		public Vector2 RootPosition => _rootPosition;

		[SerializeField]
		private Group[] _groups = new Group[0];
		public Group[] Groups => _groups;

		[System.Serializable]
		public sealed class Group {
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

	[CustomEditor(typeof(FieldSideViewAsset))]
	internal class SideViewAssetEditor : Editor {
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
			FieldSideViewAsset scriptable = target as FieldSideViewAsset;

			Vector2 rootPosition = scriptable.RootPosition;
			Handles.SphereHandleCap(0, rootPosition, Quaternion.identity, 1.0f, EventType.Repaint);

			for(int i = 0; i < scriptable.Groups.Length; ++i) {
				if (scriptable.Groups[i].Visible) {
					Graphics.DrawMeshNow(meshes[i], Matrix4x4.Translate(rootPosition), -1);
				}
			}
		}

		private void UpdateMesh() {
			FieldSideViewAsset scriptable = target as FieldSideViewAsset;

			List<Mesh> meshList = new List<Mesh>();
			foreach(FieldSideViewAsset.Group group in scriptable.Groups) {
				CreateVertexList(group.Points, out Vector3[] vertices, out int[] indices);
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

				LunarscapeEditorSettings._Field fieldSettings = LunarscapeEditorSettings.of.Field;
				
				float halfWidth = fieldSettings.SideView.Width * 0.5f;
				float zOffset = fieldSettings.SideView.ZOffset;
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