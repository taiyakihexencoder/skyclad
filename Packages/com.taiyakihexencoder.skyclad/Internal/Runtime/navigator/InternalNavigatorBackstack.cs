using Unity.Collections;
using Unity.Entities;

namespace skyclad.internalProc {
	public struct InternalNavigatorBackstack : IBufferElementData {
		public FixedString64Bytes path;
	}

	public static class InternalNavigatorBackstackExtensions {
		public static void Push(
			ref this DynamicBuffer<InternalNavigatorBackstack> buffer,
			FixedString64Bytes path,
			FixedString64Bytes popupTo,
			bool inclusive
		) {
			if (!popupTo.IsEmpty) {
				for (int i = buffer.Length-1; i >= 0; --i) {
					if (buffer[i].path == popupTo) {
						for (int j = buffer.Length-1; j > i; --j) {
							buffer.RemoveAt(j);
						}

						if (inclusive) {
							buffer.RemoveAt(i);
						}
						break;
					}
				}
			}

			buffer.Add(
				new InternalNavigatorBackstack { 
					path = path, 
				}
			);
		}

		public static void Pop(
			ref this DynamicBuffer<InternalNavigatorBackstack> buffer,
			int count
		) {
			if (buffer.Length >= count) {
				buffer.RemoveRange(buffer.Length - count, count);
			} else if (buffer.Length > 0) {
				buffer.Clear();
			}
		}
	}
}