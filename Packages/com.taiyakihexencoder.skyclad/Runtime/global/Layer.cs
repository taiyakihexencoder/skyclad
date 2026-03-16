namespace skyclad {
	public static partial class Layer {
		public const uint Terrain = 1u;
		public const uint PhysicsObject = 2u;

		public static partial class CollidesWith {
			public const uint Terrain = Layer.Terrain | Layer.PhysicsObject;
			public const uint PhysicsObject = Layer.Terrain;
		}
	}
}