namespace skyclad.bullet {
	[System.Serializable]
	public struct BulletStyle {
		public BulletTrail trail;
		public float lifeTime;
		public float speed;
	}

	public enum BulletTrail {
		None,
		Straight,
	}
}