using UnityEngine.AddressableAssets;

namespace skyclad.field {
	[System.Serializable]
	public sealed class FieldAssetReference : AssetReferenceT<FieldAsset> {
		public FieldAssetReference(string guid) : base(guid) { }
	}
}