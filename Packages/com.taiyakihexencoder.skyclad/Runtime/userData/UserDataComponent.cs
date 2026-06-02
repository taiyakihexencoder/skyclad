using System.Runtime.InteropServices;
using Unity.Entities;

namespace skyclad.userData {
	[StructLayout(LayoutKind.Auto)]
	public partial struct UserDataComponent : IComponentData {
		private int _dataSize;
		public int dataSize => _dataSize;

		public static UserDataComponent Default {
			get {
				UserDataComponent component = new UserDataComponent();
				component.SetDefault();
				return component;
			}
		}

		internal static UserDataComponent FromBinary(byte[] bytes) {
			UserDataComponent component = Default;
			component.LoadBinary(bytes);
			return component;
		}

		internal void ToBinary(out byte[] bytes) {
			bytes = new byte[_dataSize];
			SaveBinary(bytes);
		}

		partial void SetDefault();

		partial void LoadBinary(byte[] bytes);

		partial void SaveBinary(byte[] bytes);
	}

}