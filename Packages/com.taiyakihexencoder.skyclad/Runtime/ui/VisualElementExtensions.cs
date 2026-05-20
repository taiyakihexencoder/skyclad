using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace skyclad {
	public static class VisualElementExtensions {
		/// <summary>
		/// ValueAnimationはIValueAnimationで停止処理を持たないため、
		/// 停止機能をジェネリクス間で共通化するために停止用Wrapperを用意する。
		/// </summary>
		private interface IAnimationStopper {
			void Stop();
		}

		private class AnimationStopper<T> : IAnimationStopper {
			private ValueAnimation<T> _animation;
			public AnimationStopper(ValueAnimation<T> animation) {
				_animation = animation;
			}

			void IAnimationStopper.Stop() {
				// ループ処理部分をクリアしないとStop後に呼ばれてしまう
				_animation.onAnimationCompleted = null;
				_animation.Stop();
			}
		}

		public static void StartBlink(
			this VisualElement ve, 
			Color from, 
			Color to, 
			int durationMillis,
			System.Func<float, float> easing
		) {
			ValueAnimation<Color> animation = ve.experimental.animation
				.Start(from, to, durationMillis, (ve, color) => { ve.style.backgroundColor = color; })
				.Ease(easing);

			// ループ実行がライブラリに用意されていないため、停止したら方向を変えて再実行する
			// ただし停止時にnullにしておかないとこの処理が呼ばれてしまい、停止できなくなる
			animation.onAnimationCompleted += () => {
				StartBlink(ve, to, from, durationMillis, easing);
			};

			ve.RegisterCallback<DetachFromPanelEvent>(
				(evt) => {
					if (ve.userData is IAnimationStopper stopper) {
						stopper.Stop();
						ve.userData = null;
					}
				}
			);

			// 戻り値のValueAnimationからしか停止できないため、userDataに持っておく
			// その際にコールバックを止める必要があるので、停止Wrapperで管理する
			ve.userData = new AnimationStopper<Color>(animation);
		}

		public static void StartBlink(this VisualElement ve, Color from, Color to, int durationMillis) {
			StartBlink(ve, from, to, durationMillis, Easing.InOutSine);
		}

		public static void EndAnimation(this VisualElement ve) {
			if (ve.userData is IAnimationStopper stopper) {
				stopper.Stop();
				ve.userData = null;
			}
		}
	}
}