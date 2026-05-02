using UnityEngine;

namespace skyclad.editor {
	[System.Serializable]
	internal struct NodeProperty {
		public string id;
		public int type;
	}

	[System.Serializable] 
	internal struct HistoryNode {
		public NodeProperty property;
		public Vector2 position;
		public string extra;
	}

	[System.Serializable]
	internal class HistoryDeleted {
		public HistoryNode[] nodes = new HistoryNode[0];
		public GraphUIData.Edge[] edges = new GraphUIData.Edge[0];
	}
}
