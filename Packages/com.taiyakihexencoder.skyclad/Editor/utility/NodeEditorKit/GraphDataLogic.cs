using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace skyclad.editor {
	internal sealed class GraphDataLogic {
		[System.Serializable]
		public class GraphParts {
			public GraphUIData.Node[] nodes;
			public GraphUIData.Edge[] edges;
		}

		private ScriptableObject _original;
		private SerializedObject _serializedObject;

		internal GraphUIHistory History { get; private set; }

		internal GraphParts AllGraphElement {
			get {
				if (_serializedObject == null) { 
					return new GraphParts{nodes = new GraphUIData.Node[0], edges = new GraphUIData.Edge[0], };
				} else {
					GraphUIData data = _serializedObject.targetObject as GraphUIData;
					return new GraphParts{
						nodes = data.Nodes,
						edges = data.Edges,
					};
				}
			}
		}

		private GraphParts _copySource = null;
		internal GraphParts CopySource {
			get {
				if (_copySource == null) {
					return null;
				} else {
					// guidだけ変更する
					List<string> oldGuids = new List<string>();
					List<string> newGuids = new List<string>();
					GraphUIData.Node[] nodes = new GraphUIData.Node[_copySource.nodes.Length];
					for (int i = 0; i < _copySource.nodes.Length; ++i) {
						oldGuids.Add(_copySource.nodes[i].property.id);
						newGuids.Add(System.Guid.NewGuid().ToString());
						nodes[i] = new GraphUIData.Node {
							extra = _copySource.nodes[i].extra,
							position = _copySource.nodes[i].position,
							property = new NodeProperty {
								id = newGuids[i],
								type = _copySource.nodes[i].property.type,
							},
						};
					}

					List<GraphUIData.Edge> edges = new List<GraphUIData.Edge>();
					foreach(GraphUIData.Edge edge in _copySource.edges) {
						int fromIdx = oldGuids.FindIndex(_ => _ == edge.from.guid);
						int toIdx = oldGuids.FindIndex(_ => _ == edge.to.guid);
						if (fromIdx >= 0 && toIdx >= 0) {
							edges.Add(
								new GraphUIData.Edge{
									from = new GraphUIData.Port { guid = newGuids[fromIdx], portId = edge.from.portId, },
									to = new GraphUIData.Port { guid = newGuids[toIdx], portId = edge.to.portId, },
								}
							);
						}
					}

					return new GraphParts {
						nodes = nodes,
						edges = edges.ToArray(),
					};
				}
			}
		}

		/// <summary>
		/// データがすでにファイル化されているか
		/// </summary>
		internal bool IsNew => _original == null;

		internal GraphUIData.Variable[] Variables {
			get {
				GraphUIData data = _serializedObject.targetObject as GraphUIData;
				return data.Variables;
			}
		}

		internal GraphDataLogic(GraphUIData data, string graphCode) {
			_original = data;
			if (_original == null) {
				_serializedObject = new SerializedObject(ScriptableObject.CreateInstance<GraphUIData>());
				_serializedObject.FindProperty("_graphCode").stringValue = graphCode;
			} else {
				_serializedObject = new SerializedObject(Object.Instantiate(_original));
				_serializedObject.FindProperty("_graphCode").stringValue = graphCode;
			}
			_serializedObject.ApplyModifiedProperties();
			History = new GraphUIHistory();
		}

		/// <summary>
		/// 変数の追加
		/// </summary>
		/// <param name="variable"></param>
		/// <param name="addLog"></param>
		internal void AddVariable(GraphUIData.Variable variable, bool addLog = true) {
			if (_serializedObject == null) { return; }

			SerializedProperty variablesProperty = _serializedObject.FindProperty("_variables");
			int index = variablesProperty.arraySize;
			variablesProperty.Add( p => {
				p.Of("guid").stringValue = variable.guid;
				p.Of("type").intValue = (int)variable.type;
				p.Of("name").stringValue = variable.name;
				p.Of("value").stringValue = variable.value;
			});

			_serializedObject.ApplyModifiedProperties();

			if (addLog) {
				History.AddLog(
					GraphUIHistory.GraphUILogType.AddVariable, 
					$"{index:0000}:{EditorJsonUtility.ToJson(variable)}"
				);
			}
		}

		/// <summary>
		/// 変数の削除
		/// </summary>
		/// <param name="guid"></param>
		/// <param name="addLog"></param>
		internal void RemoveVariable(string guid, bool addLog = true) {
			if (_serializedObject == null) { return; }

			SerializedProperty variablesProperty = _serializedObject.FindProperty("_variables");
			for(int i = 0; i < variablesProperty.arraySize; ++i) {
				SerializedProperty variableProperty = variablesProperty.Of(i);
				if (variableProperty.Of("guid").stringValue == guid) {
					GraphUIData.Variable variable = new GraphUIData.Variable {
						guid = guid,
						type = (GraphUIData.VariableType) variableProperty.Of("type").intValue,
						name = variableProperty.Of("name").stringValue,
						value = variableProperty.Of("value").stringValue,
					};
					variablesProperty.DeleteArrayElementAtIndex(i);
					_serializedObject.ApplyModifiedProperties();

					if (addLog) {
						History.AddLog(
							GraphUIHistory.GraphUILogType.RemoveVariable,
							$"{i:0000}:{EditorJsonUtility.ToJson(variable)}"
						);
					}
					break;
				}
			}
		}

		/// <summary>
		/// 変数のリネーム
		/// </summary>
		/// <param name="guid"></param>
		/// <param name="name"></param>
		/// <param name="addLog"></param>
		internal void RenameVariable(string guid, string name, bool addLog = true) {
			if (_serializedObject == null) { return; }

			SerializedProperty variablesProperty = _serializedObject.FindProperty("_variables");
			for (int i = 0; i < variablesProperty.arraySize; ++i) {
				SerializedProperty variableProperty = variablesProperty.Of(i);
				if (variableProperty.Of("guid").stringValue == guid) {
					string oldName = variableProperty.Of("name").stringValue;
					variableProperty.Of("name").stringValue = name;
					_serializedObject.ApplyModifiedProperties();

					if (addLog) {
						History.AddLog(
							GraphUIHistory.GraphUILogType.RenameVariable,
							$"{guid},{oldName},{name}"
						);
					}
					break;
				}
			}
		}

		/// <summary>
		/// ノードの追加
		/// </summary>
		/// <param name="nodeProperty"></param>
		/// <param name="position"></param>
		/// <param name="extra"></param>
		/// <param name="addLog"></param>
		internal void AddNode(
			NodeProperty nodeProperty, 
			Vector2 position,
			string extra,
			bool addLog = true
		) {
			if (_serializedObject == null) { return; }

			SerializedProperty nodesProperty = _serializedObject.FindProperty("_nodes");
			nodesProperty.Add(p => {
				p.Of("position").vector2Value = position;
				p.Of("extra").stringValue = extra;
				p.Of("property.id").stringValue = nodeProperty.id;
				p.Of("property.type").intValue = nodeProperty.type;
			});
			_serializedObject.ApplyModifiedProperties();

			if (addLog) {
				History.AddLog(
					GraphUIHistory.GraphUILogType.AddNode,
					EditorJsonUtility.ToJson(
						new HistoryNode {
							property = nodeProperty,
							position = position,
							extra = extra,
						}
					)
				);
			}
		}

		/// <summary>
		/// グラフ要素の削除
		/// </summary>
		/// <param name="nodeGuids"></param>
		/// <param name="edges"></param>
		/// <param name="addLog"></param>
		internal void RemoveGraphElements(
			string[] nodeGuids,
			GraphUIData.Edge[] edges,
			bool addLog = true
		) {
			if (_serializedObject == null) { return; }

			SerializedProperty edgesProperty = _serializedObject.FindProperty("_edges");
			foreach(GraphUIData.Edge edge in edges) {
				for (int i = 0; i < edgesProperty.arraySize; ++i) {
					SerializedProperty edgeProperty = edgesProperty.Of(i);
					if (
						edgeProperty.Of("from.guid").stringValue == edge.from.guid &&
						edgeProperty.Of("from.portId").intValue == edge.from.portId &&
						edgeProperty.Of("to.guid").stringValue == edge.to.guid &&
						edgeProperty.Of("to.portId").intValue == edge.to.portId
					) {
						edgesProperty.Delete(i);
						break;
					}
				}
			}

			SerializedProperty nodesProperty = _serializedObject.FindProperty("_nodes");
			List<HistoryNode> nodeList = new List<HistoryNode>(); 
			foreach(string guid in nodeGuids) {
				for (int i = 0; i < nodesProperty.arraySize; ++i) {
					SerializedProperty nodeProperty = nodesProperty.Of(i);
					if (nodeProperty.Of("property.id").stringValue == guid) {
						if (addLog) {
							nodeList.Add(
								new HistoryNode {
									property = new NodeProperty {
										id = nodeProperty.Of("property.id").stringValue,
										type = nodeProperty.Of("property.type").intValue,
									},
									position = nodeProperty.Of("position").vector2Value,
									extra = nodeProperty.Of("extra").stringValue
								}
							);
						}
						nodesProperty.Delete(i);
						break;
					} 
				}
			}

			if (addLog) {
				History.AddLog(
					GraphUIHistory.GraphUILogType.RemoveGraphElements,
					EditorJsonUtility.ToJson(
						new HistoryDeleted {
							nodes = nodeList.ToArray(),
							edges = edges,
						}
					)
				);
			}

			_serializedObject.ApplyModifiedProperties();
		}

		/// <summary>
		/// ノードの移動
		/// </summary>
		/// <param name="guids"></param>
		/// <param name="toArray"></param>
		/// <param name="addLog"></param>
		internal void MoveNodes(string[] guids, Vector2[] toArray, bool addLog = true) {
			if (_serializedObject == null) { return; }

			SerializedProperty nodesProperty = _serializedObject.FindProperty("_nodes");
			Vector2[] fromArray = new Vector2[toArray.Length];
			for (int i = 0; i < guids.Length; ++i) {
				for (int j = 0; j < nodesProperty.arraySize; ++j) {
					SerializedProperty nodeProperty = nodesProperty.Of(j);
					if (nodeProperty.Of("property.id").stringValue == guids[i]) {
						fromArray[i] = nodeProperty.Of("position").vector2Value;
						nodeProperty.Of("position").vector2Value = toArray[i];
						break;
					}
				}
			}

			if (addLog) {
				string data = "";
				for(int i = 0; i < guids.Length; ++i) {
					if (i == 0) {
						data += $"{guids[i]}/{fromArray[i].x}/{fromArray[i].y}/{toArray[i].x}/{toArray[i].y}";
					} else {
						data += $"/{guids[i]}/{fromArray[i].x}/{fromArray[i].y}/{toArray[i].x}/{toArray[i].y}";
					}
				}
				History.AddLog(GraphUIHistory.GraphUILogType.MoveNode, data);
			}
			_serializedObject.ApplyModifiedProperties();
		}

		/// <summary>
		/// エッジの追加
		/// </summary>
		/// <param name="fromGuid"></param>
		/// <param name="fromPortId"></param>
		/// <param name="toGuid"></param>
		/// <param name="toPortId"></param>
		/// <param name="addLog"></param>
		internal void AddEdge(string fromGuid, int fromPortId, string toGuid, int toPortId, bool addLog = true) {
			if (_serializedObject == null) { return; }

			SerializedProperty edgesProperty = _serializedObject.FindProperty("_edges");
			edgesProperty.Add( p => {
				p.Of("from.guid").stringValue = fromGuid;
				p.Of("from.portId").intValue = fromPortId;
				p.Of("to.guid").stringValue = toGuid;
				p.Of("to.portId").intValue = toPortId;
			});

			if (addLog) {
				History.AddLog(
					GraphUIHistory.GraphUILogType.AddEdge, 
					EditorJsonUtility.ToJson(
						new GraphUIData.Edge {
							from = new GraphUIData.Port { guid = fromGuid, portId = fromPortId, },
							to = new GraphUIData.Port { guid = toGuid, portId = toPortId, },
						}
					)
				);
			}
			_serializedObject.ApplyModifiedProperties();
		}

		internal void UpdateExtra(string guid, string extra, bool addLog = true) {
			if (_serializedObject == null) { return; }

			SerializedProperty nodesProperty = _serializedObject.FindProperty("_nodes");
			for(int i = 0; i < nodesProperty.arraySize; ++i)  {
				SerializedProperty nodeProperty = nodesProperty.Of(i);
				if (nodeProperty.Of("property.id").stringValue == guid) {
					SerializedProperty extraProperty = nodeProperty.Of("extra");
					if (addLog) {
						History.AddLog(GraphUIHistory.GraphUILogType.ChangeExtra, $"{guid}|{extraProperty.stringValue}|{extra}");
					}
					extraProperty.stringValue = extra;
					_serializedObject.ApplyModifiedProperties();
					break;
				}
			}
		}

		internal void SetCopySource(in GraphUIData.Node[] nodes, in GraphUIData.Edge[] edges) {
			// 位置は左上が(0,0)になるように変更する

			Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
			for (int i = 0; i < nodes.Length; ++i) {
				if (min.x > nodes[i].position.x) {
					min.x = nodes[i].position.x;
				}

				if (min.y > nodes[i].position.y) {
					min.y = nodes[i].position.y;
				}
			}

			GraphUIData.Node[] srcNodes = new GraphUIData.Node[nodes.Length];
			for (int i = 0; i < nodes.Length; ++i) {
				srcNodes[i] = nodes[i];
				srcNodes[i].position -= min;
			}

			GraphUIData.Edge[] srcEdges = new GraphUIData.Edge[edges.Length];
			for (int i = 0; i < edges.Length; ++i) {
				srcEdges[i] = edges[i];
			}

			_copySource = new GraphParts {
				nodes = srcNodes,
				edges = srcEdges,
			};
		}

		internal void ClearCopySource() {
			_copySource = null;
		}

		internal void Paste(
			in GraphUIData.Node[] nodes,
			in GraphUIData.Edge[] edges,
			bool addLog = true
		) {
			if (_serializedObject == null) { return; }

			SerializedProperty nodesProperty = _serializedObject.FindProperty("_nodes");
			for (int i = 0; i < nodes.Length; ++i) {
				Vector2 position = nodes[i].position;
				string extra = nodes[i].extra;
				string guid = nodes[i].property.id;
				int type = nodes[i].property.type;
				nodesProperty.Add(p => {
					p.Of("position").vector2Value = position;
					p.Of("extra").stringValue = extra;
					p.Of("property.id").stringValue = guid;
					p.Of("property.type").intValue = type;
				});
			}

			SerializedProperty edgesProperty = _serializedObject.FindProperty("_edges");
			for (int i = 0; i < edges.Length; ++i) {
				string fromGuid = edges[i].from.guid;
				int fromPort = edges[i].from.portId;
				string toGuid = edges[i].to.guid;
				int toPort = edges[i].to.portId;
				edgesProperty.Add(p => {
					p.Of("from.guid").stringValue = fromGuid;
					p.Of("from.portId").intValue = fromPort;
					p.Of("to.guid").stringValue = toGuid;
					p.Of("to.portId").intValue = toPort;
				});
			}
			_serializedObject.ApplyModifiedProperties();

			if (addLog) {
				History.AddLog(
					GraphUIHistory.GraphUILogType.Paste,
					JsonUtility.ToJson(new GraphParts { nodes = nodes, edges = edges, })
				);
			}
		}

		internal void SaveAs(string path) {
			SkycladEditorUtility.Asset.Create(_serializedObject.targetObject, path);
			_original = _serializedObject.targetObject as GraphUIData;
			_serializedObject = new SerializedObject(Object.Instantiate(_original));
		}

		internal void Overwrite() {
			string name = _original.name;
			EditorUtility.CopySerialized(_serializedObject.targetObject, _original);
			_original.name = name;

			EditorUtility.SetDirty(_original);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}
	}
}