using UnityEngine;
using UnityEngine.UIElements;

namespace skyclad.editor {
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