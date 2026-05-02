using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace skyclad.editor {
	/// <summary>
	/// if分岐
	/// </summary>
	internal sealed class IfNode : BaseNode {
		private const int PORT_ENTER = 1;
		private const int PORT_TRUE = 2;
		private const int PORT_FALSE = 3;
		private const int PORT_INPUT = 4;

		public override string Extra => toggle.value.ToString();

		private Toggle toggle;

		internal IfNode(string nodeId, bool value) : base(nodeId, GraphUI.NODE_IF) {
			AddInput("", PORT_ENTER, typeof(Port));
			Port condition = AddInput("condition", PORT_INPUT, typeof(bool));

			AddOutput("true", PORT_TRUE, typeof(Port));
			AddOutput("false", PORT_FALSE, typeof(Port));

			toggle = new Toggle();
			toggle.SetValueWithoutNotify(value);
			toggle.RegisterValueChangedCallback(_ => {
				extraChanged?.Invoke(Property.id, Extra);
			});

			condition.Add(toggle);
			title = "If";
		}

		internal override void UpdateExtra(string extra) {
			toggle.SetValueWithoutNotify(bool.Parse(extra));
		}

		internal override void InputPortConnected(Port port) {
			if (port.portType == typeof(bool)) {
				toggle.visible = false;
			}
		}

		internal override void InputPortDisconnected(Port port) {
			if (port.portType == typeof(bool)) {
				toggle.visible = true;
			}
		}
	}

	/// <summary>
	/// forループ
	/// </summary>
	internal class ForNode : BaseNode {
		private const int PORT_ENTER = 1;
		private const int PORT_COMPLETE = 2;
		private const int PORT_LOOP = 3;
		private const int PORT_TIMES = 4;

		public override string Extra => timesField.value.ToString();

		private IntegerField timesField;

		internal ForNode(string nodeId, int times) : base(nodeId, GraphUI.NODE_FOR) {
			AddInput("", PORT_ENTER, typeof(Port));
			Port timesPort = AddInput("times", PORT_TIMES, typeof(int));
			AddOutput("complete", PORT_COMPLETE, typeof(Port));
			AddOutput("loop", PORT_LOOP, typeof(Port));

			timesField = new IntegerField();
			timesField.SetValueWithoutNotify(times);
			timesField.RegisterValueChangedCallback( _ => {
				extraChanged?.Invoke(Property.id, Extra);
			});

			timesPort.Add(timesField);
			title = "For";
		}

		internal override void UpdateExtra(string extra) {
			timesField.SetValueWithoutNotify(int.Parse(extra));
		}

		internal override void InputPortConnected(Port port) {
			if (port.portType == typeof(int)) {
				timesField.visible = false;
			}
		}

		internal override void InputPortDisconnected(Port port) {
			if (port.portType == typeof(int)) {
				timesField.visible = true;
			}
		}
	}

	/// <summary>
	/// Whileループ
	/// </summary>
	internal class WhileNode : BaseNode {
		private const int PORT_ENTER = 1;
		private const int PORT_COMPLETE = 2;
		private const int PORT_LOOP = 3;
		private const int PORT_INPUT = 4;

		public override string Extra => toggle.value.ToString();

		private Toggle toggle;

		internal WhileNode(string nodeId, bool value) : base(nodeId, GraphUI.NODE_WHILE) {
			AddInput("", PORT_ENTER, typeof(Port));
			Port condition = AddInput("condition", PORT_INPUT, typeof(bool));

			AddOutput("complete", PORT_COMPLETE, typeof(Port));
			AddOutput("loop", PORT_LOOP, typeof(Port));

			toggle = new Toggle();
			toggle.SetValueWithoutNotify(value);
			toggle.RegisterValueChangedCallback(_ => {
				extraChanged?.Invoke(Property.id, Extra);
			});

			condition.Add(toggle);
			title = "While";
		}

		internal override void UpdateExtra(string extra) {
			toggle.SetValueWithoutNotify(bool.Parse(extra));
		}

		internal override void InputPortConnected(Port port) {
			if (port.portType == typeof(bool)) {
				toggle.visible = false;
			}
		}

		internal override void InputPortDisconnected(Port port) {
			if (port.portType == typeof(bool)) {
				toggle.visible = true;
			}
		}
	}

	/// <summary>
	/// 開始ノード
	/// </summary>
	internal class EntryNode : BaseNode {
		public override string Extra => "";

		internal EntryNode(string nodeId) : base(nodeId, GraphUI.NODE_ENTRY) {
			AddOutput("", SINGLE_PORT_ID, typeof(Port));
			title = "Entry Point";
			inputContainer.style.display = DisplayStyle.None;
			this.capabilities &= ~(Capabilities.Deletable | Capabilities.Copiable | Capabilities.Selectable | Capabilities.Droppable | Capabilities.Groupable | Capabilities.Movable);
		}

		internal override void UpdateExtra(string extra) { }
	}

	internal class CommentNode : BaseNode {
		private const int COMMENT_LENGTH = 200;

		override public string Extra => _textField.value;

		private Label _counterLabel;
		private TextField _textField;

		internal CommentNode(string nodeId, string comment) : base(nodeId, GraphUI.NODE_COMMENT) {
			mainContainer.style.backgroundColor = new StyleColor(new Color(1.0f, 1.0f, 0.5f));
			titleContainer.style.display = DisplayStyle.None;
			inputContainer.style.display = DisplayStyle.None;
			outputContainer.style.display = DisplayStyle.None;
			Label titleLabel = new Label("Comment");
			titleLabel.style.color = new StyleColor(Color.black);
			titleLabel.style.backgroundColor = new StyleColor(new Color(1f,1f,1f,0f));
			titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			mainContainer.Add(titleLabel);
			
			_textField = new TextField(
				maxLength: COMMENT_LENGTH,
				multiline: true,
				isPasswordField: false,
				maskChar: '*'
			);
			_textField.SetValueWithoutNotify(comment);
			mainContainer.Add(_textField);
			_textField.style.whiteSpace = WhiteSpace.Normal;
			_textField.style.flexGrow = 1f;
			_textField.style.minWidth = 200.0f;
			_textField.style.maxWidth = 375.0f;
			_textField.style.minHeight = 60.0f;
			_textField.RegisterValueChangedCallback( _ => {
				extraChanged(Property.id, _.newValue);
				_counterLabel.text = $"{_.newValue.Length:000} / {COMMENT_LENGTH}";
			});
			VisualElement textFieldInput = _textField.Q("unity-text-input");
			textFieldInput.style.color = new StyleColor(Color.black);
			textFieldInput.style.backgroundColor = new StyleColor(new Color(1f,1f,1f,0f));
			textFieldInput.style.borderBottomWidth = 0.0f;
			textFieldInput.style.borderTopWidth = 0.0f;
			textFieldInput.style.borderLeftWidth = 0.0f;
			textFieldInput.style.borderRightWidth = 0.0f;

			VisualElement panel = new VisualElement();
			panel.style.flexDirection = FlexDirection.Row;
			Box spacer = new Box();
			spacer.style.backgroundColor = new StyleColor(new Color(1f,1f,1f,0f));
			spacer.style.flexGrow = 1f;
			panel.Add(spacer);
			_counterLabel = new Label($"{comment.Length} / {COMMENT_LENGTH}");
			_counterLabel.style.color = new StyleColor(new Color(0.0f, 0.0f, 0.0f, 0.5f));
			_counterLabel.style.backgroundColor = new StyleColor(new Color(1f,1f,1f,0f));
			panel.Add(_counterLabel);
			mainContainer.Add(panel);

			capabilities |= Capabilities.Resizable;
			capabilities &= ~(Capabilities.Collapsible);
		}

		internal override void UpdateExtra(string extra) {
			_textField.SetValueWithoutNotify(extra);
			_counterLabel.text = $"{extra.Length:000} / {COMMENT_LENGTH}";
		}
	}
}