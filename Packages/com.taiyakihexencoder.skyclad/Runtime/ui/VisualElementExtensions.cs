using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace skyclad {
	public static class VisualElementExtensions {
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

			animation.onAnimationCompleted += () => {
				StartBlink(ve, to, from, durationMillis, easing);
			};

			ve.RegisterCallback<DetachFromPanelEvent>(
				(evt) => {
					if (ve.userData is IValueAnimation animation) {
						animation.Stop();
						ve.userData = null;
					}
				}
			);

			// 戻り値のValueAnimationからしか停止できないため、userDataに持っておく
			ve.userData = animation;
		}

		public static void StartBlink(this VisualElement ve, Color from, Color to, int durationMillis) {
			StartBlink(ve, from, to, durationMillis, Easing.InOutSine);
		}

		public static void EndAnimation(this VisualElement ve) {
			if (ve.userData is IValueAnimation animation) {
				animation.Stop();
				ve.userData = null;
			}
		}
	}
}