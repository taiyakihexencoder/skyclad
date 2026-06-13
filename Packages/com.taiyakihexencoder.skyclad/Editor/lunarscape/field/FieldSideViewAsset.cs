using UnityEngine;

namespace skyclad.lunarscape.editor {
	[CreateAssetMenu(fileName = "FieldSideViewAsset", menuName = "Lunarscape/FieldSideViewAsset")]
	internal sealed class FieldSideViewAsset : FieldEditorAsset {
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

		public override Vector3 Position => new Vector3(_rootPosition.x, _rootPosition.y, LunarscapeEditorSettings.of.Field.SideView.ZOffset);
		public override Quaternion Rotation => Quaternion.identity;
		public override int MeshCount => _groups.Length;
		public override string GetName(int index) {
			if (index < 0 || _groups.Length <= index) {
				return "";
			} else {
				return _groups[index].Name;
			}
		}
		public override bool TryGetMesh(int index, out Vector3[] vertices, out int[] indices){
			if (index < 0 || _groups.Length <= index) {
				vertices = new Vector3[0];
				indices = new int[0];
				return false;
			} else {
				Vector2[] points = _groups[index].Points;
				if (points.Length < 2) {
					vertices = new Vector3[0];
					indices = new int[0];
					return false;
				} else {
					int vertexCount = (points.Length-1) * 6;
					vertices = new Vector3[vertexCount];
					indices = new int[vertexCount];

					float zOffset = LunarscapeEditorSettings.of.Field.SideView.ZOffset;
					float halfWidth = LunarscapeEditorSettings.of.Field.SideView.Width * 0.5f;

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
					return true;
				}
			}
		}

		public override bool IsVisible(int index) {
			return 0 <= index && index < _groups.Length && _groups[index].Visible;
		}
	}
}