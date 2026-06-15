namespace skyclad.lunarscape {
	public readonly partial struct AvatarId {
		public int Id { get; }
		internal string Name { get; }

		private AvatarId(int id, string name) {
			Id = id;
			Name = name;
		}
	}
}