using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace skyclad.editor {
	/// <summary>
	/// Popupで値を設定
	/// TはEnumまたはpublic const intで選択肢を定義したクラス
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class PopupNode<T> : BaseNode {
		public override string Extra {
			get {
				int idx = sValues.FindIndex(_ => _ == _popupField.value);
				return idx >= 0 ? sNames[idx] : " - ";
			}
		}
		private System.Type _portType;
		private PopupField<int> _popupField;

		private static string[] sNames;
		private static List<int> sValues;

		public PopupNode(string id, int type, int defaultValue) : base(id, type) {
			_portType = typeof(T).IsEnum ? typeof(T) : typeof(int);
			Port outputPort = AddOutput("", SINGLE_PORT_ID, _portType);

			if (_portType == typeof(int)) {
				FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Static | BindingFlags.Public);
				for (int i = 0; i < fields.Length; ++i) {
					if (fields[i].IsLiteral && fields[i].FieldType == typeof(int)) {
						sNames[i] = fields[i].Name;
						sValues.Add((int)fields[i].GetValue(null));
					}
				}
			} else {
				T[] enumValues = (T[])System.Enum.GetValues(typeof(T));
				for (int i = 0; i < enumValues.Length; ++i) {
					sNames[i] = enumValues[i].ToString();
					sValues.Add(System.Convert.ToInt32(enumValues[i]));
				}
			}

			_popupField = new PopupField<int>(
				choices: sValues,
				defaultValue: defaultValue,
				formatSelectedValueCallback: _ => {
					int idx = sValues.FindIndex(value => value == _);
					return idx >= 0 ? sNames[idx] : " - ";
				},
				formatListItemCallback: _ => {
					int idx = sValues.FindIndex(value => value == _);
					return idx >= 0 ? sNames[idx] : " - ";
				}
			);

			outputPort.Add(_popupField);
			inputContainer.style.display = DisplayStyle.None;
		}

		internal override void UpdateExtra(string extra) {
			for (int i = 0; i < sNames.Length; ++i) {
				if (extra == sNames[i]) {
					_popupField.SetValueWithoutNotify(sValues[i]);
					return;
				}
			}
			_popupField.SetValueWithoutNotify(-1);
		}
	}
}