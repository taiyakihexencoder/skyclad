using Unity.Collections;
using Unity.Entities;

namespace skyclad {
	public struct NavigatorPush : IComponentData {
		public FixedString64Bytes path;
		public FixedString64Bytes popupTo;
		public bool inclusive;
	}

	public struct NavigatorPop : IComponentData { }

	internal struct NavigatorBackstack : IBufferElementData {
		internal FixedString64Bytes path;
	}

	public struct NavigatorTopChanged : IComponentData, IEnableableComponent {
		public FixedString64Bytes path;
	}
}