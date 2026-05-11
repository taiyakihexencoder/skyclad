using UnityEditor;
using UnityEngine;

namespace skyclad.editor {
	public abstract class PreviewWindow : _PreviewWindow {
		protected interface PreviewCamera {
			Vector3 position { get; set; }
			Quaternion rotation { get; set; }

			float nearClipPlane{ get; set; }
			float farClipPlane{ get; set; }
		}

		protected interface PreviewLight {
			Quaternion rotation { get; set; }
		}

		protected interface PreviewScene {
			void Add(GameObject go);
			void Remove(GameObject go);
		}

		private class Preview : PreviewCamera, PreviewLight, PreviewScene {
			private PreviewRenderUtility _previewRenderer;
			private Texture _renderTexture;
			private GameObject _light;
			private Vector3 _pivot;
			private Quaternion _cameraRotation;
			private float _cameraDistance;

			private const float DEFAULT_CAMERA_DISTANCE = 10.0f;
			private const float CAMERA_DISTANCE_MIN = 1.0f;
			private const float CAMERA_DISTANCE_MAX = 50.0f;

			Vector3 PreviewCamera.position {
				get { return _pivot + _cameraRotation * Vector3.back * _cameraDistance; }
				set { _pivot = value + _cameraRotation * Vector3.forward * _cameraDistance; }
			}

			Quaternion PreviewCamera.rotation {
				get { return _cameraRotation;}
				set { 
					Vector3 position = _pivot + _cameraRotation * Vector3.back * _cameraDistance;
					_cameraRotation = value;
					_pivot = position + value * Vector3.forward * _cameraDistance;
				}
			}

			float PreviewCamera.nearClipPlane {
				get { return _previewRenderer.camera.nearClipPlane; }
				set { _previewRenderer.camera.nearClipPlane = value; }
			}

			float PreviewCamera.farClipPlane {
				get { return _previewRenderer.camera.farClipPlane; }
				set { _previewRenderer.camera.farClipPlane = value; }
			}

			Quaternion PreviewLight.rotation {
				get { return _light.transform.rotation; }
				set { _light.transform.rotation = value; }
			}

			public void OnEnable() {
				_previewRenderer = new PreviewRenderUtility();
				_previewRenderer.camera.clearFlags = CameraClearFlags.Skybox;
				InitLight();
				InitCamera();
			}

			private void InitCamera() {
				_pivot = Vector3.zero;

				_cameraDistance = DEFAULT_CAMERA_DISTANCE;
				_cameraRotation = Quaternion.identity;

				_previewRenderer.camera.transform.position = _cameraRotation * new Vector3(0.0f, 0.0f, -_cameraDistance);
				_previewRenderer.camera.transform.rotation = _cameraRotation;
				_previewRenderer.camera.nearClipPlane = 0.01f;
				_previewRenderer.camera.farClipPlane = 100.0f;
			}

			private void InitLight() {
				_light = new GameObject("Light");
				_previewRenderer.AddSingleGO(_light);
				_light.transform.rotation = Quaternion.Euler(50.0f, -30.0f, 0.0f);
				Light light = _light.AddComponent<Light>();
				light.type = LightType.Directional;
			}

			public void OnDisable() {
				DestroyImmediate(_light);
				_light = null;
				_previewRenderer?.Cleanup();
			}

			public void BeginRender(Rect rect, bool update) {
				if (update) {
					_previewRenderer.BeginPreview(rect, GUIStyle.none);
					_previewRenderer.camera.transform.SetPositionAndRotation(
						_pivot + _cameraRotation * Vector3.back * _cameraDistance,
						_cameraRotation
					);
				}
			}

			public void EndRender(Rect rect, bool update) {
				if (update) {
					_previewRenderer.camera.Render();
					_renderTexture = _previewRenderer.EndPreview();
				}
				EditorGUI.DrawRect(new Rect(rect.x-1, rect.y-1, rect.width+2, rect.height+2), Color.black);
				GUI.DrawTexture(rect, _renderTexture);
			}

			void PreviewScene.Add(GameObject go) {
				_previewRenderer.AddSingleGO(go);
			}

			void PreviewScene.Remove(GameObject go){
				DestroyImmediate(go);
			}

			public void Zoom(float value) {
				_cameraDistance = Mathf.Clamp(_cameraDistance + value, CAMERA_DISTANCE_MIN, CAMERA_DISTANCE_MAX);
			}

			public void TranslateByLookRotation(Vector3 delta) {
				_pivot += _cameraRotation * delta * _cameraDistance / CAMERA_DISTANCE_MAX;
			}

			public void RotateByPivotAxis(Vector3 rot) {
				Vector3 euler = _cameraRotation.eulerAngles + new Vector3(rot.x, rot.y, rot.z);
				euler = new Vector3(Mathf.Clamp((euler.x + 90.0f) % 360, 0.0f, 180.0f) - 90.0f, euler.y, euler.z);
				_cameraRotation = Quaternion.Euler(euler);
			}

			public void CaptureScreen(string path) {
				RenderTexture renderTexture = _renderTexture as RenderTexture;
				RenderTexture tmp = RenderTexture.active;

				RenderTexture.active = renderTexture;
				Texture2D tex = new Texture2D(renderTexture.width, renderTexture.height);
				try {
					tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
					System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
					Debug.Log($"saved:{path}");
				} catch(System.Exception e) {
					Debug.LogError(e);
				} finally {
					DestroyImmediate(tex);
					RenderTexture.active = tmp;
				}

			}
		}

		protected PreviewCamera camera => _preview;
		protected PreviewLight light => _preview;
		protected PreviewScene scene => _preview;
		private Preview _preview;

		/// <summary>
		/// 右クリックドラッグを有効にする
		/// </summary>
		protected bool dragRotate{ get; set; } = true;
		/// <summary>
		/// 中クリックドラッグを有効にする
		/// </summary>
		protected bool dragTranslate{ get; set; } = true;
		/// <summary>
		/// マウスホイールによるズームを有効にする
		/// </summary>
		protected bool mouseWheelZoom{ get; set; } = true;

		/// <summary>
		/// 画面の更新を止める
		/// </summary>
		protected bool previewPaused{ get; set; } = false;

		private const int MOUSE_LEFT_BUTTON = 0;
		private const int MOUSE_RIGHT_BUTTON = 1;
		private const int MOUSE_MIDDLE_BUTTON = 2;

		protected abstract Rect PreviewRect { get; }

		private double _lastFrame = 0.0f;

		protected sealed override void OnEnable() {
			_preview = new Preview();
			_preview.OnEnable();

			OnEnablePreview();
		}

		protected sealed override void OnDisable() {
			OnDisablePreview();
			_preview?.OnDisable();
			_preview = null;
		}

		protected virtual void OnEnablePreview() { }
		protected virtual void OnDisablePreview() { }

		protected sealed override void OnGUI() {
			if (_preview != null) {
				Rect rect = PreviewRect;
				Event evt = Event.current;
				OnObserveEvent(evt);
				if (rect.Contains(evt.mousePosition)) {
					if (!previewPaused) {
						if (evt.type == EventType.MouseDrag) {
							if (dragTranslate && evt.button == MOUSE_MIDDLE_BUTTON) {
								// カメラを移動する
								_preview.TranslateByLookRotation(new Vector3(-evt.delta.x, evt.delta.y, 0.0f) * 10.0f / position.width);
							} else if (dragRotate && evt.button == MOUSE_RIGHT_BUTTON) {
								// カメラを回転する
								float rotH = evt.delta.x / position.width * 180.0f;
								float rotV = evt.delta.y / position.height * 180.0f;
								_preview.RotateByPivotAxis(new Vector3(rotV, rotH, 0.0f));
							}
							evt.Use();
							return;
						} else if (mouseWheelZoom && evt.type == EventType.ScrollWheel) {
							float delta = Mathf.Sign(evt.delta.y);
							_preview.Zoom(delta);
							evt.Use();
							return;
						}
					}

					if (evt.type == EventType.MouseDown && evt.button == MOUSE_LEFT_BUTTON) {
						// 左のダブルクリック
						if (evt.clickCount == 2) {
							string path = EditorUtility.SaveFilePanel("Save Preview", Application.dataPath, "preview.png", "png");
							if (!string.IsNullOrEmpty(path)) {
								_preview.CaptureScreen(path);
							}
						}
					}
				}

				// Repaintのみにしないとマウスイベントなどのたびに再描画となり、
				// スタックしやすくなる
				if (evt.type == EventType.Repaint) {
					_preview.BeginRender(rect, !previewPaused);
					_preview.EndRender(rect, !previewPaused);
				}
			}
		}

		protected sealed override void Update() {
			if (_lastFrame == 0.0) {
				_lastFrame = EditorApplication.timeSinceStartup;
				return;
			}

			double current = EditorApplication.timeSinceStartup;
			if (! previewPaused) {
				OnUpdate(current - _lastFrame);
			}
			_lastFrame = current;
		}

		/// <summary>
		/// 各イベントの処理
		/// </summary>
		/// <param name="evt"></param>
		protected virtual void OnObserveEvent(Event evt) { }

		/// <summary>
		/// EditorWindowのUpdate
		/// </summary>
		/// <param name="deltaTime"></param>
		protected virtual void OnUpdate(double deltaTime) { }
	}

	/// <summary>
	/// 重要な関数を上書きされないために基底クラスで定義してsealedにする。
	/// </summary>
	public abstract class _PreviewWindow : EditorWindow {
		protected abstract void OnEnable();
		protected abstract void OnDisable();
		protected abstract void OnGUI();

		protected abstract void Update();
	}
}