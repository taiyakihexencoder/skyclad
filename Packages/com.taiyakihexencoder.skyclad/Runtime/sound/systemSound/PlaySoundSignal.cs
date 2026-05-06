using Unity.Collections;
using UnityEngine;

namespace skyclad {
	/// <summary>
	/// システム効果音再生リクエスト
	/// </summary>
	public readonly struct PlaySoundSignal {
		public readonly FixedList128Bytes<PlaySoundNote> notes;
		public bool isEmpty => notes.IsEmpty;
		public readonly SoundWaveShape shape;

		/// <summary>
		/// notesは最大15個まで。
		/// </summary>
		/// <param name="shape"></param>
		/// <param name="notes"></param>
		public PlaySoundSignal(SoundWaveShape shape, params PlaySoundNote[] notes) {
			this.shape = shape;
			this.notes = new FixedList128Bytes<PlaySoundNote>();
			foreach(PlaySoundNote note in notes) {
				this.notes.Add(note);
			}
		}
	}

	/// <summary>
	/// システム効果音再生リクエストの１つの音。
	/// </summary>
	public readonly struct PlaySoundNote {
		/// <summary>
		/// 周波数
		/// </summary>
		public readonly MusicalScale scale;

		/// <summary>
		/// フレーム数
		/// サンプリングレート * 再生時間
		/// </summary>
		public readonly int length;

		public PlaySoundNote(MusicalScale scale, int milliseconds) {
			this.scale = scale;
			length = AudioSettings.outputSampleRate * milliseconds / 1000;
		}
	}
}