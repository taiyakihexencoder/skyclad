using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace skyclad.editor {
	public sealed class GraphUI : GraphView {
		public const int NODE_INTEGER_VARIABLE = -1;
		public const int NODE_BOOL_VARIABLE = -2;
		public const int NODE_FLOAT_VARIABLE = -3;
		public const int NODE_IF = -10;
		public const int NODE_FOR = -11;
		public const int NODE_WHILE = -12;
		public const int NODE_ENTRY = -20;
		public const int NODE_COMMENT = -21;

		/// <summary>
		/// グラフ内変数
		/// </summary>
		private Blackboard _blackboard;

		/// <summary>
		/// Create Node
		/// </summary>
		private SearchWindowProvider _searchWindowProvider;

		private Sprite _deleteButtonSprite;

		private GraphDataLogic _logic;

		private BlackboardDragControl _dragControl;

		private List<string> _variableIntegerGuids;
		private List<string> _variableIntegerNames;
		private List<string> _variableBoolGuids;
		private List<string> _variableBoolNames;
		private List<string> _variableFloatGuids;
		private List<string> _variableFloatNames;

		private bool _addToHistoryOnGraphViewChange;

		private Vector2 _mousePosition;
		private IGraphSettings _settings;

		private VisualElement _floating;

		/// <summary>
		/// Floating Buttonを配置する場所
		/// </summary>
		public VisualElement Floating => _floating;

		public GraphUI(GraphUIData data, IGraphSettings settings) {
			_settings = settings;

			_variableIntegerGuids = new List<string>();
			_variableIntegerNames = new List<string>();
			_variableBoolGuids = new List<string>();
			_variableBoolNames = new List<string>();
			_variableFloatGuids = new List<string>();
			_variableFloatNames = new List<string>();

			_deleteButtonSprite = SkycladEditorUtility.Asset.GetDefaultIconSprite("CrossIcon");

			_logic = new GraphDataLogic(data, settings.EditorWindowType.Name);

			// 領域いっぱいに広げる
			style.flexGrow = 1;

			// 背景設定
			styleSheets.Add(LoadGridUSS());
			GridBackground background = new GridBackground();
			Insert(0, background);
			background.StretchToParentSize();

			// 変数
			CreateBlackboard(data);

			VisualElement graphContainer = null;
			foreach(VisualElement element in Children()) {
				graphContainer = element;
			}

			// ノード作成ウィンドウ
			_searchWindowProvider = ScriptableObject.CreateInstance<SearchWindowProvider>();
			_searchWindowProvider.requestCreateNode = (node, position) => {
				EditorWindow window = GetEditorWindow();
				Vector2 windowPosition = window?.position.position ?? Vector2.zero;

				Vector2 graphViewLocal = this.WorldToLocal(position - windowPosition);
				Vector2 graphPosition = contentViewContainer.WorldToLocal(graphViewLocal);
				AddToGraph(node, graphPosition);
				OnAddOneNodeToGraph(node, graphPosition);
			};
			_searchWindowProvider.settings = settings;

			// 初期表示位置
			// グラフの(0,0)が見える位置
			schedule.Execute(() => {
				contentViewContainer.style.translate = new StyleTranslate(new Translate(layout.width*0.3f, layout.height*0.3f));
			}).ExecuteLater(20);

			// Floatingボタン
			_floating = new VisualElement();
			_floating.style.position = Position.Absolute;
			_floating.style.bottom = 10.0f;
			_floating.style.right = 10.0f;
			Add(_floating);

			// ノードの移動
			graphViewChanged = OnGraphViewChanged;

			// ズーム
			this.AddManipulator(new ContentZoomer());

			// ドラッグ操作
			this.AddManipulator(new SelectionDragger());
			this.AddManipulator(new ContentDragger());

			// 矩形選択
			this.AddManipulator(new RectangleSelector());

			this.CaptureMouse();

			// キー入力
			RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.NoTrickleDown);
			// マウス位置（グラフエリア）
			RegisterCallback<MouseMoveEvent>(evt => {
				_mousePosition = contentViewContainer.WorldToLocal(evt.mousePosition);
			}, TrickleDown.TrickleDown);

			// ドラッグ操作
			_dragControl = new BlackboardDragControl(graphContainer);
			RegisterCallback<DragPerformEvent>(evt => _dragControl.OnDragPerform(evt, OnDropBlackboardVariableHeader));

			// 移動中のカーソルアイコンの変更
			RegisterCallback<DragUpdatedEvent>(evt => {
				object data = DragAndDrop.GetGenericData("blackboard");
				if (data != null) {
					DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
					evt.StopPropagation();
				}
			});

			_addToHistoryOnGraphViewChange = true;

			Init(data);
		}

		private void Init(GraphUIData data) {
			OnUpdateBlackboardVariables();
			bool existsEntryPoint = false;
			if (data != null) {
				GraphDataLogic.GraphParts parts = _logic.AllGraphElement;
				Dictionary<string, BaseNode> nodeList = new Dictionary<string, BaseNode>();
				foreach(GraphUIData.Node node in parts.nodes) {
					if (TryCreateNode(node.property, node.extra, out BaseNode baseNode)) {
						if (baseNode.Property.type == NODE_ENTRY) {
							existsEntryPoint = true;
						}
						AddToGraph(baseNode, node.position);
						nodeList.Add(node.property.id, baseNode);
					}
				}
				foreach(GraphUIData.Edge edge in parts.edges) {
					if (
						nodeList.TryGetValue(edge.from.guid, out BaseNode from) &&
						nodeList.TryGetValue(edge.to.guid, out BaseNode to) &&
						from.TryGetOutputPort(edge.from.portId, out Port fromPort) &&
						to.TryGetInputPort(edge.to.portId, out Port toPort)
					) {
						AddElement(fromPort.ConnectTo(toPort));
						from.OutputPortConnected(fromPort);
						to.InputPortConnected(toPort);
					}
				}
			}

			// Entry Pointノードの追加
			if (_settings.EntryPoint && !existsEntryPoint) {
				EntryNode entryNode = new EntryNode(System.Guid.NewGuid().ToString());
				AddToGraph(entryNode, Vector2.zero);
				_logic.AddNode(entryNode.Property, Vector2.zero, "", false);
			}
		}

		private GraphViewChange OnGraphViewChanged(GraphViewChange changes) {
			// 移動の検知
			if (changes.movedElements != null) {
				List<string> guids = new List<string>();
				List<Vector2> toArray = new List<Vector2>();
				foreach (VisualElement ve in changes.movedElements) {
					if (ve is BaseNode node) {
						NodeProperty property = node.Property;
						guids.Add(property.id);
						toArray.Add(node.Position);
					}
				}
				_logic.MoveNodes(guids.ToArray(), toArray.ToArray(), _addToHistoryOnGraphViewChange);
			}

			// 削除の検知
			if (changes.elementsToRemove != null) {
				List<string> nodes = new List<string>();
				List<GraphUIData.Edge> edges = new List<GraphUIData.Edge>();
				foreach(VisualElement ve in changes.elementsToRemove) {
					if (ve is BaseNode node) {
						NodeProperty property = node.Property;
						nodes.Add(property.id);
					}

					if (ve is Edge edge) {
						if (edge.output.node is BaseNode from && edge.input.node is BaseNode to) {
							edges.Add(
								new GraphUIData.Edge {
									from = new GraphUIData.Port{ guid = from.Property.id, portId = (int)edge.output.userData, },
									to = new GraphUIData.Port{ guid = to.Property.id, portId = (int)edge.input.userData, },
								}
							);
							from.OutputPortDisconnected(edge.output);
							to.InputPortDisconnected(edge.input);
						}
					}
				}
				_logic.RemoveGraphElements(nodes.ToArray(), edges.ToArray(), _addToHistoryOnGraphViewChange);
			}

			// エッジの追加
			if (changes.edgesToCreate != null) {
				// 手動での接続は複数にならない
				foreach(Edge edge in changes.edgesToCreate) {
					if (edge.output.node is BaseNode from && edge.input.node is BaseNode to) {
						_logic.AddEdge(from.Property.id, (int)edge.output.userData, to.Property.id, (int)edge.input.userData, _addToHistoryOnGraphViewChange);

						from.OutputPortConnected(edge.output);
						to.InputPortConnected(edge.input);
					}
				}
			}

			_addToHistoryOnGraphViewChange = true;
			return changes;
		}

		private void CreateBlackboard(GraphUIData data) {
			_blackboard = new Blackboard(this){
				title = "Blackboard",
				subTitle = "Variables",
				style = { 
					width = 200, 
				}
			};

			// +ボタン
			_blackboard.addItemRequested = (blackboard) => {
				GenericMenu menu = new GenericMenu();
				menu.AddItem(new GUIContent("int"), false, () => AddVariable(blackboard, "new int", 0));
				menu.AddItem(new GUIContent("bool"), false, () => AddVariable(blackboard, "new bool", false));
				menu.AddItem(new GUIContent("float"), false, () => AddVariable(blackboard, "new float", 0.0f));
				menu.ShowAsContext();
			};

			// リネーム
			_blackboard.editTextRequested = (blackboard, element, newName) => {
				if (element is BlackboardVariableHeader header) {
					header.text = newName;
					_logic.RenameVariable(header.Guid, newName);
					OnUpdateBlackboardVariables();
				}
			};

			// セーブされたデータの読み込み
			if (data != null) {
				foreach(GraphUIData.Variable variable in data.Variables) {
					AppendBlackboardRowFromVariable(variable);
				}
			}

			Add(_blackboard);
		}

		private void AppendBlackboardRowFromVariable(GraphUIData.Variable variable, int index = -1) {
			VisualElement inputField = null;
			switch(variable.type) {
				case GraphUIData.VariableType.Integer: {
					IntegerField input = new IntegerField();
					input.SetValueWithoutNotify(int.Parse(variable.value));
					inputField = input;
					break;
				}
				case GraphUIData.VariableType.Toggle: {
					Toggle input = new Toggle();
					input.SetValueWithoutNotify(bool.Parse(variable.value));
					inputField = input;
					break;
				}
				case GraphUIData.VariableType.Float: {
					FloatField input = new FloatField();
					input.SetValueWithoutNotify(float.Parse(variable.value));
					inputField = input;
					break;
				}
			}
			if (inputField != null) {
				inputField.name = "input";
			}

			BlackboardVariableHeader header = new BlackboardVariableHeader(variable);
			AddVariableCommon(_blackboard, header, inputField, index);
		}

		public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter) {
			List<Port> compatiblePorts = new List<Port>();
			foreach(Port port in ports.ToList()) {
				if (startPort.node != port.node && startPort.direction != port.direction && startPort.portType == port.portType) {
					compatiblePorts.Add(port);
				}
			}
			return compatiblePorts;
		} 

		/// <summary>
		/// キーイベント
		/// </summary>
		/// <param name="evt"></param>
		private void OnKeyDown(KeyDownEvent evt) {
			if (evt.ctrlKey && evt.keyCode == KeyCode.A) { CommandSelectAll(); }
			if (evt.ctrlKey && evt.keyCode == KeyCode.S) { 
				if (evt.shiftKey) {
					CommandSaveAs();
				} else {
					CommandOverwrite(); 
				}
			}
			if (evt.ctrlKey && evt.keyCode == KeyCode.Z) { CommandUndo(); }
			if (evt.ctrlKey && evt.keyCode == KeyCode.Y) { CommandRedo(); }
			if (evt.ctrlKey && evt.keyCode == KeyCode.X) { CommandCut(); }
			if (evt.ctrlKey && evt.keyCode == KeyCode.C) { CommandCopy(); }
			if (evt.ctrlKey && evt.keyCode == KeyCode.V) { CommandPaste(); }
		}

		/// <summary>
		/// Ctrl+A 全選択
		/// </summary>
		private void CommandSelectAll() {
			if (selection.Count > 0) {
				selection.Clear();
			} else {
				foreach(Node node in nodes) {
					AddToSelection(node);
				}

				foreach(Edge edge in edges) {
					AddToSelection(edge);
				}
			}
		}

		/// <summary>
		/// Ctrl+Shift+S 名前をつけて保存
		/// </summary>
		private void CommandSaveAs() {
			string path = EditorUtility.SaveFilePanelInProject("Save Graph", "newGraphData", "asset", "asset|asset");
			if (!string.IsNullOrEmpty(path)) {
				_logic.SaveAs(path);
			}
		}

		/// <summary>
		/// Ctrl+S 上書き保存
		/// </summary>
		private void CommandOverwrite() {
			if (_logic.IsNew) {
				string path = EditorUtility.SaveFilePanelInProject("Save Graph", "newGraphData", "asset", "asset|asset");
				if (!string.IsNullOrEmpty(path)) {
					_logic.SaveAs(path);
				}
			} else {
				_logic.Overwrite();
			}
		}

		/// <summary>
		/// Ctrl+Z 元に戻す
		/// </summary>
		private void CommandUndo() {
			if (_logic.History.Undo(out GraphUIHistory.GraphUILog log)) {
				switch(log.type) {
					case GraphUIHistory.GraphUILogType.AddVariable: UndoAddVariable(log); break;
					case GraphUIHistory.GraphUILogType.RemoveVariable: UndoRemoveVariable(log); break;
					case GraphUIHistory.GraphUILogType.RenameVariable: UndoRenameVariable(log); break;
					case GraphUIHistory.GraphUILogType.AddNode: UndoAddNode(log); break;
					case GraphUIHistory.GraphUILogType.RemoveGraphElements: UndoRemoveGraphElements(log); break;
					case GraphUIHistory.GraphUILogType.MoveNode: UndoMoveNode(log); break;
					case GraphUIHistory.GraphUILogType.AddEdge: UndoAddEdge(log); break;
					case GraphUIHistory.GraphUILogType.ChangeExtra: UndoChangeExtra(log); break;
					case GraphUIHistory.GraphUILogType.Paste: UndoPaste(log); break;
				}
			}
		}

		private void UndoAddVariable(GraphUIHistory.GraphUILog log) {
			int index = int.Parse(log.data[..4]);
			GraphUIData.Variable variable = JsonUtility.FromJson<GraphUIData.Variable>(log.data[5..]);
			BlackboardRow row = _blackboard.GetRow(variable.guid);

			if (row != null) {
				row.RemoveFromHierarchy();
				_logic.RemoveVariable(variable.guid, addLog: false);
				OnUpdateBlackboardVariables();
			}
		}

		private void UndoRemoveVariable(GraphUIHistory.GraphUILog log) {
			int index = int.Parse(log.data[..4]);
			GraphUIData.Variable variable = JsonUtility.FromJson<GraphUIData.Variable>(log.data[5..]);
			_logic.AddVariable(variable, false);
			OnUpdateBlackboardVariables();
			AppendBlackboardRowFromVariable(variable, index);
		}

		private void UndoRenameVariable(GraphUIHistory.GraphUILog log) {
			string[] split = log.data.Split(',');
			string guid = split[0];
			string from = split[1];

			BlackboardRow row = _blackboard.GetRow(guid);
			if (row != null) {
				BlackboardVariableHeader header = row.Header();
				header.text = from;
				_logic.RenameVariable(guid, from, false);
				OnUpdateBlackboardVariables();
			}
		}

		private void UndoAddNode(GraphUIHistory.GraphUILog log) {
			HistoryNode history = JsonUtility.FromJson<HistoryNode>(log.data);
			Node deleteNode = null;
			foreach(Node node in nodes) {
				if (node is BaseNode baseNode) {
					NodeProperty property = baseNode.Property;
					if (property.id == history.property.id) {
						deleteNode = node;
						break;
					}
				}
			}

			if (deleteNode != null) {
				// Edgeもまとめて消してもらうためにgraphViewChangedで消す
				_addToHistoryOnGraphViewChange = false;
				selection.Clear();
				AddToSelection(deleteNode);
				DeleteSelection();
			}
		}

		private void UndoRemoveGraphElements(GraphUIHistory.GraphUILog log) {
			HistoryDeleted deleted = JsonUtility.FromJson<HistoryDeleted>(log.data);

			// ノードの復元
			foreach (HistoryNode node in deleted.nodes) {
				CreateNodeFromLog(node);
			}

			// 接続の復元
			List<Port> fromPorts = new List<Port>();
			List<Port> toPorts = new List<Port>();
			foreach (GraphUIData.Edge edge in deleted.edges) {
				foreach (Node node in nodes) {
					if (node is BaseNode baseNode) {
						if (baseNode.Property.id == edge.from.guid && baseNode.TryGetOutputPort(edge.from.portId, out Port output)) {
							fromPorts.Add(output);
						} else if (baseNode.Property.id == edge.to.guid && baseNode.TryGetInputPort(edge.to.portId, out Port input)) {
							toPorts.Add(input);
						}
					}
				}
			}

			if (deleted.edges.Length != fromPorts.Count || deleted.edges.Length != toPorts.Count) {
				Debug.LogError($"port pair not valid. deleteEdges:{deleted.edges.Length}, fromPorts:{fromPorts.Count}, toPorts:{toPorts.Count}");
			} else {
				for(int i = 0; i < fromPorts.Count; ++i) {
					AddElement(fromPorts[i].ConnectTo(toPorts[i]));
					(fromPorts[i].node as BaseNode).OutputPortConnected(fromPorts[i]);
					(toPorts[i].node as BaseNode).InputPortConnected(toPorts[i]);
					_logic.AddEdge(
						deleted.edges[i].from.guid, 
						deleted.edges[i].from.portId, 
						deleted.edges[i].to.guid, 
						deleted.edges[i].to.portId,
						false
					);
				}
			}
		}

		private void UndoMoveNode(GraphUIHistory.GraphUILog log) {
			string[] split = log.data.Split('/');
			int stride = 5;
			string[] guids = new string[split.Length / stride];
			Vector2[] toArray = new Vector2[guids.Length];
			for (int i = 0, idx = 0; i < split.Length; i += stride, ++idx) {
				guids[idx] = split[i];
				foreach(VisualElement ve in nodes) {
					if (ve is BaseNode node && node.Property.id == guids[idx]) {
						toArray[idx] = new Vector2(float.Parse(split[i+1]), float.Parse(split[i+2]));
						node.Position = toArray[idx];
					}
				}
			}
			_logic.MoveNodes(guids, toArray, false);
		}

		private void UndoAddEdge(GraphUIHistory.GraphUILog log) {
			GraphUIData.Edge edge = JsonUtility.FromJson<GraphUIData.Edge>(log.data);
			selection.Clear();
			foreach(Edge e in edges) {
				if (e.output.node is BaseNode from && e.input.node is BaseNode to) {
					if (
						from.Property.id == edge.from.guid && (int)e.output.userData == edge.from.portId &&
						to.Property.id == edge.to.guid && (int)e.input.userData == edge.to.portId
					) {
						AddToSelection(e);
					}
				}
			}

			if (selection.Count > 0) {
				_addToHistoryOnGraphViewChange = false;
				DeleteSelection();
			}
		}

		private void UndoChangeExtra(GraphUIHistory.GraphUILog log) {
			string[] split = log.data.Split('|');
			string guid = split[0];
			string extra = split[1];

			foreach (Node node in nodes) {
				if (node is BaseNode baseNode && baseNode.Property.id == guid) {
					baseNode.UpdateExtra(extra);
					_logic.UpdateExtra(guid, extra, false);
					break;
				}
			}
		}

		private void UndoPaste(GraphUIHistory.GraphUILog log) {
			GraphDataLogic.GraphParts parts = JsonUtility.FromJson<GraphDataLogic.GraphParts>(log.data);
			selection.Clear();
			List<string> nodeGuids = new List<string>();
			foreach(GraphUIData.Node node in parts.nodes) {
				nodeGuids.Add(node.property.id);

				foreach(Node n in nodes) {
					if (n is BaseNode baseNode && baseNode.Property.id == node.property.id) {
						AddToSelection(n);
					}
				}
			}

			foreach(GraphUIData.Edge edge in parts.edges) {
				foreach(Edge e in edges) {
					if (
						e.output.node is BaseNode from && from.Property.id == edge.from.guid &&
						e.input.node is BaseNode to && to.Property.id == edge.to.guid &&
						(int)e.output.userData == edge.from.portId &&
						(int)e.input.userData == edge.to.portId
					) {
						AddToSelection(e);
					}
				}
			}
			_addToHistoryOnGraphViewChange = false;
			DeleteSelection();
		}

		/// <summary>
		/// Ctrl+Y やり直す
		/// </summary>
		private void CommandRedo() {
			if (_logic.History.Redo(out GraphUIHistory.GraphUILog log)) {
				switch(log.type) {
					case GraphUIHistory.GraphUILogType.AddVariable: RedoAddVariable(log); break;
					case GraphUIHistory.GraphUILogType.RemoveVariable: RedoRemoveVariable(log); break;
					case GraphUIHistory.GraphUILogType.RenameVariable: RedoRenameVariable(log); break;
					case GraphUIHistory.GraphUILogType.AddNode: RedoAddNode(log); break;
					case GraphUIHistory.GraphUILogType.RemoveGraphElements: RedoRemoveGraphElements(log); break;
					case GraphUIHistory.GraphUILogType.MoveNode: RedoMoveNode(log); break;
					case GraphUIHistory.GraphUILogType.AddEdge: RedoAddEdge(log); break;
					case GraphUIHistory.GraphUILogType.ChangeExtra: RedoChangeExtra(log); break;
					case GraphUIHistory.GraphUILogType.Paste: RedoPaste(log); break;
				}
			}
		}

		private void RedoAddVariable(GraphUIHistory.GraphUILog log) {
			int index = int.Parse(log.data[..4]);
			GraphUIData.Variable variable = JsonUtility.FromJson<GraphUIData.Variable>(log.data[5..]);
			_logic.AddVariable(variable, false);
			OnUpdateBlackboardVariables();
			AppendBlackboardRowFromVariable(variable, index);
		}

		private void RedoRemoveVariable(GraphUIHistory.GraphUILog log) {
			int index = int.Parse(log.data[..4]);
			GraphUIData.Variable variable = JsonUtility.FromJson<GraphUIData.Variable>(log.data[5..]);
			BlackboardRow row = _blackboard.GetRow(variable.guid);

			if (row != null) {
				row.RemoveFromHierarchy();
				_logic.RemoveVariable(variable.guid, addLog: false);
				OnUpdateBlackboardVariables();
			}
		}

		private void RedoRenameVariable(GraphUIHistory.GraphUILog log) {
			string[] split = log.data.Split(',');
			string guid = split[0];
			string to = split[2];

			BlackboardRow row = _blackboard.GetRow(guid);

			if (row != null) {
				BlackboardVariableHeader header = row.Header();
				header.text = to;
				_logic.RenameVariable(guid, to, false);
				OnUpdateBlackboardVariables();
			}
		}

		private void RedoAddNode(GraphUIHistory.GraphUILog log) {
			CreateNodeFromLog(JsonUtility.FromJson<HistoryNode>(log.data));
		}

		private void CreateNodeFromLog(HistoryNode node) {
			CreateNodeFromLog(node.property, node.position, node.extra);
		}

		private void CreateNodeFromLog(in NodeProperty property, Vector2 position, string extra) {
			if (TryCreateNode(property, extra, out BaseNode node)) {
				AddToGraph(node, position);
				OnAddOneNodeToGraph(node, position, false);
			}
		}

		private void RedoRemoveGraphElements(GraphUIHistory.GraphUILog log) {
			selection.Clear();

			HistoryDeleted deleted = JsonUtility.FromJson<HistoryDeleted>(log.data);

			// Edges
			List<Edge> deleteEdges = new List<Edge>();
			foreach(GraphUIData.Edge edge in deleted.edges) {
				foreach (Edge e in edges) {
					if (e.output.node is BaseNode from && e.input.node is BaseNode to) {
						if (
							from.Property.id == edge.from.guid && (int)e.output.userData == edge.from.portId &&
							to.Property.id == edge.to.guid && (int)e.input.userData == edge.to.portId
						) {
							AddToSelection(e);
						}
					}
				}
			}

			// Nodes
			List<BaseNode> deleteNodes = new List<BaseNode>();
			foreach (HistoryNode history in deleted.nodes) {
				foreach (Node node in nodes) {
					if (node is BaseNode baseNode) {
						if (baseNode.Property.id == history.property.id) {
							deleteNodes.Add(baseNode);
							AddToSelection(node);
							break;
						}
					}
				}
			}

			// graphViewChangesで消してもらう
			if (selection.Count > 0) {
				_addToHistoryOnGraphViewChange = false;
				DeleteSelection();
			}
		}

		private void RedoMoveNode(GraphUIHistory.GraphUILog log) {
			string[] split = log.data.Split('/');
			int stride = 5;
			string[] guids = new string[split.Length / stride];
			Vector2[] toArray = new Vector2[guids.Length];
			for (int i = 0, idx = 0; i < split.Length; i += stride, ++idx) {
				guids[idx] = split[i];
				foreach(VisualElement ve in nodes) {
					if (ve is BaseNode node && node.Property.id == guids[idx]) {
						toArray[idx] = new Vector2(float.Parse(split[i+3]), float.Parse(split[i+4]));
						node.Position = toArray[idx];
					}
				}
			}
			_logic.MoveNodes(guids, toArray, false);
		}

		private void RedoAddEdge(GraphUIHistory.GraphUILog log) {
			GraphUIData.Edge edge = JsonUtility.FromJson<GraphUIData.Edge>(log.data);
			Port fromPort = null;
			Port toPort = null;
			foreach (Node node in nodes) {
				if (node is BaseNode baseNode) {
					if (baseNode.Property.id == edge.from.guid && baseNode.TryGetOutputPort(edge.from.portId, out Port output)) {
						fromPort = output;
						if (toPort != null) { break; }
					} else if (baseNode.Property.id == edge.to.guid && baseNode.TryGetInputPort(edge.to.portId, out Port input)) {
						toPort = input;
						if (fromPort != null) { break; }
					}
				}
			}
			if (fromPort != null && toPort != null) {
				AddElement(fromPort.ConnectTo(toPort));
				(fromPort.node as BaseNode).OutputPortConnected(fromPort);
				(toPort.node as BaseNode).InputPortConnected(toPort);
				_logic.RemoveGraphElements(new string[0], new GraphUIData.Edge[]{edge}, false);
			} else {
				Debug.LogError($"port pair not valid. {edge.from.guid}({edge.from.portId}) -> {edge.to.guid}({edge.to.portId})");
			}
		}

		private void RedoChangeExtra(GraphUIHistory.GraphUILog log) {
			string[] split = log.data.Split('|');
			string guid = split[0];
			string extra = split[2];

			foreach (Node node in nodes) {
				if (node is BaseNode baseNode && baseNode.Property.id == guid) {
					baseNode.UpdateExtra(extra);
					_logic.UpdateExtra(guid, extra, false);
					break;
				}
			}
		}

		private void RedoPaste(GraphUIHistory.GraphUILog log) {
			GraphDataLogic.GraphParts parts = JsonUtility.FromJson<GraphDataLogic.GraphParts>(log.data);

			// ノードの復元
			foreach (GraphUIData.Node node in parts.nodes) {
				if (TryCreateNode(node.property, node.extra, out BaseNode baseNode)) {
					AddToGraph(baseNode, node.position);
				}
			}

			// 接続の復元
			List<Port> fromPorts = new List<Port>();
			List<Port> toPorts = new List<Port>();
			foreach (GraphUIData.Edge edge in parts.edges) {
				foreach (Node node in nodes) {
					if (node is BaseNode baseNode) {
						if (baseNode.Property.id == edge.from.guid && baseNode.TryGetOutputPort(edge.from.portId, out Port output)) {
							fromPorts.Add(output);
						} else if (baseNode.Property.id == edge.to.guid && baseNode.TryGetInputPort(edge.to.portId, out Port input)) {
							toPorts.Add(input);
						}
					}
				}
			}

			if (parts.edges.Length != fromPorts.Count || parts.edges.Length != toPorts.Count) {
				Debug.LogError($"port pair not valid. Edges:{parts.edges.Length}, fromPorts:{fromPorts.Count}, toPorts:{toPorts.Count}");
			} else {
				for(int i = 0; i < fromPorts.Count; ++i) {
					AddElement(fromPorts[i].ConnectTo(toPorts[i]));
					(fromPorts[i].node as BaseNode).OutputPortConnected(fromPorts[i]);
					(toPorts[i].node as BaseNode).InputPortConnected(toPorts[i]);
				}
			}
			_logic.Paste(parts.nodes, parts.edges, false);
		}

		/// <summary>
		/// Ctrl+X 切り取り
		/// </summary>
		private void CommandCut() { 
			CommandCopy();
			DeleteSelection();
		}

		/// <summary>
		/// Ctrl+C コピー
		/// </summary>
		private void CommandCopy() {
			List<GraphUIData.Node> copyNodes = new List<GraphUIData.Node>();
			List<GraphUIData.Edge> copyEdges = new List<GraphUIData.Edge>();
			if (selection.Count > 0) {
				foreach(VisualElement ve in selection) {
					if (ve is BaseNode node) {
						copyNodes.Add(
							new GraphUIData.Node {
								position = node.Position,
								extra = node.Extra,
								property = node.Property,
							}
						);
					} else if (ve is Edge edge) {
						if (edge.output.node is BaseNode from && edge.input.node is BaseNode to) {
							copyEdges.Add(
								new GraphUIData.Edge {
									from = new GraphUIData.Port { guid = from.Property.id, portId = (int)edge.output.userData, },
									to = new GraphUIData.Port { guid = to.Property.id, portId = (int) edge.input.userData, },
								}
							);
						}
					}
				}
				_logic.SetCopySource(copyNodes.ToArray(), copyEdges.ToArray());
			}
		}

		/// <summary>
		/// Ctrl+V 貼り付け
		/// </summary>
		private void CommandPaste() {
			GraphDataLogic.GraphParts parts = _logic.CopySource;
			if (parts == null) { return; }

			// ノードの復元
			// 左上が(0,0)になるように座標変換してあるので、マウス位置を足す
			GraphUIData.Node[] logNodes = new GraphUIData.Node[parts.nodes.Length];
			for (int i = 0; i < parts.nodes.Length; ++i) {
				if (TryCreateNode(parts.nodes[i].property, parts.nodes[i].extra, out BaseNode baseNode)) {
					AddToGraph(baseNode, parts.nodes[i].position + _mousePosition);
					logNodes[i] = parts.nodes[i];
					logNodes[i].position = parts.nodes[i].position + _mousePosition;
				}
			}

			// 接続の復元
			List<Port> fromPorts = new List<Port>();
			List<Port> toPorts = new List<Port>();
			foreach (GraphUIData.Edge edge in parts.edges) {
				foreach (Node node in nodes) {
					if (node is BaseNode baseNode) {
						if (baseNode.Property.id == edge.from.guid && baseNode.TryGetOutputPort(edge.from.portId, out Port outPort)) {
							fromPorts.Add(outPort);
						} else if (baseNode.Property.id == edge.to.guid && baseNode.TryGetInputPort(edge.to.portId, out Port inPort)) {
							toPorts.Add(inPort);
						}
					}
				}
			}

			if (parts.edges.Length != fromPorts.Count || parts.edges.Length != toPorts.Count) {
				Debug.LogError($"port pair not valid. deleteEdges:{parts.edges.Length}, fromPorts:{fromPorts.Count}, toPorts:{toPorts.Count}");
			} else {
				for(int i = 0; i < fromPorts.Count; ++i) {
					AddElement(fromPorts[i].ConnectTo(toPorts[i]));
					(fromPorts[i].node as BaseNode).OutputPortConnected(fromPorts[i]);
					(toPorts[i].node as BaseNode).InputPortConnected(toPorts[i]);
				}
				_logic.Paste(logNodes, parts.edges);
			}
		}

		/// <summary>
		/// Contextual Menu
		/// </summary>
		/// <param name="evt"></param>
		public override void BuildContextualMenu(ContextualMenuPopulateEvent evt) {
			EditorWindow window = GetEditorWindow();
			Vector2 screenPosition = evt.mousePosition + window?.position.position ?? Vector2.zero;
			evt.menu.AppendAction("Create Node", _ => {
				SearchWindow.Open(
					new SearchWindowContext(screenPosition),
					_searchWindowProvider
				);
			});
		}

		/// <summary>
		/// BlackboardVariableHeaderのドラッグからノードを生成
		/// </summary>
		/// <param name="position"></param>
		/// <param name="field"></param>
		private void OnDropBlackboardVariableHeader(Vector2 position, BlackboardVariableHeader field) {
			string guid = (string)field.userData;
			if (guid != null) {
				foreach(BlackboardRow row in _blackboard.Rows()) {
					if (row != null) {
						if (row.Header()?.Guid == guid) {
							FieldInfo fieldInfo = typeof(BlackboardRow).GetField("m_PropertyViewContainer", BindingFlags.NonPublic | BindingFlags.Instance);
							VisualElement container = fieldInfo.GetValue(row) as VisualElement;
							switch(container.Q("input")) {
								case IntegerField: {
									IntegerVariableNode node = new IntegerVariableNode(
										System.Guid.NewGuid().ToString(),
										field.userData as string, 
										_variableIntegerGuids, 
										_variableIntegerNames
									);
									AddToGraph(node, position);
									OnAddOneNodeToGraph(node, position);
									break;
								}
								case Toggle: {
									BoolVariableNode node = new BoolVariableNode(
										System.Guid.NewGuid().ToString(),
										field.userData as string, 
										_variableBoolGuids, 
										_variableBoolNames
									);
									AddToGraph(node, position);
									OnAddOneNodeToGraph(node, position);
									break;
								}
								case FloatField: {
									FloatVariableNode node = new FloatVariableNode(
										System.Guid.NewGuid().ToString(),
										field.userData as string, 
										_variableFloatGuids, 
										_variableFloatNames
									);
									AddToGraph(node, position);
									OnAddOneNodeToGraph(node, position);
									break;
								}
								default: {
									Debug.LogWarning($"not valid type:{container.Q("input").GetType()}");
									break;
								}
							}
						}
					}
				}
			}
		}

		private void AddVariable(
			Blackboard blackboard,
			string name,
			int value
		) {
			GraphUIData.Variable variable = new GraphUIData.Variable {
				guid = System.Guid.NewGuid().ToString(),
				type = GraphUIData.VariableType.Integer,
				name = name,
				value = value.ToString(),
			};
			_logic.AddVariable(variable);
			OnUpdateBlackboardVariables();

			BlackboardVariableHeader header = new BlackboardVariableHeader(variable);
			IntegerField input = new IntegerField();
			input.name = "input";
			AddVariableCommon(blackboard, header, input);
		}

		private void AddVariable(
			Blackboard blackboard,
			string name,
			bool value
		) {
			GraphUIData.Variable variable = new GraphUIData.Variable {
				guid = System.Guid.NewGuid().ToString(),
				type = GraphUIData.VariableType.Toggle,
				name = name,
				value = value.ToString(),
			};
			_logic.AddVariable(variable);
			OnUpdateBlackboardVariables();

			BlackboardVariableHeader header = new BlackboardVariableHeader(variable);
			Toggle input = new Toggle();
			input.name = "input";

			AddVariableCommon(blackboard, header, input);
		}

		private void AddVariable(
			Blackboard blackboard,
			string name,
			float value
		) {
			GraphUIData.Variable variable = new GraphUIData.Variable {
				guid = System.Guid.NewGuid().ToString(),
				type = GraphUIData.VariableType.Float,
				name = name,
				value = value.ToString(),
			};
			_logic.AddVariable(variable);
			OnUpdateBlackboardVariables();

			BlackboardVariableHeader header = new BlackboardVariableHeader(variable);
			FloatField input = new FloatField();
			input.name = "input";

			AddVariableCommon(blackboard, header, input);
		}

		private void AddVariableCommon(
			Blackboard blackboard,
			BlackboardVariableHeader header, 
			VisualElement input,
			int index = -1
		) {
			Button button = new Button(Background.FromSprite(_deleteButtonSprite));
			button.style.backgroundColor = Color.red;
			button.style.width = 24.0f;
			button.style.height = 24.0f;

			VisualElement container = new VisualElement();
			container.style.flexDirection = FlexDirection.Row;
			container.style.flexGrow = 1;
			container.Add(header);
			container.Add(button);

			BlackboardRow row = new BlackboardRow(container, input);
			if (index == -1) {
				blackboard.Add(row);
			} else {
				blackboard.Insert(index, row);
			}
			header.RegisterCallback<PointerDownEvent>(evt => _dragControl.OnPointerDown(evt, header));
			header.RegisterCallback<PointerMoveEvent>(evt => _dragControl.OnPointerMove(evt, header));
			button.clicked += () => {
				row.RemoveFromHierarchy();
				_logic.RemoveVariable(header.Guid);
				OnUpdateBlackboardVariables();
			};
		}

		/// <summary>
		/// Variableが更新されたらリストと表示を更新する
		/// </summary>
		private void OnUpdateBlackboardVariables() {
			_variableIntegerGuids.Clear();
			_variableIntegerNames.Clear();
			_variableBoolGuids.Clear();
			_variableBoolNames.Clear();
			_variableFloatGuids.Clear();
			_variableFloatNames.Clear();

			_variableIntegerGuids.Add("");
			_variableIntegerNames.Add(" - ");

			_variableBoolGuids.Add("");
			_variableBoolNames.Add(" - ");

			_variableFloatGuids.Add("");
			_variableFloatNames.Add(" - ");

			foreach(GraphUIData.Variable variable in _logic.Variables) {
				switch (variable.type) {
					case GraphUIData.VariableType.Integer: {
						_variableIntegerGuids.Add(variable.guid);
						_variableIntegerNames.Add(variable.name);
						break;
					}
					case GraphUIData.VariableType.Toggle: {
						_variableBoolGuids.Add(variable.guid);
						_variableBoolNames.Add(variable.name);
						break;
					}
					case GraphUIData.VariableType.Float: {
						_variableFloatGuids.Add(variable.guid);
						_variableFloatNames.Add(variable.name);
						break;
					}
				}
			}

			foreach(Node node in nodes) {
				switch(node) {
					case IntegerVariableNode integerVariable: {
						integerVariable.RefreshPopup();
						break;
					}
					case BoolVariableNode boolVariable: {
						boolVariable.RefreshPopup();
						break;
					}
					case FloatVariableNode floatVariable: {
						floatVariable.RefreshPopup();
						break;
					}
				}
			}
		}

		private void AddToGraph(BaseNode node, Vector2 position) {
			node.extraChanged += OnExtraChanged;
			AddElement(node);
			node.Position = position;
		}

		private void OnAddOneNodeToGraph(BaseNode node, Vector2 position, bool addLog = true) {
			_logic.AddNode(node.Property, position, node.Extra, addLog);
		}

		private void OnExtraChanged(string guid, string extra) {
			_logic.UpdateExtra(guid, extra);
		}

		private bool TryCreateNode(
			in NodeProperty property,
			string extra,
			out BaseNode node
		) {
			switch(property.type) {
				case NODE_INTEGER_VARIABLE: {
					node = new IntegerVariableNode(
						property.id, 
						extra, 
						_variableIntegerGuids, 
						_variableIntegerNames
					);
					return true;
				}
				case NODE_BOOL_VARIABLE: {
					node = new BoolVariableNode(
						property.id,
						extra,
						_variableBoolGuids,
						_variableBoolNames
					);
					return true;
				}
				case NODE_FLOAT_VARIABLE: {
					node = new FloatVariableNode(
						property.id,
						extra,
						_variableFloatGuids,
						_variableFloatNames
					);
					return true;
				}
				case NODE_IF: {
					node = new IfNode(
						property.id,
						bool.Parse(extra)
					);
					return true;
				}
				case NODE_FOR: {
					node = new ForNode(
						property.id,
						int.Parse(extra)
					);
					return true;
				}
				case NODE_WHILE: {
					node = new WhileNode(
						property.id,
						bool.Parse(extra)
					);
					return true;
				}
				case NODE_ENTRY: {
					node = new EntryNode(property.id);
					return true;
				}
				case NODE_COMMENT: {
					node = new CommentNode(
						property.id,
						extra
					);
					return true;
				}
				default: {
					if (_settings.TryCreateNode(property.id, property.type, out BaseNode customNode)) {
						node = customNode;
						return true;
					} else {
						node = null;
						return false;
					}
				}
			}
		}

		private EditorWindow GetEditorWindow() {
			// ownerObject(DockArea)を取得
  			PropertyInfo ownerObjectPropertyInfo = panel.GetType().GetProperty("ownerObject", BindingFlags.Instance | BindingFlags.Public);
			object ownerObject = ownerObjectPropertyInfo.GetValue(panel);
			// DockAreaのactualViewを取得
			PropertyInfo actualViewPropertyInfo = ownerObject.GetType().GetProperty("actualView", BindingFlags.Instance | BindingFlags.NonPublic);
			return actualViewPropertyInfo.GetValue(ownerObject) as EditorWindow;
		}

		private StyleSheet LoadGridUSS() {
			return AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.taiyakihexencoder.skyclad/Editor/utility/NodeEditorKit/asset/style_grid.uss");
		}

		private class SearchWindowProvider : ScriptableObject, ISearchWindowProvider {
			public System.Action<BaseNode, Vector2> requestCreateNode;
			public IGraphSettings settings;

			List<SearchTreeEntry> ISearchWindowProvider.CreateSearchTree(SearchWindowContext context) {
				List<SearchTreeEntry> entries = new List<SearchTreeEntry> {
					new SearchTreeGroupEntry(new GUIContent("Create Node")),
				};

				if (settings.UseControlNode) {
					entries.Add(new SearchTreeGroupEntry(new GUIContent("Control"), 1));
					entries.Add(new SearchTreeEntry(new GUIContent("If")) { userData = "If", level = 2, });
					entries.Add(new SearchTreeEntry(new GUIContent("For")) { userData = "For", level = 2, });
					entries.Add(new SearchTreeEntry(new GUIContent("While")) { userData = "While", level = 2, });
				}

				if (settings.Entries.Count > 0) {
					entries.Add(new SearchTreeGroupEntry(new GUIContent("Custom"), 1));
				}
				foreach(string customEntry in settings.Entries.Keys) {
					entries.Add(new SearchTreeEntry(new GUIContent(customEntry)) { userData = customEntry, level = 2, });
				}

				entries.Add(new SearchTreeEntry(new GUIContent("Comment")){ userData = "Comment", level = 1, });

				return entries;
			}

			bool ISearchWindowProvider.OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context) {
				string tag = SearchTreeEntry.userData as string;
				switch(tag) {
					case "If": {
						requestCreateNode(
							new IfNode(System.Guid.NewGuid().ToString(), false),
							context.screenMousePosition
						);
						break;
					}
					case "For": {
						requestCreateNode(
							new ForNode(System.Guid.NewGuid().ToString(), 1),
							context.screenMousePosition
						);
						break;
					}
					case "While": {
						requestCreateNode(
							new WhileNode(System.Guid.NewGuid().ToString(), false),
							context.screenMousePosition
						);
						break;
					}
					case "Comment": {
						requestCreateNode(
							new CommentNode(System.Guid.NewGuid().ToString(), ""),
							context.screenMousePosition
						);
						break;
					}
					default: {
						if (settings.Entries.TryGetValue(tag, out int type) && settings.TryCreateNode(System.Guid.NewGuid().ToString(), type, out BaseNode node)) {
							requestCreateNode(node, context.screenMousePosition);
						}
						break;
					}
				}
				return true;
			}
		}

		/// <summary>
		/// Blackboardのドラッグ操作管理
		/// </summary>
		private class BlackboardDragControl {
			private bool _drag;
	  		private VisualElement _container;

			internal BlackboardDragControl(VisualElement container) {
				_container = container;
			}

			/// <summary>
			/// 左クリックでドラッグ開始
			/// </summary>
			/// <param name="evt"></param>
			/// <param name="header"></param>
			internal void OnPointerDown(PointerDownEvent evt, BlackboardVariableHeader header) {
				if (evt.button == 0) {
					_drag = true;
					header.CapturePointer(evt.pointerId);
				}
			}

			/// <summary>
			/// ドラッグデータのセット
			/// </summary>
			/// <param name="evt"></param>
			/// <param name="field"></param>
			internal void OnPointerMove(PointerMoveEvent evt, BlackboardVariableHeader header) {
				if (_drag && evt.pressedButtons == 1) {
					DragAndDrop.PrepareStartDrag();
					DragAndDrop.SetGenericData("blackboard", header);
					DragAndDrop.StartDrag("Dragging");
					_drag = false;
					header.ReleasePointer(evt.pointerId);
					evt.StopPropagation();
				}
			}

			/// <summary>
			/// ドラッグ完了
			/// </summary>
			/// <param name="evt"></param>
			/// <param name="drop"></param>
			internal void OnDragPerform(
				DragPerformEvent evt,
				System.Action<Vector2, BlackboardVariableHeader> drop
			) {
				object data = DragAndDrop.GetGenericData("blackboard");
				switch (data) {
					case BlackboardVariableHeader header: {
						// グラフ座標を取得
						Vector2 position = _container?.WorldToLocal(evt.localMousePosition) ?? evt.localMousePosition;
						drop(position, header);

						DragAndDrop.AcceptDrag();
						evt.StopPropagation();
						break;
					}
				}
			}
		}

		/// <summary>
		/// グラフの設定
		/// </summary>
		public interface IGraphSettings {
			bool UseControlNode { get; }
			bool EntryPoint { get; }

			/// <summary>
			/// 作成のために表示するカスタムEditorWindowの型。
			/// ScriptableObjectのGraphUIDataから
			/// 表示するウィンドウを指定するために使う。
			/// </summary>
			System.Type EditorWindowType { get; }

			/// <summary>
			/// ノードの表示名と型に対応するコード。
			/// コードは重複しないように正の値で設定する。
			/// </summary>
			Dictionary<string, int> Entries { get; }

			/// <summary>
			/// ノードの作成
			/// typeはEntries.Valuesに対応する。
			/// </summary>
			/// <param name="guid"></param>
			/// <param name="type"></param>
			/// <param name="node"></param>
			/// <returns></returns>
			bool TryCreateNode(string guid, int type, out BaseNode node);
		}
	}
}