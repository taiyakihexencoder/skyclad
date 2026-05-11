using System.Threading.Tasks;

namespace skyclad {
	internal interface IResourceHolder {
		int ReferenceCount{ get; }

		void IncrementReferenceCount();
		bool DecrementReferenceCount();

		Task<object> Resource{ get; }
		void Dispose();
	}
}