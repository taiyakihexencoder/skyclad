namespace skyclad.lunarscape {
	public readonly partial struct FieldAssetAddress {
		public int Id { get; }
		public string Address { get; }
		internal string Name { get; }

		private FieldAssetAddress(int id, string address, string name) {
			Id = id;
			Address = address;
			Name = name;
		}
	}
}