using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

namespace skyclad.lunarscape.editor {
	using System.IO;
	using skyclad.editor;
	using skyclad.lunarscape.internalProc;

	internal class FieldAssetGenerator : ScriptGenerator {
		private FieldEditorMode _mode;
		internal FieldAssetGenerator(FieldEditorMode mode) {
			_mode = mode;
		}

		internal bool Validation(out string message) {
			StringBuilder sb = new StringBuilder();
			System.Type editorAssetType = _mode.EditorAssetType();
			string[] guids = AssetDatabase.FindAssets($"t:{editorAssetType.Name}");

			bool result = true;
			Regex invalidAddressRegex = new Regex(@"/$");
			Regex invalidFileNameRegex = new Regex(@"[^a-zA-Z0-9_]");
			foreach(string guid in guids) {
				string assetPath = AssetDatabase.GUIDToAssetPath(guid);
				FieldEditorAsset asset = AssetDatabase.LoadAssetAtPath<FieldEditorAsset>(assetPath);
				if (string.IsNullOrEmpty(asset.RuntimeAssetAddress)) {
					sb.Append("Empty address: " + asset.name + System.Environment.NewLine);
					result = false;
				} else if (asset.RuntimeAssetAddress.Length > 62) {
					sb.Append("Too long address: " + asset.RuntimeAssetAddress + System.Environment.NewLine);
				} else if (invalidAddressRegex.IsMatch(asset.RuntimeAssetAddress)) {
					sb.Append("Invalid address: " + asset.RuntimeAssetAddress + System.Environment.NewLine);
					result = false;
				} else if(invalidFileNameRegex.IsMatch(asset.name)) {
					sb.Append("Invalid asset name: " + asset.name + System.Environment.NewLine);
					result = false;
				}
			}
			message = sb.ToString();
			return result;
		}

		protected override void WriteScript() {
			System.Type editorAssetType = _mode.EditorAssetType();
			string[] guids = AssetDatabase.FindAssets($"t:{editorAssetType.Name}");
			FieldEditorAsset[] assets = new FieldEditorAsset[guids.Length];

			for (int i = 0; i < guids.Length; ++i) {
				string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
				assets[i] = AssetDatabase.LoadAssetAtPath<FieldEditorAsset>(assetPath);
			}

			using (Namespace("skyclad.lunarscape")) {
				using (Struct("FieldAssetAddress", isPartial: true, isStatic: false, isReadonly: true)) {
					foreach (FieldEditorAsset asset in assets) {
						AppendLine($"public static readonly FieldAssetAddress {asset.name} = new FieldAssetAddress({asset.Id}, \"{asset.RuntimeAssetAddress}\", \"{asset.name}\");");
					}
				}
			}
		}

		internal void GenerateResources() {
			LunarscapeFieldTable table = ScriptableObject.CreateInstance<LunarscapeFieldTable>();
			SkycladEditorUtility.Asset.Create(table, $"Assets{Path.DirectorySeparatorChar}{LunarEditorConst.AUTO_GENERATE_RESOURCE_PATH}{Path.DirectorySeparatorChar}{typeof(LunarscapeFieldTable).Name}.asset");
			SkycladEditorUtility.Resource.SetAddress(table, LunarscapeFieldTable.RES_ADDRESS);
			SerializedObject tableObj = new SerializedObject(table);
			SerializedProperty tableRowProperties = tableObj.FindProperty("_rows");

			System.Type editorAssetType = _mode.EditorAssetType();
			string[] guids = AssetDatabase.FindAssets($"t:{editorAssetType.Name}");
			FieldEditorAsset[] assets = new FieldEditorAsset[guids.Length];
			for(int i = 0; i < guids.Length; ++i) {
				string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
				assets[i] = AssetDatabase.LoadAssetAtPath<FieldEditorAsset>(assetPath);
			}

			for(int n = 0; n < assets.Length; ++n) {
				FieldEditorAsset asset = assets[n];
				FieldMeshAsset mainAsset = ScriptableObject.CreateInstance<FieldMeshAsset>();
				SkycladEditorUtility.Asset.Create(mainAsset, $"Assets{Path.DirectorySeparatorChar}{LunarEditorConst.AUTO_GENERATE_RESOURCE_PATH}{Path.DirectorySeparatorChar}{typeof(FieldMeshAsset).Name}{Path.DirectorySeparatorChar}{asset.name}.asset");
				SerializedObject mainAssetObj = new SerializedObject(mainAsset);

				SerializedProperty guidProperty = mainAssetObj.FindProperty("guid");
				guidProperty.stringValue = guids[n];

				SerializedProperty subassetsProperty = mainAssetObj.FindProperty("subassets");
				subassetsProperty.arraySize = asset.MeshCount;

				// フィールドの領域範囲
				Vector3 regionMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
				Vector3 regionMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

				// 各メッシュを作成してサブアセットへ
				for(int i = 0, iMax = asset.MeshCount; i < iMax; ++i) {
					if (asset.TryGetMesh(i, out Vector3[] vertices, out int[] indices)) {
						foreach(Vector3 v in vertices) {
							Vector3 pos = asset.Rotation * v;
							if (pos.x < regionMin.x) { regionMin.x = pos.x; } else if (pos.x > regionMax.x) { regionMax.x = pos.x; }
							if (pos.y < regionMin.y) { regionMin.y = pos.y; } else if (pos.y > regionMax.y) { regionMax.y = pos.y; }
							if (pos.z < regionMin.z) { regionMin.z = pos.z; } else if (pos.z > regionMax.z) { regionMax.z = pos.z; }
						}

						if (vertices.Length >= 2) {
							string name = asset.GetName(i);
							if (string.IsNullOrEmpty(name)) {
								name = $"NoName{i.ToString("00")}";
							}
							Mesh mesh = new Mesh();
							mesh.name = name;
							mesh.SetVertices(vertices);
							mesh.SetIndices(indices, MeshTopology.Triangles, 0);
							mesh.RecalculateNormals();
							AssetDatabase.AddObjectToAsset(mesh, mainAsset);
							subassetsProperty.GetArrayElementAtIndex(i).stringValue = name;
						}
					}
				}

				if (regionMin.x > regionMax.x || regionMin.y > regionMax.y || regionMin.z > regionMax.z) {
					regionMin = Vector3.zero;
					regionMax = Vector3.zero;
				}

				SkycladEditorUtility.Resource.SetAddress(mainAsset, asset.RuntimeAssetAddress);

				tableRowProperties.Add(p => {
					p.FindPropertyRelative("id").intValue = asset.Id;
					p.FindPropertyRelative("address").stringValue = asset.RuntimeAssetAddress;
					p.FindPropertyRelative("name").stringValue = asset.name;
					p.FindPropertyRelative("guid").stringValue = guids[n];
					p.FindPropertyRelative("position").vector3Value = asset.Position;
					p.FindPropertyRelative("rotation").quaternionValue = asset.Rotation;
					p.FindPropertyRelative("boundsMin").vector3Value = regionMin + asset.Position;
					p.FindPropertyRelative("boundsMax").vector3Value = regionMax + asset.Position;
				});
				mainAssetObj.ApplyModifiedProperties();
			}
			tableObj.ApplyModifiedProperties();

			AssetDatabase.SaveAssets();
			Debug.Log("Field Asset Generator: Completed");
		}
	}
}