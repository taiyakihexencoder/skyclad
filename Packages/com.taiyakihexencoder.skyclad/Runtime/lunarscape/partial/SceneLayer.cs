using skyclad.lunarscape.internalProc;

namespace skyclad.lunarscape {
	public partial class SceneLayer {
		public const uint Terrain = LunarscapeInternalConst.SCENE_LAYER_TERRAIN;
		public const uint PhysicsObject = LunarscapeInternalConst.SCENE_LAYER_PHYSICS_OBJECT;
	}

	public partial class SceneLayerCollidesWith {
		public const uint Terrain = LunarscapeInternalConst.SCENE_LAYER_COLLIDES_WITH_TERRAIN;
		public const uint PhysicsObject = LunarscapeInternalConst.SCENE_LAYER_COLLIDES_WITH_PHYSICS_OBJECT;
	}
}