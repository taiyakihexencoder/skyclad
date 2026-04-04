using UnityEngine;

namespace skyclad.editor {
	[System.Serializable]
	public class UserDataProjectSettings {
		public abstract class UserDataParameter<T> {
			public string guid;
			public string name;
			public T defaultValue;
		}

		[System.Serializable]
		public class IntParameter : UserDataParameter<int> { }

		[System.Serializable]
		public class BoolParameter : UserDataParameter<bool> { }

		[System.Serializable]
		public class FloatParameter : UserDataParameter<float> { }

		[System.Serializable]
		public class Vector2Parameter : UserDataParameter<Vector2> { }

		[System.Serializable]
		public class Vector3Parameter : UserDataParameter<Vector3> { }

		[SerializeField]
		private IntParameter[] _intParameters;
		public IntParameter[] IntParameters => _intParameters;

		[SerializeField]
		private BoolParameter[] _boolParameters;
		public BoolParameter[] BoolParameters => _boolParameters;

		[SerializeField]
		private FloatParameter[] _floatParameters;
		public FloatParameter[] FloatParameters => _floatParameters;

		[SerializeField]
		private Vector2Parameter[] _vector2Parameters;
		public Vector2Parameter[] Vector2Parameters => _vector2Parameters;

		[SerializeField]
		private Vector3Parameter[] _vector3Parameters;
		public Vector3Parameter[] Vector3Parameters => _vector3Parameters;
	}
}