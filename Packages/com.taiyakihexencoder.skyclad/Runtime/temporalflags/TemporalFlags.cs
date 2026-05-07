using Unity.Entities;

namespace skyclad {
	/// <summary>
	/// 256個の一時的フラグを管理。
	/// フラグは共用するため、別の場面で使う場合はFlagResetRequestを行う。
	/// シングルトンで取得する。
	/// ulongフラグを直接操作してもよいし、GetFlag、SetFlagを使ってもOK。
	/// </summary>
	public struct TemporalFlags : IComponentData {
		public ulong flag0to63;
		public ulong flag64to127;
		public ulong flag128to191;
		public ulong flag192to255;

		/// <summary>
		/// 0未満、256以上の場合はfalse
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public bool GetFlag(int index) {
			if (index < 0) {
				return false;
			} else if (index < 64) {
				return (flag0to63 & (1UL << index)) != 0;
			} else if (index < 128) {
				return (flag64to127 & (1UL << (index ^ 64))) != 0;
			} else if (index < 192) {
				return (flag128to191 & (1UL << (index ^ 128))) != 0;
			} else {
				return (flag192to255 & (1UL << (index ^ 192))) != 0;
			}
		}

		/// <summary>
		/// 0未満、256以上の場合はno effect
		/// </summary>
		/// <param name="index"></param>
		/// <param name="flag"></param>
		public void SetFlag(int index, bool flag) {
			if (index < 0) {
				return;
			} if (index < 64) {
				flag0to63 = flag ? flag0to63 | (1UL << index) : flag0to63 & ~(1UL << index);
			} else if (index < 128) {
				flag64to127 = flag ? flag64to127 | (1UL << (index ^ 64)) : flag64to127 & ~(1UL << (index ^ 64));
			} else if (index < 192) {
				flag128to191 = flag ? flag128to191 | (1UL << (index ^ 128)) : flag128to191 & ~(1UL << (index ^ 128));
			} else {
				flag192to255 = flag ? flag192to255 | (1UL << (index ^ 192)) : flag192to255 & ~(1UL << (index ^ 192));
			}
		}
	}

	/// <summary>
	/// 全フラグのリセット。
	/// 変数にセットしなければ0になるのですべてfalseになる。
	/// 特定のフラグを初期状態でtrueにしたければ生成時にフラグを変更する。
	/// </summary>
	public struct TemporalFlagsResetRequest : IComponentData {
		public ulong flag0to63;
		public ulong flag64to127;
		public ulong flag128to191;
		public ulong flag192to255;
	}
}
