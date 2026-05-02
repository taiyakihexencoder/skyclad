using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace skyclad.editor {
	internal static class BlackboardExtensions {
		internal static BlackboardRow GetRow(this Blackboard blackboard, string guid) {
			return blackboard.Query<BlackboardRow>()
				.Where(r => r.Q<BlackboardVariableHeader>()?.Guid == guid)
				.First();
		}

		internal static List<BlackboardRow> Rows(this Blackboard blackboard) {
			return blackboard.contentContainer.Query<BlackboardRow>().ToList();
		}

		internal static BlackboardVariableHeader Header(this BlackboardRow row) {
			return row.Q<BlackboardVariableHeader>();
		}
	}

	internal sealed class BlackboardVariableHeader : BlackboardField {
		internal string Guid => userData as string;

		internal BlackboardVariableHeader(in GraphUIData.Variable variable) {
			text = variable.name;
			userData = variable.guid;
			switch(variable.type) {
				case GraphUIData.VariableType.Integer: {
					typeText = "int";
					break;
				}
				case GraphUIData.VariableType.Toggle: {
					typeText = "bool";
					break;
				}
				case GraphUIData.VariableType.Float: {
					typeText = "float";
					break;
				}
			}
		}
	}
}