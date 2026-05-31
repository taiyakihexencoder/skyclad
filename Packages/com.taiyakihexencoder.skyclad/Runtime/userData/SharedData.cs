namespace skyclad.userData {
	using internalProc;
	public static class SharedData {
		public static void Set(string key, bool value) { SaveUtilityInternal.Prefs.Set(key, value); }
		public static void Set(string key, bool[] value) { SaveUtilityInternal.Prefs.Set(key, value); }
		public static void Set(string key, int value) { SaveUtilityInternal.Prefs.Set(key, value); }
		public static void Set(string key, int[] value) { SaveUtilityInternal.Prefs.Set(key, value); }
		public static void Set(string key, long value) { SaveUtilityInternal.Prefs.Set(key, value); }
		public static void Set(string key, long[] value) { SaveUtilityInternal.Prefs.Set(key, value); }
		public static void Set(string key, float value) { SaveUtilityInternal.Prefs.Set(key, value); }
		public static void Set(string key, float[] value) { SaveUtilityInternal.Prefs.Set(key, value); }
		public static void Set(string key, string value) { SaveUtilityInternal.Prefs.Set(key, value); }
		public static void Set<T>(string key, T value) { SaveUtilityInternal.Prefs.Set(key, value); }

		public static bool TryGet(string key, out bool value) { return SaveUtilityInternal.Prefs.TryGetFlag(key, out value); }
		public static bool TryGet(string key, out int value) { return SaveUtilityInternal.Prefs.TryGetInt(key, out value); }
		public static bool TryGet(string key, out long value) { return SaveUtilityInternal.Prefs.TryGetLong(key, out value); }
		public static bool TryGet(string key, out float value) { return SaveUtilityInternal.Prefs.TryGetFloat(key, out value); }
		public static bool TryGet(string key, out string value) { return SaveUtilityInternal.Prefs.TryGetString(key, out value); }
		public static bool TryGet<T>(string key, out T value) { return SaveUtilityInternal.Prefs.TryGet<T>(key, out value); }

		public static bool GetFlag(string key, bool defaultValue = false) { return SaveUtilityInternal.Prefs.GetFlag(key, defaultValue); }
		public static bool[] GetFlagArray(string key, bool defaultValue = false) { return SaveUtilityInternal.Prefs.GetFlagArray(key, defaultValue); }
		public static int GetInt(string key, int defaultValue = 0) { return SaveUtilityInternal.Prefs.GetInt(key, defaultValue); }
		public static int[] GetIntArray(string key, int defaultValue = 0) { return SaveUtilityInternal.Prefs.GetIntArray(key, defaultValue); }
		public static long GetLong(string key, long defaultValue = 0L) { return SaveUtilityInternal.Prefs.GetLong(key, defaultValue); }
		public static long[] GetLongArray(string key, long defaultValue = 0L) { return SaveUtilityInternal.Prefs.GetLongArray(key, defaultValue); }
		public static float GetFloat(string key, float defaultValue = 0f) { return SaveUtilityInternal.Prefs.GetFloat(key, defaultValue); }
		public static float[] GetFloatArray(string key, float defaultValue = 0f) { return SaveUtilityInternal.Prefs.GetFloatArray(key, defaultValue); }
		public static string GetString(string key, string defaultValue = null) { return SaveUtilityInternal.Prefs.GetString(key, defaultValue); }
		public static T Get<T>(string key, T defaultValue = default) { return SaveUtilityInternal.Prefs.Get<T>(key, defaultValue); }

		public static void Save() { SaveUtilityInternal.Prefs.Save(); }

		public static void DeleteKey(string key) { SaveUtilityInternal.Prefs.DeleteKey(key); }

		public static void DeleteAll() { SaveUtilityInternal.Prefs.DeleteAll(); }
	}
}
