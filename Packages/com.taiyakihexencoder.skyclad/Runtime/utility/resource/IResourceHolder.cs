using System.Threading.Tasks;

namespace skyclad {
	internal interface IResourceHolder {
		Task<object> Resource{ get; }
		void Dispose();
	}
}