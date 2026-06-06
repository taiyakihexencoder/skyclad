namespace skyclad {
	public struct AppVersion : System.IComparable<AppVersion>{
		public byte major;
		public byte minor;
		public byte revision;

		public AppVersion(byte major, byte minor, byte revision) {
			this.major = major;
			this.minor = minor;
			this.revision = revision;
		}

		public static bool TryParse(string text, out AppVersion version) {
			string[] split = text.Split('.');
			if (split.Length >= 3 &&
				byte.TryParse(split[0], out byte major) &&
				byte.TryParse(split[1], out byte minor) &&
				byte.TryParse(split[2], out byte revision) ) {
				version = new AppVersion {
					major = major,
					minor = minor,
					revision = revision,
				};
				return true;
			} else {
				version = default;
				return false;
			}
		}

		public static AppVersion Parse(string text) {
			string[] split = text.Split('.');
			return new AppVersion {
				major = byte.Parse(split[0]),
				minor = byte.Parse(split[1]),
				revision = byte.Parse(split[2]),
			};
		}

		public readonly override string ToString() {
			return $"{major}.{minor}.{revision}";
		}

		public readonly override bool Equals(object obj) {
			if (obj is AppVersion v) {
				return major == v.major && minor == v.minor && revision == v.revision;
			} else {
				return false;
			}
		}

		public readonly override int GetHashCode() {
			return major ^ minor ^ revision;
		}

		readonly int System.IComparable<AppVersion>.CompareTo(AppVersion other){
			int comp;
			comp = major.CompareTo(other.major);
			if (comp == 0) {
				comp = minor.CompareTo(other.minor);
				if (comp == 0) {
					comp = revision.CompareTo(other.revision);
				}
			}
			return comp;
		}
	}
}