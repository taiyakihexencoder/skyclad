using Unity.Properties;
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

		/// <summary>
		/// ブリンク
		/// </summary>
		/// <param name="ve"></param>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <param name="durationMillis"></param>
		/// <param name="easing"></param>
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

		/// <summary>
		/// ブリンク
		/// </summary>
		/// <param name="ve"></param>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <param name="durationMillis"></param>
		public static void StartBlink(this VisualElement ve, Color from, Color to, int durationMillis) {
			StartBlink(ve, from, to, durationMillis, Easing.InOutSine);
		}

		/// <summary>
		/// Labelの色を変更する
		/// </summary>
		/// <param name="ve"></param>
		/// <param name="to"></param>
		/// <param name="durationMillis"></param>
		public static void ChangeLabelColor(this Label ve, Color to, int durationMillis) {
			ve.experimental.animation
				.Start(ve.style.color.value, to, durationMillis, (ve, color) => ve.style.color = to );
		}

		/// <summary>
		/// 背景色を変更する
		/// </summary>
		/// <param name="ve"></param>
		/// <param name="to"></param>
		/// <param name="durationMillis"></param>
		public static void ChangeBackgroundColor(this VisualElement ve, Color to, int durationMillis) {
			ve.experimental.animation
				.Start(ve.style.backgroundColor.value, to, durationMillis, (ve, color) => ve.style.backgroundColor = to );
		}

		/// <summary>
		/// アニメーション停止
		/// </summary>
		/// <param name="ve"></param>
		public static void EndAnimation(this VisualElement ve) {
			if (ve.userData is IAnimationStopper stopper) {
				stopper.Stop();
				ve.userData = null;
			}
		}

		/// <summary>
		/// フェードイン
		/// </summary>
		/// <param name="ve"></param>
		/// <param name="durationMillis"></param>
		/// <param name="easing"></param>
		public static void FadeIn(this VisualElement ve, int durationMillis, System.Func<float, float> easing) {
			ve.style.opacity = 0.0f;
			ve.style.visibility = Visibility.Visible;
			ve.experimental.animation
				.Start(0.0f, 1.0f, durationMillis, (ve, opacity) => ve.style.opacity = opacity)
				.Ease(easing);
		}

		/// <summary>
		/// フェードイン
		/// </summary>
		/// <param name="ve"></param>
		/// <param name="durationMillis"></param>
		public static void FadeIn(this VisualElement ve, int durationMillis) {
			FadeIn(ve, durationMillis, Easing.InQuad);
		}

		/// <summary>
		/// フェードアウト
		/// </summary>
		/// <param name="ve"></param>
		/// <param name="durationMillis"></param>
		/// <param name="easing"></param>
		public static void FadeOut(this VisualElement ve, int durationMillis, System.Func<float, float> easing) {
			ValueAnimation<float> animation = ve.experimental.animation
				.Start(1.0f, 0.0f, durationMillis, (ve, opacity) => ve.style.opacity = opacity)
				.Ease(easing);
			animation.onAnimationCompleted += () => {
				ve.style.visibility = Visibility.Hidden;
				animation.onAnimationCompleted = null;
			};
		}

		/// <summary>
		/// フェードアウト
		/// </summary>
		/// <param name="ve"></param>
		/// <param name="durationMillis"></param>
		public static void FadeOut(this VisualElement ve, int durationMillis) {
			FadeOut(ve, durationMillis, Easing.OutQuad);
		}

		/// <summary>
		/// スライドイン
		/// </summary>
		/// <param name="ve"></param>
		/// <param name="offset"></param>
		/// <param name="durationMillis"></param>
		/// <param name="easing"></param>
		public static void SlideIn(this VisualElement ve, Vector2 offset, int durationMillis, System.Func<float, float> easing) {
			ve.style.visibility = Visibility.Visible;
			ve.style.translate = offset;
			ve.experimental.animation
				.Position(Vector3.zero, durationMillis)
				.Ease(easing);
		}

		/// <summary>
		/// スライドイン
		/// </summary>
		/// <param name="ve"></param>
		/// <param name="offset"></param>
		/// <param name="durationMillis"></param>
		public static void SlideIn(this VisualElement ve, Vector2 offset, int durationMillis) {
			SlideIn(ve, offset, durationMillis, Easing.OutQuad);
		}

		/// <summary>
		/// スライドアウト
		/// </summary>
		/// <param name="ve"></param>
		/// <param name="offset"></param>
		/// <param name="durationMillis"></param>
		/// <param name="easing"></param>
		public static void SlideOut(this VisualElement ve, Vector2 offset, int durationMillis, System.Func<float, float> easing) {
			ValueAnimation<Vector3> animation = ve.experimental.animation
				.Position(offset, durationMillis)
				.Ease(easing);
			animation.onAnimationCompleted += () => {
				ve.style.visibility = Visibility.Hidden;
				animation.onAnimationCompleted = null;
			};
		}

		/// <summary>
		/// スライドアウト
		/// </summary>
		/// <param name="ve"></param>
		/// <param name="offset"></param>
		/// <param name="durationMillis"></param>
		public static void SlideOut(this VisualElement ve, Vector2 offset, int durationMillis) {
			SlideOut(ve, offset, durationMillis, Easing.OutQuad);
		}

		/// <summary>
		/// ズームイン
		/// </summary>
		/// <param name="ve"></param>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <param name="durationMillis"></param>
		/// <param name="easing"></param>
		public static void ZoomIn(this VisualElement ve, float from, float to, int durationMillis, System.Func<float, float> easing) {
			ve.style.scale = new StyleScale(new Scale(new Vector2(from, from)));
			ve.style.visibility = Visibility.Visible;
			ve.experimental.animation
				.Scale(to, durationMillis)
				.Ease(easing);
		}

		/// <summary>
		/// ズームイン
		/// </summary>
		/// <param name="ve"></param>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <param name="durationMillis"></param>
		public static void ZoomIn(this VisualElement ve, float from, float to, int durationMillis) {
			ZoomIn(ve, from, to, durationMillis, Easing.OutBack);
		}

		/// <summary>
		/// ズームアウト
		/// </summary>
		/// <param name="ve"></param>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <param name="durationMillis"></param>
		/// <param name="easing"></param>
		public static void ZoomOut(this VisualElement ve, float from, float to, int durationMillis, System.Func<float, float> easing) {
			ve.style.scale = new StyleScale(new Scale(new Vector2(from, from)));
			ValueAnimation<float> animation = ve.experimental.animation
				.Scale(to, durationMillis)
				.Ease(easing);
			animation.onAnimationCompleted += () => {
				ve.style.visibility = Visibility.Hidden;
				animation.onAnimationCompleted = null;
			};
		}

		/// <summary>
		/// ズームアウト
		/// </summary>
		/// <param name="ve"></param>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <param name="durationMillis"></param>
		public static void ZoomOut(this VisualElement ve, float from, float to, int durationMillis) {
			ZoomOut(ve, from, to, durationMillis, Easing.InBack);
		}

		/// <summary>
		/// SliderのパラメーターBinding
		/// </summary>
		/// <param name="slider"></param>
		/// <param name="binding"></param>
		public static void Bind(this Slider slider, in SliderBinding binding) {
			slider.SetBinding(
				"value", 
				new DataBinding {
					dataSourcePath = new PropertyPath(nameof(SliderBinding.value)),
					bindingMode = BindingMode.ToTarget,
				}
			);
			slider.SetBinding(
				"lowValue",
				new DataBinding {
					dataSourcePath = new PropertyPath(nameof(SliderBinding.lowValue)),
					bindingMode = BindingMode.ToTarget,
				}
			);
			slider.SetBinding(
				"highValue",
				new DataBinding {
					dataSourcePath = new PropertyPath(nameof(SliderBinding.highValue)),
					bindingMode = BindingMode.ToTarget,
				}
			);
			slider.dataSource = binding;
		}
	}
}