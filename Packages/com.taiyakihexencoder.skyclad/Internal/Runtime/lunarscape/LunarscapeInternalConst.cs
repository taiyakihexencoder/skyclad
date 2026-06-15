namespace skyclad.lunarscape.internalProc {
	public static class LunarscapeInternalConst {
		public const uint SCENE_LAYER_TERRAIN = 1u;
		public const uint SCENE_LAYER_PHYSICS_OBJECT = 2u;
		public const uint SCENE_LAYER_COLLIDES_WITH_TERRAIN = SCENE_LAYER_TERRAIN | SCENE_LAYER_PHYSICS_OBJECT;
		public const uint SCENE_LAYER_COLLIDES_WITH_PHYSICS_OBJECT = SCENE_LAYER_TERRAIN;
	}
}