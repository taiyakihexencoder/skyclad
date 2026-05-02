using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace skyclad.editor {
	public abstract class BaseNode : Node {
		protected const int SINGLE_PORT_ID = 0;

		public abstract string Extra { get; }
		internal NodeProperty Property => (NodeProperty)userData;
		internal Vector2 Position {
			get => GetPosition().position;
			set => SetPosition(new Rect(value, Vector2.zero));
		}

		/// <summary>
		/// ノード内の変更
		/// </summary>
		internal System.Action<string, string> extraChanged;

		public BaseNode(string id, int type) {
			userData = new NodeProperty {
				id = id,
				type = type,
			};
		}

		protected Port AddInput(string name, int portId, System.Type type) {
			Port port = InstantiatePort(
				orientation: Orientation.Horizontal,
				direction: Direction.Input,
				capacity: type == typeof(Port) ? Port.Capacity.Multi : Port.Capacity.Single,
				type: type
			);
			port.portName = name;
			port.userData = portId;
			inputContainer.Add(port);
			return port;
		}

		protected Port AddOutput(string name, int portId, System.Type type) {
			Port port = InstantiatePort(
				orientation: Orientation.Horizontal,
				direction: Direction.Output,
				capacity: type == typeof(Port) ? Port.Capacity.Single : Port.Capacity.Multi,
				type: type
			);
			port.portName = name;
			port.userData = portId;
			outputContainer.Add(port);
			return port;
		}

		internal abstract void UpdateExtra(string extra);

		internal virtual void InputPortConnected(Port port) { }
		internal virtual void InputPortDisconnected(Port port) { }
		internal virtual void OutputPortConnected(Port port) { }
		internal virtual void OutputPortDisconnected(Port port) { }

		internal bool TryGetInputPort(int portId, out Port port) {
			foreach(VisualElement ve in inputContainer.Children()) {
				if (ve is Port p && (int)p.userData == portId) {
					port = p;
					return true;
				}
			}
			port = null;
			return false;
		}

		internal bool TryGetOutputPort(int portId, out Port port) {
			foreach(VisualElement ve in outputContainer.Children()) {
				if (ve is Port p && (int)p.userData == portId) {
					port = p;
					return true;
				}
			}
			port = null;
			return false;
		}
	}
}