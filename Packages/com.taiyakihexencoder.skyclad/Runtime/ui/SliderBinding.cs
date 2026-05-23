namespace skyclad {
	[System.Serializable]
	public class SliderBinding {
		public int value;
		public int lowValue;
		public int highValue;

		public SliderBinding(int value, int lowValue, int highValue) {
			this.value = value;
			this.lowValue = lowValue;
			this.highValue = highValue;
		}
	}
}