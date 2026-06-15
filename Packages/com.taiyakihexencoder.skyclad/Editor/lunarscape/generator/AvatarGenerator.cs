using System.Collections.Generic;
using System.Text.RegularExpressions;
using skyclad.lunarscape.internalProc;
using UnityEditor;

namespace skyclad.lunarscape.editor {
	internal sealed class AvatarGenerator : ScriptGenerator {
		internal bool Validation(out string message) {
			return ValidationColliderTable(out message) &&
				ValidationAvatarTable(out message);
		}

		private bool ValidationAvatarTable(out string message) {
			message = "";
			string[] guids = AssetDatabase.FindAssets($"t:{typeof(LunarscapeAvatarTable)}");
			LunarscapeAvatarTable table = null;
			if (guids.Length > 0) {
				List<int> colliderIds = GetColliderIds();

				string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
				table = AssetDatabase.LoadAssetAtPath<LunarscapeAvatarTable>(assetPath);
				
				bool result = true;
				Regex regexVarName = new Regex(@"^[a-zA-Z_]+[a-zA-Z0-9_]*$");
				List<string> nameList = new List<string>();
				foreach(LunarscapeAvatarTable.AvatarSettings setting in table.Settings) {
					if (string.IsNullOrEmpty(setting.name)) {
						message += $"Avatar empty name exists{System.Environment.NewLine}";
						result = false;
					} else if (nameList.Contains(setting.name)) {
						message += $"Avatar duplicated name:{setting.name}{System.Environment.NewLine}";
						result = false;
					} else if (!regexVarName.IsMatch(setting.name)) {
						message += $"Avatar invalid name:{setting.name}{System.Environment.NewLine}";
						result = false;
					} else if (!colliderIds.Contains(setting.collider) && setting.collider != LunarscapeAvatarPhysicsColliderTable.EMPTY_COLLIDER) {
						message += $"Avatar invalid collider:{setting.collider}({setting.name})";
						result = false;
					}
					nameList.Add(setting.name);
				}

				return result;
			} else {
				return true;
			}
		}

		private List<int> GetColliderIds() {
			string[] guids = AssetDatabase.FindAssets($"t:{typeof(LunarscapeAvatarPhysicsColliderTable)}");
			LunarscapeAvatarPhysicsColliderTable table = null;
			List<int> colliderIds = new List<int>();
			if (guids.Length > 0) {
				string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
				table = AssetDatabase.LoadAssetAtPath<LunarscapeAvatarPhysicsColliderTable>(assetPath);
				foreach(LunarscapeAvatarPhysicsColliderTable.PhysicsColliderSettings collider in table.Colliders) {
					colliderIds.Add(collider.id);
				}
			}
			return colliderIds;
		}

		private bool ValidationColliderTable(out string message) {
			message = "";
			string[] guids = AssetDatabase.FindAssets($"t:{typeof(LunarscapeAvatarPhysicsColliderTable)}");
			LunarscapeAvatarPhysicsColliderTable table = null;
			if (guids.Length > 0) {
				string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
				table = AssetDatabase.LoadAssetAtPath<LunarscapeAvatarPhysicsColliderTable>(assetPath);

				bool result = true;
				Regex regexVarName = new Regex(@"^[a-zA-Z_]+[a-zA-Z0-9_]*$");
				List<string> nameList = new List<string>();
				foreach(LunarscapeAvatarPhysicsColliderTable.PhysicsColliderSettings collider in table.Colliders) {
					if (string.IsNullOrEmpty(collider.name)) {
						message += $"Collider empty name exists{System.Environment.NewLine}";
						result = false;
					} else if (collider.radius == 0) {
						message += $"Collider radius cannot zero:{collider.name}{System.Environment.NewLine}";
						result = false;
					} else if (collider.height < collider.radius * 2f) {
						message += $"Collider height too small:{collider.name}{System.Environment.NewLine}";
						result = false;
					} else if (nameList.Contains(collider.name)) {
						message += $"Collider duplicated name:{collider.name}{System.Environment.NewLine}";
						result = false;
					} else if (!regexVarName.IsMatch(collider.name)) {
						message += $"Collider invalid name:{collider.name}{System.Environment.NewLine}";
						result = false;
					}

					nameList.Add(collider.name);
				}
				return result;
			} else {
				return true;
			}
		}

		protected override void WriteScript() {
			string[] guids = AssetDatabase.FindAssets($"t:{typeof(LunarscapeAvatarTable)}");
			if (guids.Length > 0) {
				List<int> colliderIds = GetColliderIds();

				string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
				LunarscapeAvatarTable table = AssetDatabase.LoadAssetAtPath<LunarscapeAvatarTable>(assetPath);

				using(Namespace("skyclad.lunarscape")) {
					using(Struct("AvatarId", isPartial: true, isStatic: false, isReadonly: true)) {
						foreach(LunarscapeAvatarTable.AvatarSettings setting in table.Settings) {
							AppendLine($"public static readonly AvatarId {setting.name} = new AvatarId({setting.id}, \"{setting.name}\");");
						}
					}
				}
			}
		}
	}
}