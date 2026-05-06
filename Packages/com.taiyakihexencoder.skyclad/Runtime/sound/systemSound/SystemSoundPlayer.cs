using UnityEngine;

namespace skyclad {
	/// <summary>
	/// サウンドをシンセサイザーで動的に生成して再生する。
	/// </summary>
	public sealed class SystemSoundPlayer : MonoBehaviour {
		private static SystemSoundPlayer _instance = null;

		[SerializeField]
		private int _playerCount = 4;

		[SerializeField]
		private AudioSource _audioSourcePrefab = null;

		private SingleSystemSoundPlayer[] _soundPlayers = new SingleSystemSoundPlayer[0];
		private AudioSource[] _audioSources = new AudioSource[0];

		private void Awake() {
			_instance = this;

			if (_audioSourcePrefab != null && _playerCount > 0) {
				_soundPlayers = new SingleSystemSoundPlayer[_playerCount];
				_audioSources = new AudioSource[_playerCount];
				for (int i = 0; i < _playerCount; ++i) {
					_soundPlayers[i] = ScriptableObject.CreateInstance<SingleSystemSoundPlayer>();
					_audioSources[i] = Instantiate(_audioSourcePrefab, _audioSourcePrefab.transform.parent);

					_audioSources[i].generator = _soundPlayers[i];
					_soundPlayers[i].AssignAudioSource(_audioSources[i]);
					_soundPlayers[i].AllocateResources();
				}
				_audioSourcePrefab.gameObject.SetActive(false);
			} else {
				Debug.LogWarning("Incorrect system sound player settings.");
			}
		}

		private bool TryPlaySound(in PlaySoundSignal signal) {
			for(int i = 0; i < _soundPlayers.Length; ++i) {
				if (_soundPlayers[i].TryAcceptRequest(signal)) {
					return true;
				}
			}

			return false;
		}

		private void Update() {
			for(int i = 0; i < _soundPlayers.Length; ++i) {
				_soundPlayers[i].UpdateSound();
			}
		}

		private void OnDestroy() {
			for(int i = 0; i < _soundPlayers.Length; ++i) {
				_soundPlayers[i].DisposeResources();
			}
		}

		private void OnApplicationQuit() {
			for(int i = 0; i < _soundPlayers.Length; ++i) {
				_soundPlayers[i].DisposeResources();
			}
		}

		/// <summary>
		/// サウンド再生
		/// </summary>
		/// <param name="signal"></param>
		public static void Play(in PlaySoundSignal signal) {
			if (_instance == null) {
				Debug.LogWarning("Instance not initialized.");
			} else if (!_instance.TryPlaySound(signal)) {
				Debug.LogWarning("Sound player is busy.");
			}
		}

		[ContextMenu("Pattern01")]
		private void TestPlaySoundPattern01() {
			Play(SystemSound.Pattern01);
		}

		[ContextMenu("Pattern02")]
		private void TestPlaySoundPattern02() {
			Play(SystemSound.Pattern02);
		}

		[ContextMenu("Pattern03")]
		private void TestPlaySoundPattern03() {
			Play(SystemSound.Pattern03);
		}

		[ContextMenu("Noise High")]
		private void TestPlaySoundNoiseHigh() {
			Play(SystemSound.NoiseHigh);
		}

		[ContextMenu("Noise Middle")]
		private void TestPlaySoundNoiseMiddle() {
			Play(SystemSound.NoiseMiddle);
		}

		[ContextMenu("Noise Low")]
		private void TestPlaySoundNoiseLow() {
			Play(SystemSound.NoiseLow);
		}

		[ContextMenu("Attack High")]
		private void TestPlaySoundAttackHigh() {
			Play(SystemSound.AttackHigh);
		}

		[ContextMenu("Attack Middle")]
		private void TestPlaySoundAttackMiddle() {
			Play(SystemSound.AttackMiddle);
		}

		[ContextMenu("Attack Low")]
		private void TestPlaySoundAttackLow() {
			TryPlaySound(SystemSound.AttackLow);
		}

	}
}