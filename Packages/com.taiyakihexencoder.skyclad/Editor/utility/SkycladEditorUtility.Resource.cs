using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace skyclad.editor {
	public static partial class SkycladEditorUtility {
		public static class Resource {
			private static AddressableAssetSettings settings => AddressableAssetSettingsDefaultObject.GetSettings(true);

			/// <summary>
			/// アドレスリストを取得
			/// </summary>
			public static List<string> GetAddressList(System.Type type) {
				List<string> addressList = new List<string>();
				foreach (AddressableAssetGroup group in settings.groups) {
					foreach (AddressableAssetEntry entry in group.entries) {
						if (type.IsAssignableFrom(entry.TargetAsset.GetType())) {
							addressList.Add(entry.address);
						}
					}
				}
				return addressList;
			}

			/// <summary>
			/// アドレスリストを取得
			/// </summary>
			public static List<string> GetAddressList(System.Type type, string path, params string[] paths) {
				List<string> targetPaths = new List<string>(paths);
				targetPaths.Add(path);
				for(int i = 0; i < targetPaths.Count; ++i) {
					if (!targetPaths[i].EndsWith('/')) {
						targetPaths[i] = $"{targetPaths[i]}/";
					}
				}

				List<string> addressList = new List<string>();
				foreach (AddressableAssetGroup group in settings.groups) {
					foreach (AddressableAssetEntry entry in group.entries) {
						if (type.IsAssignableFrom(entry.TargetAsset.GetType())) {
							if (targetPaths.Exists(p => entry.address.StartsWith(p))) {
								addressList.Add(entry.address);
							}
						}
					}
				}
				return addressList;
			}

			/// <summary>
			/// アドレスリストを取得
			/// </summary>
			public static List<string> GetAddressList(string path, params string[] paths) {
				List<string> targetPaths = new List<string>(paths);
				targetPaths.Add(path);
				for(int i = 0; i < targetPaths.Count; ++i) {
					if (!targetPaths[i].EndsWith('/')) {
						targetPaths[i] = $"{targetPaths[i]}/";
					}
				}

				List<string> addressList = new List<string>();
				foreach (AddressableAssetGroup group in settings.groups) {
					foreach (AddressableAssetEntry entry in group.entries) {
						if (targetPaths.Exists(p => entry.address.StartsWith(p))) {
							addressList.Add(entry.address);
						}
					}
				}
				return addressList;
			}

			/// <summary>
			/// アドレスリストを取得
			/// </summary>
			public static List<string> GetAddressList() {
				List<string> addressList = new List<string>();
				foreach (AddressableAssetGroup group in settings.groups) {
					foreach (AddressableAssetEntry entry in group.entries) {
						addressList.Add(entry.address);
					}
				}
				return addressList;
			}

			/// <summary>
			/// アドレスとGUIDのテーブルを取得
			/// </summary>
			public static Dictionary<string, string> GetAddressAndGuidList(System.Type type) {
				Dictionary<string, string> addressList = new Dictionary<string, string>();
				foreach (AddressableAssetGroup group in settings.groups) {
					foreach (AddressableAssetEntry entry in group.entries) {
						if (type.IsAssignableFrom(entry.TargetAsset.GetType())) {
							addressList.Add(AssetDatabase.AssetPathToGUID(entry.AssetPath), entry.address);
						}
					}
				}
				return addressList;
			}

			/// <summary>
			/// アドレスとGUIDのテーブルを取得
			/// </summary>
			public static Dictionary<string, string> GetAddressAndGuidList(System.Type type, string path, params string[] paths) {
				List<string> targetPaths = new List<string>(paths);
				targetPaths.Add(path);
				for(int i = 0; i < targetPaths.Count; ++i) {
					if (!targetPaths[i].EndsWith('/')) {
						targetPaths[i] = $"{targetPaths[i]}/";
					}
				}

				Dictionary<string, string> addressList = new Dictionary<string, string>();
				foreach (AddressableAssetGroup group in settings.groups) {
					foreach (AddressableAssetEntry entry in group.entries) {
						if (type.IsAssignableFrom(entry.TargetAsset.GetType())) {
							if (targetPaths.Exists(p => entry.address.StartsWith(p))) {
								addressList.Add(AssetDatabase.AssetPathToGUID(entry.AssetPath), entry.address);
							}
						}
					}
				}
				return addressList;
			}

			/// <summary>
			/// アドレスとGUIDのテーブルを取得
			/// </summary>
			public static Dictionary<string, string> GetAddressAndGuidList(string path, params string[] paths) {
				List<string> targetPaths = new List<string>(paths);
				targetPaths.Add(path);
				for(int i = 0; i < targetPaths.Count; ++i) {
					if (!targetPaths[i].EndsWith('/')) {
						targetPaths[i] = $"{targetPaths[i]}/";
					}
				}

				Dictionary<string, string> addressList = new Dictionary<string, string>();
				foreach (AddressableAssetGroup group in settings.groups) {
					foreach (AddressableAssetEntry entry in group.entries) {
						if (targetPaths.Exists(p => entry.address.StartsWith(p))) {
							addressList.Add(AssetDatabase.AssetPathToGUID(entry.AssetPath), entry.address);
						}
					}
				}
				return addressList;
			}

			/// <summary>
			/// アドレスとGUIDのテーブルを取得
			/// </summary>
			public static Dictionary<string, string> GetAddressAndGuidList() {
				Dictionary<string, string> addressList = new Dictionary<string, string>();
				foreach (AddressableAssetGroup group in settings.groups) {
					foreach (AddressableAssetEntry entry in group.entries) {
						addressList.Add(AssetDatabase.AssetPathToGUID(entry.AssetPath), entry.address);
					}
				}
				return addressList;
			}

			public static string GetAddress(string guid) {
				foreach (AddressableAssetGroup group in settings.groups) {
					foreach (AddressableAssetEntry entry in group.entries) {
						if (entry.guid == guid) {
							return entry.address;
						}
					}
				}
				return null;
			}

			public static string GetAssetPathFromAddress(string address) {
				foreach (AddressableAssetGroup group in settings.groups) {
					foreach(AddressableAssetEntry entry in group.entries) {
						if (entry.address == address) {
							return entry.AssetPath;
						}
					}
				}
				return "";
			}

			/// <summary>
			/// アドレスからアセット名を取得する
			/// </summary>
			/// <param name="address"></param>
			/// <returns></returns>
			public static string GetAssetNameFromAddress(string address) {
				foreach (AddressableAssetGroup group in settings.groups) {
					foreach(AddressableAssetEntry entry in group.entries) {
						if (entry.address == address) {
							return entry.MainAsset.name;
						}
					}
				}
				return "";
			}

			/// <summary>
			/// Addressableアドレスを設定する
			/// </summary>
			/// <param name="obj"></param>
			/// <param name="address"></param>
			public static void SetAddress(Object obj, string address) => SetAddress(AssetDatabase.GetAssetPath(obj), address);

			/// <summary>
			/// Addressableアドレスを設定する
			/// </summary>
			/// <param name="assetPath"></param>
			/// <param name="address"></param>
			public static void SetAddress(string assetPath, string address) {
				CreateEntry(assetPath, address, settings.DefaultGroup);
			}

			/// <summary>
			/// グループを指定してAddressableアドレスを設定する
			/// </summary>
			/// <param name="obj"></param>
			/// <param name="groupName"></param>
			/// <param name="address"></param>
			public static void SetAddress(Object obj, string groupName, string address) => SetAddress(AssetDatabase.GetAssetPath(obj), groupName, address);

			/// <summary>
			/// グループを指定してAddressableアドレスを設定する
			/// </summary>
			/// <param name="assetPath"></param>
			/// <param name="groupName"></param>
			/// <param name="address"></param>
			public static void SetAddress(string assetPath, string groupName, string address) {
				AddressableAssetGroup assetGroup = settings.groups.Find(group => group.name == groupName);
				if (assetGroup == null) {
					AddressableAssetGroupTemplate template = settings.GetGroupTemplateObject(0) as AddressableAssetGroupTemplate;
					assetGroup = settings.CreateGroup(groupName, false, false, true, null, template.GetTypes());
				}
				CreateEntry(assetPath, address, assetGroup);
			}

			private static void CreateEntry(string assetPath, string address, AddressableAssetGroup group) {
				string guid = AssetDatabase.AssetPathToGUID(assetPath);
				AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group);
				entry.address = address;
			}

			public static T LoadAssetAtAddress<T>(string address) where T : Object {
				return AssetDatabase.LoadAssetAtPath<T>(GetAssetPathFromAddress(address));
			}
		}
	}
}