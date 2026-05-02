using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace skyclad.editor {
	public sealed class GraphUIData : ScriptableObject {
		[System.Serializable]
		internal class Variable {
			public string guid;
			public VariableType type;
			public string name;
			public string value;
		}

		internal enum VariableType {
			Integer,
			Toggle,
			Float,
		}

		[System.Serializable]
		internal struct Node {
			public Vector2 position;
			public NodeProperty property;
			public string extra;
		}

		[System.Serializable]
		internal struct Edge {
			public Port from;
			public Port to;
		}

		[System.Serializable]
		internal struct Port {
			public string guid;
			public int portId;
		}

		[SerializeField]
		private string _graphCode = "";
		public string GraphCode => _graphCode;

		[SerializeField]
		private Variable[] _variables = new Variable[0];
		internal Variable[] Variables => _variables;

		[SerializeField]
		private Node[] _nodes = new Node[0];
		internal Node[] Nodes => _nodes;

		[SerializeField]
		private Edge[] _edges = new Edge[0];
		internal Edge[] Edges => _edges;
	}

	[CustomEditor(typeof(GraphUIData))]
	public sealed class GraphUIDataEditor : Editor {
		public override void OnInspectorGUI(){
			if (SkycladEditor.GUI.Layout.Button(new GUIContent("Open Editor"))) {
				Assembly[] assemblies = CompilationPipeline.GetAssemblies(AssembliesType.Editor);
				List<System.Reflection.Assembly> editorAssemblies = new List<System.Reflection.Assembly>();

				foreach (System.Reflection.Assembly assembly in System.AppDomain.CurrentDomain.GetAssemblies()) {
					string name = assembly.GetName().Name;
					for (int i = 0; i < assemblies.Length; ++i) {
						if (assemblies[i].name == name) {
							editorAssemblies.Add(assembly);
							break;
						}
					}
				}

				System.Type baseType = typeof(GraphUI.IGraphSettings);
				GraphUIData targetObj = target as GraphUIData;
				foreach(System.Reflection.Assembly assembly in editorAssemblies) {
					foreach(System.Type type in assembly.GetTypes()) {
						if (!type.IsInterface && !type.IsAbstract && baseType.IsAssignableFrom(type)) {
							GraphUI.IGraphSettings settings = (GraphUI.IGraphSettings) System.Activator.CreateInstance(type);
							if (settings.EditorWindowType.Name == targetObj.GraphCode) {
								BaseNodeEditorKitWindow.OpenWindow(settings.EditorWindowType, targetObj);
								break;
							}
						}
					}
				}
			}

			EditorGUILayout.Space();
			base.OnInspectorGUI();
		}
	}
}