using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace skyclad.internalProc {
	public static class SaveUtilityInternal {
		public const string USER_DATA_PATH = "transaction";
		public const string USER_DATA_FILE_NAME = "userData_{0:D3}.bytes";

		private const string PREFS_LONG_UPPER = ".up";
		private const string PREFS_LONG_LOWER = ".low";
		private const string PREFS_ARRAY_ELEMENT = ".data";
		private const string PREFS_ARRAY_LENGTH = ".length";

		public static class UserData {
			public static List<int> GetSavedSlots() {
				try {
					List<int> slots = new List<int>();

					string absPath = Application.persistentDataPath + Path.DirectorySeparatorChar + USER_DATA_PATH + Path.DirectorySeparatorChar;
					DirectoryInfo directory = new DirectoryInfo(absPath);

					Regex regex = new Regex("userData_(?<idx>\\d{3})\\.bytes");
					foreach(FileInfo file in directory.EnumerateFiles("*.bytes", SearchOption.AllDirectories)) {
						Match match = regex.Match(file.Name);
						if (match.Success) {
							slots.Add(int.Parse(match.Groups["idx"].Value));
						}
					}
					slots.Sort();
					return slots;
				} catch(IOException) {
					// directory not found
					return new List<int>();
				}
			}

			public static async Task Load(
				int slot,
				System.Action<byte[]> action
			) {
				string absPath = AsyncUtilityInternal.Send( () => GetPath(slot) );
				try {
					if (File.Exists(absPath)) {
						byte[] bytes = await File.ReadAllBytesAsync(absPath);
						AsyncUtilityInternal.Send(() => action(bytes));
						AsyncUtilityInternal.Log($"Load:{absPath}");
					}
				} catch (System.Exception e) {
					AsyncUtilityInternal.LogError(e);
				}
			}

			public static async Task Save(
				int slot,
				byte[] source
			) {
				string absPath = AsyncUtilityInternal.Send( () => GetPath(slot) );
				try {
					await File.WriteAllBytesAsync(absPath, source);
					AsyncUtilityInternal.Log($"Save:{absPath}");
				} catch (System.Exception e) {
					AsyncUtilityInternal.LogError(e);
				}
			}

			public static async Task Unload(Entity entity) {
				AsyncUtilityInternal.Send(() => {
					ECSUtilityInternal.ExecuteCommandBufferTemp(
						commandBuffer => {
							commandBuffer.DestroyEntity(entity);
						}
					);
				});

				EntityManager entityManager = AsyncUtilityInternal.Send(() => ECSUtilityInternal.EntityManager);
				bool exists = true;
				while(exists) {
					exists = AsyncUtilityInternal.Send( () => entityManager.Exists(entity) );
					await Task.Yield();
				}
			}

			private static string GetPath(int slot) {
				string basePath = $"{Application.persistentDataPath}{Path.DirectorySeparatorChar}{USER_DATA_PATH}";
				if (!Directory.Exists(basePath)) {
					Directory.CreateDirectory(basePath);
				}
				return $"{basePath}{Path.DirectorySeparatorChar}{string.Format(USER_DATA_FILE_NAME, slot)}";
			}

			public static void CreateLoadRequest(EntityCommandBuffer commandBuffer, int slot) {
				Entity entity = commandBuffer.CreateEntity();
				commandBuffer.SetDebugName(entity, "Load User Data Request");
				commandBuffer.AddComponent(
					entity,
					new InternalRequestLoadUserDataComponent { slot = slot, }
				);
			}

			public static void CreateSaveRequest(EntityCommandBuffer commandBuffer, int slot) {
				Entity entity = commandBuffer.CreateEntity();
				commandBuffer.SetDebugName(entity, "Save User Data Request");
				commandBuffer.AddComponent(
					entity,
					new InternalRequestSaveUserDataComponent { slot = slot, }
				);
			}

			public static void CreateUnloadRequest(EntityCommandBuffer commandBuffer) {
				Entity entity = commandBuffer.CreateEntity();
				commandBuffer.SetDebugName(entity, "Unload User Data Request");
				commandBuffer.AddComponent(
					entity,
					new InternalRequestUnloadUserDataComponent { }
				);
			}
		}

		public static class Prefs {
			public static bool GetFlag(string key, bool defaultValue = false) {
				int value = PlayerPrefs.GetInt(key, -1);
				return value == -1 ? defaultValue : value == 1;
			}

			public static bool[] GetFlagArray(string key, bool defaultValue = false) {
				int length = GetInt($"{key}{PREFS_ARRAY_LENGTH}", -1);
				if (length == -1) {
					return null;
				} else {
					bool[] array = new bool[length];
					for (int i = 0; i < length; ++i) {
						array[i] = GetFlag($"{key}{PREFS_ARRAY_ELEMENT}[{i}]", defaultValue);
					}
					return array;
				}
			}


			public static bool TryGetFlag(string key, out bool value) {
				if (PlayerPrefs.HasKey(key)) {
					value = GetFlag(key);
					return true;
				} else {
					value = false;
					return false;
				}
			}

			public static int GetInt(string key, int defaultValue = 0) {
				return PlayerPrefs.GetInt(key, defaultValue);
			}

			public static int[] GetIntArray(string key, int defaultValue = 0) {
				int length = GetInt($"{key}{PREFS_ARRAY_LENGTH}", -1);
				if (length == -1) {
					return null;
				} else {
					int[] array = new int[length];
					for (int i = 0; i < length; ++i) {
						array[i] = GetInt($"{key}{PREFS_ARRAY_ELEMENT}[{i}]", defaultValue);
					}
					return array;
				}
			}

			public static bool TryGetInt(string key, out int value) {
				if (PlayerPrefs.HasKey(key)) {
					value = GetInt(key);
					return true;
				} else {
					value = 0;
					return false;
				}
			}

			public static long GetLong(string key, long defaultValue = 0L) {
				int upper = PlayerPrefs.GetInt($"{key}{PREFS_LONG_UPPER}", -1);
				uint lower = (uint)PlayerPrefs.GetInt($"{key}{PREFS_LONG_LOWER}", 0);
				if (upper == -1) {
					return defaultValue;
				} else {
					return ((long)upper << 32) | lower;
				}
			}

			public static long[] GetLongArray(string key, long defaultValue = 0L) {
				int length = PlayerPrefs.GetInt($"{key}{PREFS_ARRAY_LENGTH}", -1);
				if (length == -1) {
					return null;
				} else {
					long[] array = new long[length];
					for (int i = 0; i < length; ++i) {
						array[i] = GetLong($"{key}{PREFS_ARRAY_ELEMENT}[{i}]", defaultValue);
					}
					return array;
				}
			}

			public static bool TryGetLong(string key, out long value) {
				if (PlayerPrefs.HasKey(key)) {
					value = GetLong(key);
					return true;
				} else {
					value = 0L;
					return false;
				}
			}


			public static float GetFloat(string key, float defaultValue = 0f) {
				return PlayerPrefs.GetFloat(key, defaultValue);
			}

			public static float[] GetFloatArray(string key, float defaultValue = 0) {
				int length = PlayerPrefs.GetInt($"{key}{PREFS_ARRAY_LENGTH}", -1);
				if (length == -1) {
					return null;
				} else {
					float[] array = new float[length];
					for (int i = 0; i < length; ++i) {
						array[i] = GetFloat($"{key}{PREFS_ARRAY_ELEMENT}[{i}]", defaultValue);
					}
					return array;
				}
			}


			public static bool TryGetFloat(string key, out float value) {
				if (PlayerPrefs.HasKey(key)) {
					value = GetFloat(key);
					return true;
				} else {
					value = 0f;
					return false;
				}
			}

			public static string GetString(string key, string defaultValue = null) {
				return PlayerPrefs.GetString(key, defaultValue);
			}

			public static bool TryGetString(string key, out string value) {
				if (PlayerPrefs.HasKey(key)) {
					value = GetString(key);
					return true;
				} else {
					value = null;
					return false;
				}
			}

			public static T Get<T>(string key, T defaultValue = default) {
				string json = GetString(key);
				return json == null ? defaultValue : JsonUtility.FromJson<T>(json);
			}

			public static bool TryGet<T>(string key, out T value) {
				if (PlayerPrefs.HasKey(key)) {
					value = Get<T>(key);
					return true;
				} else {
					value = default;
					return false;
				}
			}

			public static T[] GetArray<T>(string key, T defaultValue = default) {
				int length = PlayerPrefs.GetInt($"{key}{PREFS_ARRAY_LENGTH}", -1);
				if (length == -1) {
					return null;
				} else {
					T[] array = new T[length];
					for (int i = 0; i < length; ++i) {
						array[i] = Get($"{key}{PREFS_ARRAY_ELEMENT}[{i}]", defaultValue);
					}
					return array;
				}
			}

			public static void Set(string key, bool value) {
				PlayerPrefs.SetInt(key, value ? 1 : 0);
			}

			public static void Set(string key, bool[] values) {
				PlayerPrefs.SetInt($"{key}{PREFS_ARRAY_LENGTH}", values.Length);
				for (int i = 0; i < values.Length; ++i) {
					Set($"{key}{PREFS_ARRAY_ELEMENT}[{i}]", values[i]);
				}
			}

			public static void Set(string key, int value) {
				PlayerPrefs.SetInt(key, value);
			}

			public static void Set(string key, int[] values) {
				PlayerPrefs.SetInt($"{key}{PREFS_ARRAY_LENGTH}", values.Length);
				for (int i = 0; i < values.Length; ++i) {
					Set($"{key}{PREFS_ARRAY_ELEMENT}[{i}]", values[i]);
				}
			}

			public static void Set(string key, long value) {
				int upper = (int)(value >> 32);
				int lower = (int)(value & 0xFFFFFFFF);

				PlayerPrefs.SetInt($"{key}{PREFS_LONG_UPPER}", upper);
				PlayerPrefs.SetInt($"{key}{PREFS_LONG_LOWER}", lower);
			}

			public static void Set(string key, long[] values) {
				PlayerPrefs.SetInt($"{key}{PREFS_ARRAY_LENGTH}", values.Length);
				for (int i = 0; i < values.Length; ++i) {
					Set($"{key}{PREFS_ARRAY_ELEMENT}[{i}]", values[i]);
				}
			}

			public static void Set(string key, float value) {
				PlayerPrefs.SetFloat(key, value);
			}

			public static void Set(string key, float[] values) {
				PlayerPrefs.SetInt($"{key}{PREFS_ARRAY_LENGTH}", values.Length);
				for (int i = 0; i < values.Length; ++i) {
					Set($"{key}{PREFS_ARRAY_ELEMENT}[{i}]", values[i]);
				}
			}

			public static void Set(string key, string value) {
				PlayerPrefs.SetString(key, value);
			}

			public static void Set<T>(string key, T value) {
				Set(key, JsonUtility.ToJson(key));
			}

			public static void Set<T>(string key, T[] values) {
				PlayerPrefs.SetInt($"{key}{PREFS_ARRAY_LENGTH}", values.Length);
				for (int i = 0; i < values.Length; ++i) {
					Set($"{key}{PREFS_ARRAY_ELEMENT}[{i}]", values[i]);
				}
			}

			public static void Save() {
				PlayerPrefs.Save();
			}

			public static void DeleteKey(string key) {
				PlayerPrefs.DeleteKey(key);
			}

			public static void DeleteAll() {
				PlayerPrefs.DeleteAll();
			}
		}
	}
}