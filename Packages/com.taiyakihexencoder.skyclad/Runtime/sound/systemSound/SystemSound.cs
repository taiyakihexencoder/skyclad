namespace skyclad {
	/// <summary>
	/// 定義済の音声パターン
	/// </summary>
	public static class SystemSound {
		public static PlaySoundSignal Pattern01 => new PlaySoundSignal(
			SoundWaveShape.Sin,
			new PlaySoundNote[] {
				new PlaySoundNote(MusicalScale.C5, 20),
				new PlaySoundNote(MusicalScale.A5, 50),
			}
		);

		public static PlaySoundSignal Pattern02 => new PlaySoundSignal(
			SoundWaveShape.Sin,
			new PlaySoundNote[] {
				new PlaySoundNote(MusicalScale.Gs5, 40),
				new PlaySoundNote(MusicalScale.Cs5, 80),
			}
		);

		public static PlaySoundSignal Pattern03 => new PlaySoundSignal(
			SoundWaveShape.Sin,
			new PlaySoundNote[] {
				new PlaySoundNote(MusicalScale.A4, 20),
				new PlaySoundNote(MusicalScale.NONE, 50),
				new PlaySoundNote(MusicalScale.Ds5, 100),
				new PlaySoundNote(MusicalScale.NONE, 80),
				new PlaySoundNote(MusicalScale.Ds5, 80),
				new PlaySoundNote(MusicalScale.F5, 30),
			}
		);

		public static PlaySoundSignal NoiseHigh => new PlaySoundSignal(
			SoundWaveShape.Noise,
			new PlaySoundNote[] {
				new PlaySoundNote(MusicalScale.C8, 1_200),
			}
		);

		public static PlaySoundSignal NoiseMiddle => new PlaySoundSignal(
			SoundWaveShape.Noise,
			new PlaySoundNote[] {
				new PlaySoundNote(MusicalScale.B5, 1_200),
			}
		);

		public static PlaySoundSignal NoiseLow => new PlaySoundSignal(
			SoundWaveShape.Noise,
			new PlaySoundNote[] {
				new PlaySoundNote(MusicalScale.G2, 1_200),
			}
		);

		public static PlaySoundSignal AttackHigh => new PlaySoundSignal(
			SoundWaveShape.Attack,
			new PlaySoundNote[] {
				new PlaySoundNote(MusicalScale.C8, 100),
			}
		);

		public static PlaySoundSignal AttackMiddle => new PlaySoundSignal(
			SoundWaveShape.Attack,
			new PlaySoundNote[] {
				new PlaySoundNote(MusicalScale.B5, 100),
			}
		);

		public static PlaySoundSignal AttackLow => new PlaySoundSignal(
			SoundWaveShape.Attack,
			new PlaySoundNote[] {
				new PlaySoundNote(MusicalScale.G2, 100),
			}
		);

	}
}