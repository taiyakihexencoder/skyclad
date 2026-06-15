using System.Collections.Generic;
using UnityEngine.UIElements;

namespace skyclad.editor {
	/// <summary>
	/// Dictionaryの更新を一元管理するPopupField
	/// </summary>
	/// <typeparam name="KEY"></typeparam>
	public class DictionaryPopupBuilder<KEY> {
		private Dictionary<KEY, string> _data;
		private List<KEY> _keys;
		private System.Func<KEY, string> _nameConverter;

		private System.Action onDictionaryUpdated;

		public DictionaryPopupBuilder(System.Func<KEY, string> formatString = null) {
			_data = new Dictionary<KEY, string>();
			_keys = new List<KEY>();
			_nameConverter = formatString;
		}

		public PopupField<KEY> Generate(KEY defaultValue) {
			PopupField<KEY> popup = new PopupField<KEY>(
				choices: _keys,
				defaultIndex: _keys.FindIndex(_ => _.Equals(defaultValue)),
				formatListItemCallback: _nameConverter ?? DefaultCoverter,
				formatSelectedValueCallback: _nameConverter ?? DefaultCoverter
			);

			System.Action onUpdated = () => {
				popup.choices = _keys;
				popup.formatListItemCallback = _nameConverter ?? DefaultCoverter;
				popup.formatSelectedValueCallback = _nameConverter ?? DefaultCoverter;
			};
			onDictionaryUpdated += onUpdated;

			EventCallback<DetachFromPanelEvent> detach = default;
			detach = evt => {
				onDictionaryUpdated -= onUpdated;
				popup.UnregisterCallback(detach);
			};
			popup.RegisterCallback(detach);

			return popup;
		}

		public void Update(Dictionary<KEY, string> table) {
			_data = table;
			_keys = new List<KEY>(table.Keys);
			onDictionaryUpdated?.Invoke();
		}

		private string DefaultCoverter(KEY key) {
			return _data.TryGetValue(key, out string text) ? text : "-";
		}
	}
}