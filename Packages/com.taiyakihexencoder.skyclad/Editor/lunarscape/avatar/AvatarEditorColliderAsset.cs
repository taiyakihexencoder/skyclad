using UnityEngine;

namespace skyclad.lunarscape.editor {
	internal sealed class AvatarEditorColliderAsset : ScriptableObject {
		[System.Serializable]
		internal struct _Physics {
			[SerializeField]
			private float _radius;
			internal float Radius => _radius;

			[SerializeField]
			private float _height;
			internal float Height => _height;
		}

		[SerializeField]
		private _Physics _physics = new _Physics();
		internal _Physics Physics => _physics;

		[System.Serializable]
		internal struct _Hit {
		}

		[SerializeField]
		private _Hit[] _hit = new _Hit[0];
		internal _Hit[] Hit => _hit;
	}
}