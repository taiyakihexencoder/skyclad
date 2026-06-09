namespace skyclad.lunarscape {
	public partial class SceneLayer {
		public const uint Terrain = 1u;
		public const uint PhysicsObject = 2u;
	}

	public partial class SceneLayerCollidesWith {
		public const uint Terrain = SceneLayer.Terrain | SceneLayer.PhysicsObject;
		public const uint PhysicsObject = SceneLayer.Terrain;
	}
}