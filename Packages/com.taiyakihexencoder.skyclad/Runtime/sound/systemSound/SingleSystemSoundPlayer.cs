using Unity.Collections;
using Unity.IntegerTime;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Audio;

namespace skyclad {
	/// <summary>
	/// 実際に再生している部分。
	/// </summary>
	internal sealed class SingleSystemSoundPlayer : ScriptableObject, IAudioGenerator {
		bool GeneratorInstance.ICapabilities.isFinite => true;
		bool GeneratorInstance.ICapabilities.isRealtime => false;

		private DiscreteTime _length;
		DiscreteTime? GeneratorInstance.ICapabilities.length => _length;

		private AudioSource _source;

		private PlaySoundSignal _signal = new PlaySoundSignal();

		/// <summary>
		/// リクエスト受け取りフラグ
		/// 外部からリクエストを受けるが、Controlへの追加はそのタイミングでできないので、
		/// フラグで管理する
		/// </summary>
		private bool _cue = false;

		/// <summary>
		/// サウンド終了フラグ
		/// IRealtime内でtrueに設定する。
		/// IRealtimeからAudioSourceに触れる方法がなく、
		/// 設定したlengthに応じて自動で止まるわけでもないので、
		/// 共有メモリでフラグ管理するしかない。
		/// </summary>
		private NativeArray<bool> _endSound;

		internal void AssignAudioSource(AudioSource source) {
			_source = source;
		}

		internal void AllocateResources() {
			if (!_endSound.IsCreated) {
				_endSound = new NativeArray<bool>(1, Allocator.Persistent);
				_endSound[0] = true;
			}
		}

		internal void DisposeResources() {
			if (_endSound.IsCreated) {
				_endSound.Dispose();
			}
		}

		internal void UpdateSound() {
			ProcessorInstance instance = _source.generatorInstance;
			if (IsValid(instance)) {
				// メッセージを送る
				if (_cue) {
					ControlContext.builtIn.SendMessage(instance, ref _signal);
					_cue = false;
				}
				UpdateIsPlaying();
			}
		}

		/// <summary>
		/// サウンドの更新ができるか
		///   * フラグリソースが生成されている
		///   * AudioSourceが設定されている
		///   * 再生リクエストがある
		///   * ProcessorInstanceが存在する
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		private bool IsValid(in ProcessorInstance instance) {
			return
				_endSound.IsCreated &&
				_source != null && 
				!_signal.isEmpty &&
				ControlContext.builtIn.Exists(instance);
		}

		/// <summary>
		/// 再生が終了していたら、リクエストを破棄して更新を止める
		/// </summary>
		private void UpdateIsPlaying() {
			if (_endSound[0]) {
				_source.Stop();
				_signal = new PlaySoundSignal();
			}
		}

		/// <summary>
		/// 待機中の場合のみサウンドを再生する => true
		/// </summary>
		/// <param name="signal"></param>
		/// <returns></returns>
		public bool TryAcceptRequest(in PlaySoundSignal signal) {
			if (_source.isPlaying) {
				return false;
			} else {
				// 受付済にする
				_signal = signal;
			
				// 再生時間を計算する
				int samplingRate = AudioSettings.outputSampleRate;
				int sum = 0;
				foreach(PlaySoundNote note in _signal.notes) {
					sum += note.length;
				}
				_length = new DiscreteTime(sum / AudioSettings.outputSampleRate);
				// AudioSourceを再起動
				_source.Play();

				// メッセージ送信フラグ(送信にProcessorInstanceが必要)
				_cue = true;
				// 再生終了フラグ(終了検知に必要)
				_endSound[0] = false;

				return true;
			}
		}

		GeneratorInstance IAudioGenerator.CreateInstance(
			ControlContext context, 
			AudioFormat? nestedFormat, 
			ProcessorInstance.CreationParameters creationParameters
		) {
			return context.AllocateGenerator(
				new RealtimeImpl(AudioSettings.outputSampleRate, _length, _endSound), 
				new ControlImpl()
			);
		}
	}

	/// <summary>
	/// 実際の音声加工
	/// </summary>
	internal struct RealtimeImpl : GeneratorInstance.IRealtime {
		bool GeneratorInstance.ICapabilities.isFinite => true;
		bool GeneratorInstance.ICapabilities.isRealtime => false;

		private PlaySoundSignal _signal;
		private float _currentFrequency;
		private int _currentNoteIndex;
		private int _currentNoteOffset;
		private int _currentNoteLength;

		private int _samplingRate;

		private DiscreteTime _length;
		DiscreteTime? GeneratorInstance.ICapabilities.length => _length;
		private NativeArray<bool> _completed;

		private NativeArray<float> _noiseArray;
		private int _noiseIndex;

		public RealtimeImpl(int samplingRate, DiscreteTime length, NativeArray<bool> completed) {
			_samplingRate = samplingRate;
			_length = length;
			_signal = default;
			_currentFrequency = 0f;
			_currentNoteIndex = 0;
			_currentNoteOffset = 0;
			_currentNoteLength = 0;
			_completed = completed;

			_noiseArray = new NativeArray<float>();
			_noiseIndex = 0;
		}

		GeneratorInstance.Result GeneratorInstance.IRealtime.Process(
			in RealtimeContext context, 
			ProcessorInstance.Pipe pipe, 
			ChannelBuffer buffer, 
			GeneratorInstance.Arguments args
		) {
			float fedeDelta = 1.0f / buffer.frameCount;

			int rest = _currentNoteLength - _currentNoteOffset;
			int frame = 0, startFrame;
			// 1フレームあたりの位相の変化量(rad)
			double phasePerFrame = _currentFrequency * math.PI2 / _samplingRate;
			// 現在の位相
			double phase = (_currentNoteOffset * phasePerFrame) % math.PI2;
			float v;

			buffer.Clear();
			while (rest <= buffer.frameCount - frame) {
				float fedeout = (float)rest / buffer.frameCount;
				startFrame = frame;
				for (; frame < rest + startFrame; ++frame) {
					// 現在の位相から設定値を取得し、位相を更新
					v = CalculatPhaseValue(_signal.shape, phase, frame - startFrame + _currentNoteOffset);
					phase += phasePerFrame;
					if (phase >= math.PI2) {
						phase -= math.PI2;
					}

					// フレームに値を設定
					for (int ch = 0; ch < buffer.channelCount; ++ch) {
						buffer[ch, frame] = v * fedeout;
					}
					fedeout -= fedeDelta;
				}

				// 最後ならそのまま終了。
				if (_currentNoteIndex + 1 == _signal.notes.Length) {
					_currentNoteOffset = _currentNoteLength;
					_completed[0] = true;
					if (_noiseArray.IsCreated) {
						_noiseArray.Dispose();
					}
					return rest;
				} else {
					// 次のノートへ
					_currentNoteIndex++;
					_currentFrequency = _signal.notes[_currentNoteIndex].scale.Frequency();
					_currentNoteOffset = 0;
					_currentNoteLength = _signal.notes[_currentNoteIndex].length;
					rest = _currentNoteLength;

					phasePerFrame = _currentFrequency * math.PI2 / _samplingRate;

					AllocateNoiseArray(_signal.notes[_currentNoteIndex].scale);

				}
			}

			float fedeIn = math.min(1.0f, (float)_currentNoteOffset / buffer.frameCount);
			startFrame = frame;

			// 残りのフレームを現在のノートで埋める
			if (fedeIn == 1.0f) {
				for (; frame < buffer.frameCount; ++frame) {
					// 現在の位相から設定値を取得し、位相を更新
					v = CalculatPhaseValue(_signal.shape, phase, frame - startFrame + _currentNoteOffset);
					phase += phasePerFrame;
					if (phase >= math.PI2) {
						phase -= math.PI2;
					}

					// フレームに値を設定
					for (int ch = 0; ch < buffer.channelCount; ++ch) {
						buffer[ch, frame] = v;
					}
				}
			} else {
				for (; frame < buffer.frameCount; ++frame) {
					v = CalculatPhaseValue(_signal.shape, phase, frame - startFrame + _currentNoteOffset);
					phase += phasePerFrame;
					if (phase >= math.PI2) {
						phase -= math.PI2;
					}

					for (int ch = 0; ch < buffer.channelCount; ++ch) {
						buffer[ch, frame] = v * fedeIn;
					}
					fedeIn += fedeDelta;
					if (fedeIn > 1.0f) {
						fedeDelta = 0.0f;
						fedeIn = 1.0f;
					}
				}
			}
			// フレーム数をオフセットに追加
			_currentNoteOffset += buffer.frameCount - startFrame;

			return buffer.frameCount;
		}

		private float CalculatPhaseValue(SoundWaveShape shape, double phase, int frame) {
			switch(shape) {
				case SoundWaveShape.Sin: {
					return 2.0f * math.sin((float)phase);
				}
				case SoundWaveShape.Square: {
					return phase <= math.PI ? 0.5f : -0.5f;
				}
				case SoundWaveShape.Attack: {
					_noiseArray[_noiseIndex] = Unity.Mathematics.Random.CreateFromIndex((uint)frame).NextFloat(-1f, 1f);
					float sum = 0.0f;
					for (int i = 0; i < _noiseArray.Length; ++i) {
						sum += _noiseArray[i];
					}
					_noiseIndex++;
					if (_noiseIndex == _noiseArray.Length) { _noiseIndex = 0; }
					float alpha = frame * 8e-3f;
					return 60.0f * sum / _noiseArray.Length * math.min(math.exp(-alpha), 1.0f) * alpha;
				}
				case SoundWaveShape.Noise: {
					_noiseArray[_noiseIndex] = Unity.Mathematics.Random.CreateFromIndex((uint)frame).NextFloat(-1f, 1f);
					float sum = 0.0f;
					for (int i = 0; i < _noiseArray.Length; ++i) {
						sum += _noiseArray[i];
					}
					_noiseIndex++;
					if (_noiseIndex == _noiseArray.Length) { _noiseIndex = 0; }
					return sum / _noiseArray.Length;
				}
				default: {
					return 0.0f;
				}
			}
		}

		void ProcessorInstance.IRealtime.Update(
			ProcessorInstance.UpdatedDataContext context, 
			ProcessorInstance.Pipe pipe
		) {
			foreach (ProcessorInstance.AvailableData.Element element in pipe.GetAvailableData(context)) {
				if (element.TryGetData(out PlaySoundSignal signal)) {
					_signal = signal;
					_currentNoteIndex = 0;
					_currentNoteOffset = 0;
					if (!_signal.notes.IsEmpty) {
						_currentNoteLength = _signal.notes[_currentNoteIndex].length;
						_currentFrequency = _signal.notes[_currentNoteIndex].scale.Frequency();
					}

					if (!_signal.notes.IsEmpty) {
						AllocateNoiseArray(_signal.notes[0].scale);
					}
				}
			}
		}

		/// <summary>
		/// ノイズは前のパラメーターで平滑化するほど低音になる。
		/// </summary>
		/// <param name="freq"></param>
		private void AllocateNoiseArray(MusicalScale scale) {
			if (_signal.shape == SoundWaveShape.Attack || _signal.shape == SoundWaveShape.Noise) {
				if (_noiseArray.IsCreated) {
					_noiseArray.Dispose();
				}

				int freq = (int)scale;
				int digits = 0;
				for(; freq > 0; freq /= 10) {
					digits++;
				}
				_noiseArray = new NativeArray<float>((7 - digits)*5+1, Allocator.Persistent);
				_noiseIndex = 0;
			}
		}
	}

	/// <summary>
	/// メッセージなど操作の制御
	/// </summary>
	internal struct ControlImpl : GeneratorInstance.IControl<RealtimeImpl> {
		void GeneratorInstance.IControl<RealtimeImpl>.Configure(
			ControlContext context, 
			ref RealtimeImpl realtime, 
			in AudioFormat format, 
			out GeneratorInstance.Setup setup, 
			ref GeneratorInstance.Properties properties
		) {
			setup = new GeneratorInstance.Setup(
				AudioSpeakerMode.Mono,
				format.sampleRate
			);
		}

		void ProcessorInstance.IControl<RealtimeImpl>.Dispose(ControlContext context, ref RealtimeImpl realtime) { }

		ProcessorInstance.Response ProcessorInstance.IControl<RealtimeImpl>.OnMessage(
			ControlContext context, 
			ProcessorInstance.Pipe pipe, 
			ProcessorInstance.Message message
		) {
			if (message.Is<PlaySoundSignal>()) {
				pipe.SendData(context, message.Get<PlaySoundSignal>());
				return ProcessorInstance.Response.Handled;
			} else {
				return ProcessorInstance.Response.Unhandled;
			}
		}

		void ProcessorInstance.IControl<RealtimeImpl>.Update(
			ControlContext context, 
			ProcessorInstance.Pipe pipe
		) { }
	}
}