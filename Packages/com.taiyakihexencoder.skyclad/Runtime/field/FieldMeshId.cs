namespace skyclad {
	public static partial class FieldMeshId {
		private static string _lastSelected;

		public static string GetAddress(int id) {
			SetAddress(id);
			return _lastSelected;
		}
		static partial void SetAddress(int id);
	}
}