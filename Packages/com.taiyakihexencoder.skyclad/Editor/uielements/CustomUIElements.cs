using UnityEngine;
using UnityEngine.UIElements;

namespace skyclad.editor {
	public sealed class SelectableListView<T> : CommonVisualElement<SelectableListView<T>> where T : class {
		private ScrollView _scrollView;
		private T _selected;
		private Color _selectedColor;

		public System.Action<T> selectionChanged;

		public SelectableListView() {
			_scrollView = new ScrollView(ScrollViewMode.Vertical);
			_scrollView.style.flexDirection = FlexDirection.Column;
			_scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
			Add(_scrollView);
		}

		public SelectableListView<T> SelectedColor(Color color) {
			_selectedColor = color;
			return this;
		}

		public void Select(T key) {
			if (!key.Equals(_selected)) {
				foreach(VisualElement ve in _scrollView.Children()) {
					if (ve.userData is T userData && userData.Equals(key)) {
						_selected = key;
						OnListSelected();
						selectionChanged?.Invoke(userData);
					}
				}
			}
		}

		public void AddSelection(T key, VisualElement ve) {
			ve.userData = key;
			ve.style.width = new StyleLength(StyleKeyword.Auto);
			ve.RegisterCallback<MouseDownEvent>(
				evt => {
					if(evt.button == 0) {
						if (ve.userData is T userData && !userData.Equals(_selected)) {
							_selected = userData;
							OnListSelected();
							selectionChanged?.Invoke(userData);
						}
					}
				}
			);
			_scrollView.Add(ve);
		}

		private void OnListSelected() {
			foreach(VisualElement ve in _scrollView.Children()) {
				if (ve.userData is T userData) {
					Color color = _selectedColor;
					color.a = userData.Equals(_selected) ? 1.0f : 0.0f;
					ve.style.backgroundColor = new StyleColor(color);
				}
			}
		}

		public void Unselect() {
			_selected = null;
			OnListSelected();
			selectionChanged?.Invoke(null);
		}

		public void ClearElements() {
			_scrollView.Clear();
			_selected = null;
		}
	}

	public class Spacer : CommonVisualElement<Spacer> {
		public Spacer(float width = 0f, float height = 0f) {
			style.width = width;
			style.height = height;
			style.marginLeft = 0f;
			style.marginRight = 0f;
			style.marginTop = 0f;
			style.marginBottom = 0f;
			style.minWidth = 0f;
			style.minHeight = 0f;
			style.maxWidth = width;
			style.maxHeight = height;
		}
	}

	public class Row : CommonVisualElement<Row> {
		public Row() {
			style.flexDirection = FlexDirection.Row;
			style.marginLeft = 0f;
			style.marginRight = 0f;
			style.marginTop = 0f;
			style.marginBottom = 0f;
			style.minWidth = 0f;
			style.minHeight = 0f;
		}

		public Row HorizontalArrangement(Justify arrangement) {
			style.justifyContent = arrangement;
			return this;
		}

		public Row VerticalAlignment(Align align) {
			return Align(align);
		}
	}

	public class Column : CommonVisualElement<Column> {
		public Column() {
			style.flexDirection = FlexDirection.Column;
			style.marginLeft = 0f;
			style.marginRight = 0f;
			style.marginTop = 0f;
			style.marginBottom = 0f;
			style.minWidth = 0f;
			style.minHeight = 0f;
		}

		public Column VerticalArrangement(Justify arrangement) {
			style.justifyContent = arrangement;
			return this;
		}

		public Column HorizontalAlignment(Align align) {
			return Align(align);
		}

	}

	public abstract class CommonVisualElement<T> : VisualElement where T : CommonVisualElement<T> {
		public T Align(Align align) {
			style.alignContent = align;
			return this as T;
		}

		public T Padding(float all) {
			style.paddingLeft = all;
			style.paddingRight = all;
			style.paddingTop = all;
			style.paddingBottom = all;
			return this as T;
		}

		public T Padding(float vertical = 0f, float horizontal = 0f) {
			style.paddingLeft = horizontal;
			style.paddingRight = horizontal;
			style.paddingTop = vertical;
			style.paddingBottom = vertical;
			return this as T;
		}

		public T Padding(float left = 0f, float right = 0f, float top = 0f, float bottom = 0f) {
			style.paddingLeft = left;
			style.paddingRight = right;
			style.paddingTop = top;
			style.paddingBottom = bottom;
			return this as T;
		}

		public T Background(Color color) {
			style.backgroundColor = color;
			return this as T;
		}

		public T Border(Color color, float width = 1f, float radius = 0f) {
			style.borderLeftWidth = width;
			style.borderRightWidth = width;
			style.borderTopWidth = width;
			style.borderBottomWidth = width;

			style.borderLeftColor = color;
			style.borderRightColor = color;
			style.borderTopColor = color;
			style.borderBottomColor = color;

			style.borderTopLeftRadius = radius;
			style.borderTopRightRadius = radius;
			style.borderBottomLeftRadius = radius;
			style.borderBottomRightRadius = radius;
			return this as T;
		}

		public T Width(float width) {
			style.width = width;
			return this as T;
		}

		public T Height(float height) {
			style.height = height;
			return this as T;
		}

		public void AddChildren(params VisualElement[] children) {
			foreach(VisualElement child in children) {
				Add(child);
			}
		}
	}
}