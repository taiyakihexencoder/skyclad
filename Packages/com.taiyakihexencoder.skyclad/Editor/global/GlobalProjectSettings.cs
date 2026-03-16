using System.IO;
using UnityEngine;

namespace skyclad.editor {
	[System.Serializable]
	internal struct GlobalProjectSettings {
		internal static uint[] LAYER_DEFAULT = new uint[] {
			Layer.Terrain,
			Layer.PhysicsObject,
		};
		internal static uint[] COLLISION_DEFAULT = new uint[] {
			Layer.CollidesWith.Terrain,
			Layer.CollidesWith.PhysicsObject,
		};

		internal static string AutoGeneratePath => $"Skyclad{Path.DirectorySeparatorChar}auto-generated";
		internal static string AutoGenerateScriptPath => $"{AutoGeneratePath}{Path.DirectorySeparatorChar}Scripts";

		internal static string ApplicationExternalDataPath => $".skyclad{Path.DirectorySeparatorChar}.app";

		[SerializeField]
		private string[] _colliderLayers;
		/// <summary>
		/// レイヤー名
		/// </summary>
		internal string[] ColliderLayers => _colliderLayers;

		[SerializeField]
		private bool[] _collides;
		/// <summary>
		/// レイヤー間の衝突情報
		/// </summary>
		internal bool[] Collides => _collides;
	}
}