using System.Collections.Generic;
using UnityEngine.UIElements;

namespace skyclad.editor {
	/// <summary>
	/// Dictionaryの更新を一元管理するPopupField
	/// </summary>
	/// <typeparam name="KEY"></typeparam>
	public class DictionaryPopupBuilder<KEY> {
		private List<KEY> _keys;
		private System.Func<KEY, string> _nameConverter;

		private System.Action onDictionaryUpdated;

		public DictionaryPopupBuilder() {
			_keys = new List<KEY>();
			_nameConverter = DefaultCoverter;
		}

		public PopupField<KEY> Generate(KEY defaultValue) {
			PopupField<KEY> popup = new PopupField<KEY>(
				choices: _keys,
				defaultIndex: _keys.FindIndex(_ => _.Equals(defaultValue)),
				formatListItemCallback: _nameConverter,
				formatSelectedValueCallback: _nameConverter
			);

			System.Action onUpdated = () => {
				popup.choices = _keys;
				popup.formatListItemCallback = _nameConverter;
				popup.formatSelectedValueCallback = _nameConverter;
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

		public DictionaryPopupBuilder<KEY> SetKeys(List<KEY> keys) {
			_keys = keys;
			onDictionaryUpdated?.Invoke();
			return this;
		}

		public DictionaryPopupBuilder<KEY> SetConverter(System.Func<KEY, string> converter) {
			_nameConverter = converter ?? DefaultCoverter;
			return this;
		}

		private string DefaultCoverter(KEY key) {
			return key.ToString();
		}
	}
}