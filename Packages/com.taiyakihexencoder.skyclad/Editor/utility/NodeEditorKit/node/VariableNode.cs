using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace skyclad.editor {
	public abstract class VariableNode<T> : BaseNode {
		protected string variableGuid;
		protected PopupField<string> _popupField;

		public sealed override string Extra => variableGuid;

		public VariableNode(
			string nodeId,
			int type,
			string variableGuid, 
			List<string> variableGuids, 
			List<string> variableNames
		) : base(nodeId, type) {
			title = "Variable";
			this.variableGuid = variableGuid;
			_popupField = new PopupField<string>(
				choices: variableGuids,
				defaultIndex: variableGuids.FindIndex(_ => _ == variableGuid),
				formatListItemCallback: guid => {
					int index = variableGuids.FindIndex(_ => _ == guid);
					return index >= 0 ? variableNames[index] : " - ";
				},
				formatSelectedValueCallback: guid => {
					int index = variableGuids.FindIndex(_ => _ == guid);
					return index >= 0 ? variableNames[index] : " - ";
				}
			);
			_popupField.RegisterValueChangedCallback(_ => {
				extraChanged?.Invoke(Property.id, _.newValue);
				variableGuid = _.newValue;
			});
			Port outPort = AddOutput("", SINGLE_PORT_ID, typeof(T));
			outPort.Add(_popupField);

			inputContainer.style.display = DisplayStyle.None;
		}

		public void RefreshPopup() {
			_popupField.MarkDirtyRepaint();
			int index = _popupField.choices.FindIndex(_ => _ == _popupField.value);
			if (index < 0) {
				_popupField.value = "";
			}
		}

		internal sealed override void UpdateExtra(string extra) {
			variableGuid = extra;
			_popupField.SetValueWithoutNotify(extra);
		}
	}

	public sealed class IntegerVariableNode : VariableNode<int> {
		public IntegerVariableNode(
			string nodeId,
			string variableGuid, 
			List<string> variableGuids, 
			List<string> variableNames
		) : base(
			nodeId, 
			GraphUI.NODE_INTEGER_VARIABLE, 
			variableGuid, 
			variableGuids, 
			variableNames
		) { }
	}

	public sealed class BoolVariableNode : VariableNode<bool> {
		public BoolVariableNode(
			string nodeId,
			string variableGuid, 
			List<string> variableGuids, 
			List<string> variableNames
		) : base(
			nodeId,
			GraphUI.NODE_BOOL_VARIABLE,
			variableGuid, 
			variableGuids, 
			variableNames
		) { }
	}

	public sealed class FloatVariableNode : VariableNode<float> {
		public FloatVariableNode(
			string nodeId,
			string variableGuid, 
			List<string> variableGuids, 
			List<string> variableNames
		) : base(
			nodeId,
			GraphUI.NODE_FLOAT_VARIABLE,
			variableGuid, 
			variableGuids, 
			variableNames
		) { }
	}
}