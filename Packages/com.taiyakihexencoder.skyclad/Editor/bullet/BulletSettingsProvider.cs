using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace skyclad.editor {
	internal sealed class BulletSettingsProvider : SettingsProvider {
		private SerializedObject serializedObject;
		private BulletSettingsModel bulletSettingsModel;

		private int _toolbarIndex = 0;
		private float _toolbarScroll = 0.0f;
		private float _bulletGroupScroll = 0.0f;

		internal BulletSettingsProvider(
			string path,
			SettingsScope scopes,
			IEnumerable<string> keywords = null
		) : base(path, scopes, keywords) {
			serializedObject = new SerializedObject(SkycladProjectSettings.Instance);
			bulletSettingsModel = new BulletSettingsModel(serializedObject);
		}

		[SettingsProvider]
		public static SettingsProvider CreateSettingsProvider() {
			return new BulletSettingsProvider(
				path: "Skyclad/Bullet",
				scopes: SettingsScope.Project,
				keywords: new string[] { "skyclad", }
			);
		}

		public override void OnGUI(string searchContext) {
			using (bulletSettingsModel.ChangeScope) {
				Toolbar();
				if (_toolbarIndex == 0) {
					OnGUIBulletGeneral();
				} else {
					OnGUIBulletGroup(_toolbarIndex-1);
				}
			}
		}

		private void Toolbar() {
			List<string> tabs = bulletSettingsModel.GroupNames;
			tabs.Insert(0, "General");
			int index = SkycladEditor.GUI.Layout.Toolbar(_toolbarIndex, ref _toolbarScroll, Repaint, tabs.ToArray());

			if (_toolbarIndex != index) {
				_toolbarIndex = index;
				_bulletGroupScroll = 0.0f;
			}
		}

		private void OnGUIBulletGeneral() {
			using (SkycladEditor.GUI.Layout.VerticalScroll(ref _bulletGroupScroll)) {
				using (SkycladEditor.GUI.Layout.Horizontal) {
					if (SkycladEditor.GUI.Layout.Button("Update Script")) {
						BulletIdScriptGenerator.Generate(serializedObject);
						BulletParameterScriptGenerator.Generate(serializedObject);
					}
					SkycladEditor.GUI.Layout.Space(width: 48);
					if (SkycladEditor.GUI.Layout.Button("Update Binary")) {
						BulletParameterTableBinaryGenerator.Generate(serializedObject, "bullet.bytes");
					}
				}

				SkycladEditor.GUI.Layout.Space(height: 24);

				SkycladEditor.GUI.Layout.Label("Parameter Defs");
				using (SkycladEditor.GUI.Layout.Box(new RectOffset(16,16,0,0))) {
					SerializedProperty parameterDefs = bulletSettingsModel.ParameterDefsProperty;
					ParameterDefs.Editor(parameterDefs);
				}

				SkycladEditor.GUI.Layout.Space(height: 24);

				SkycladEditor.GUI.Layout.Label("Groups");
				List<string> groupNames = bulletSettingsModel.GroupNames;
				for(int i = 0; i < groupNames.Count; ++i) {
					using(SkycladEditor.GUI.Layout.Horizontal) {
						string groupName = SkycladEditor.GUI.Layout.TextField(
							groupNames[i],
							SkycladEditor.Modifier
								.Width(200.0f)
						);
						if (groupName != groupNames[i]) {
							bulletSettingsModel.SetGroupName(i, groupName);
						}

						SkycladEditor.GUI.Layout.Space(width: 32);

						if (SkycladEditor.GUI.Layout.MinusButton()) {
							bulletSettingsModel.DeleteGroup(i);
							break;
						}
					}
				}

				if (SkycladEditor.GUI.Layout.PlusButton()) {
					bulletSettingsModel.AddGroup();
				}
			}
		}

		private void OnGUIBulletGroup(int index) {
			using (SkycladEditor.GUI.Layout.VerticalScroll(ref _bulletGroupScroll)) {
				SerializedProperty property = bulletSettingsModel.GroupProperty(index);
				SerializedProperty groupNameProperty = property.Of("name");
				SerializedProperty hitBoxLayerProperty = property.Of("hitBoxLayer");

				groupNameProperty.stringValue = SkycladEditor.GUI.Layout.TextField(
					groupNameProperty.stringValue,
					SkycladEditor.Modifier
						.Label("Name")
				);

				hitBoxLayerProperty.intValue = SkycladEditor.GUI.Layout.IntPopup(
					hitBoxLayerProperty.intValue,
					bulletSettingsModel.LayerValues,
					bulletSettingsModel.LayerNames,
					SkycladEditor.Modifier
						.Label("Layer")
				);

				SerializedProperty unitsProperty = property.Of("units");
				List<ParameterDefs> parameterList = bulletSettingsModel.ParameterList;

				SkycladEditor.GUI.Layout.Label("Bullet");

				using(SkycladEditor.GUI.Layout.Box(new RectOffset(12, 12, 0, 0))) {
					for(int i = 0; i < unitsProperty.arraySize; ++i) {
						SerializedProperty unitProperty = unitsProperty.Of(i);
						SerializedProperty nameProperty = unitProperty.Of("name");

						bool foldout;
						using (SkycladEditor.GUI.Layout.Horizontal) {
							foldout = SkycladEditor.GUI.Layout.Foldout(unitProperty, nameProperty.stringValue);
							SkycladEditor.GUI.Layout.Space(width: 24);
							if (SkycladEditor.GUI.Layout.MinusButton()) {
								unitsProperty.DeleteArrayElementAtIndex(i);
								break;
							}
						}

						if (foldout) {
							using (SkycladEditor.GUI.Layout.Box(new RectOffset(20, 20, 0, 0))) {
								SerializedProperty hitBoxTypeProperty = unitProperty.Of("hitBoxType");
								SerializedProperty extentProperty = unitProperty.Of("extent");
								SerializedProperty parameterProperty = unitProperty.Of("parameter");

								nameProperty.stringValue = SkycladEditor.GUI.Layout.TextField(
									text: nameProperty.stringValue,
									SkycladEditor.Modifier
										.Label("Name")
								);

								hitBoxTypeProperty.intValue = SkycladEditor.GUI.Layout.EnumField<BulletHitBoxType>(
									hitBoxTypeProperty.intValue, 
									SkycladEditor.Modifier
										.Label("HitBoxType")
								);

								switch((BulletHitBoxType)hitBoxTypeProperty.intValue) {
									case BulletHitBoxType.Box: {
										extentProperty.vector3Value = SkycladEditor.GUI.Layout.Vector3Field(
											extentProperty.vector3Value,
											SkycladEditor.Modifier
												.Label("Extent")
										);
										break;
									}
									case BulletHitBoxType.Sphere: {
										float radius = extentProperty.vector3Value.x;
										radius = SkycladEditor.GUI.Layout.FloatField(
											radius,
											SkycladEditor.Modifier
												.Label("Radius")
										);
										extentProperty.vector3Value = new Vector3(radius, 0.0f, 0.0f);
										break;
									}
									case BulletHitBoxType.CylinderV: {
										float radius = extentProperty.vector3Value.x;
										float height = extentProperty.vector3Value.y;
										using (SkycladEditor.GUI.Layout.Horizontal) {
											radius = SkycladEditor.GUI.Layout.FloatField(
												radius,
												SkycladEditor.Modifier
													.Label("Radius")
													.ExpandWidth
											);
											height = SkycladEditor.GUI.Layout.FloatField(
												height,
												SkycladEditor.Modifier
													.Label("Height")
													.ExpandWidth
											);
										}
										extentProperty.vector3Value = new Vector3(radius, height, 0.0f);
										break;
									}
									case BulletHitBoxType.CylinderH: {
										float radius = extentProperty.vector3Value.x;
										float height = extentProperty.vector3Value.y;
										using (SkycladEditor.GUI.Layout.Horizontal) {
											radius = SkycladEditor.GUI.Layout.FloatField(
												radius,
												SkycladEditor.Modifier
													.Label("Radius")
													.ExpandWidth
											);
											height = SkycladEditor.GUI.Layout.FloatField(
												height,
												SkycladEditor.Modifier
													.Label("Height")
													.ExpandWidth
											);
										}
										extentProperty.vector3Value = new Vector3(radius, height, 0.0f);
										break;
									}
								}

								SkycladEditor.GUI.Layout.Space(height: 12);

								SkycladEditor.GUI.Layout.Label("Parameters");

								using(SkycladEditor.GUI.Layout.Box(new RectOffset(16,16,4,4))) {
									ParameterInfo.Editor(parameterProperty, parameterList);
								}

								SkycladEditor.GUI.Layout.Space(height: 12);

								SkycladEditor.GUI.Layout.Label("Style");
								using(SkycladEditor.GUI.Layout.Box(new RectOffset(16,16,4,4))) {
									BulletStyleEdit(unitProperty.Of("style"));
								}

								SkycladEditor.GUI.Layout.Space(height: 12);
							}
						}
					}

					if (SkycladEditor.GUI.Layout.PlusButton()) {
						bulletSettingsModel.AddUnit(index);
					}

				}

			}
		}

		private void BulletStyleEdit(SerializedProperty property) {
			SerializedProperty lifeTimeProperty = property.Of("lifeTime");
			SerializedProperty trailProperty = property.Of("trail");
			SerializedProperty speedProperty = property.Of("speed");

			using (SkycladEditor.GUI.Layout.Horizontal) {
				SkycladEditor.GUI.Layout.Label("Life time", SkycladEditor.Modifier.Width(70.0f));
				lifeTimeProperty.floatValue = SkycladEditor.GUI.Layout.FloatField(
					lifeTimeProperty.floatValue,
					SkycladEditor.Modifier
						.Width(100.0f)
				);
			}
			using (SkycladEditor.GUI.Layout.Horizontal) {
				SkycladEditor.GUI.Layout.Label("Trail", SkycladEditor.Modifier.Width(70.0f));
				trailProperty.intValue = SkycladEditor.GUI.Layout.EnumField<bullet.BulletTrail>(
					trailProperty.intValue,
					SkycladEditor.Modifier
						.Width(100.0f)
				);

				SkycladEditor.GUI.Layout.Space(width: 30);

				switch((bullet.BulletTrail)trailProperty.intValue) {
					case bullet.BulletTrail.None: {
						break;
					}
					case bullet.BulletTrail.Straight: {
						SkycladEditor.GUI.Layout.Label("Speed");
						speedProperty.floatValue = SkycladEditor.GUI.Layout.FloatField(
							speedProperty.floatValue,
							SkycladEditor.Modifier
								.Width(100.0f)
						);
						break;
					}
				}
			}
		}
	}
}