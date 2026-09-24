using UnityEngine;

namespace DungeonsCrows.UI
{
    [RequireComponent(typeof(Camera))]
    public sealed class RuntimeMinimapCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField, Min(8f)] private float height = 22f;
        [SerializeField, Min(4f)] private float orthographicSize = 11f;
        [SerializeField, Range(128, 512)] private int textureSize = 256;

        private Camera _camera;
        private RenderTexture _texture;

        public RenderTexture Texture
        {
            get
            {
                EnsureTexture();
                return _texture;
            }
        }

        private void Awake()
        {
            EnsureCamera();
            EnsureTexture();
            SnapToTarget();
        }

        private void LateUpdate()
        {
            SnapToTarget();
        }

        public void Configure(
            Transform followTarget,
            float cameraHeight = 22f,
            float mapSize = 11f,
            int renderTextureSize = 256)
        {
            target = followTarget;
            height = Mathf.Max(8f, cameraHeight);
            orthographicSize = Mathf.Max(4f, mapSize);
            textureSize = Mathf.Clamp(
                renderTextureSize,
                128,
                512);

            EnsureCamera();

            if (Application.isPlaying)
                EnsureTexture();

            SnapToTarget();
        }

        private void EnsureCamera()
        {
            if (_camera == null)
                _camera = GetComponent<Camera>();

            _camera.orthographic = true;
            _camera.orthographicSize = orthographicSize;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor =
                new Color(0.018f, 0.022f, 0.025f, 1f);
            _camera.nearClipPlane = 0.3f;
            _camera.farClipPlane = 80f;
            _camera.allowHDR = false;
            _camera.allowMSAA = false;
        }

        private void EnsureTexture()
        {
            EnsureCamera();

            if (_texture != null &&
                _texture.width == textureSize &&
                _texture.height == textureSize)
            {
                if (_camera.targetTexture != _texture)
                    _camera.targetTexture = _texture;
                return;
            }

            ReleaseTexture();

            _texture = new RenderTexture(
                textureSize,
                textureSize,
                16,
                RenderTextureFormat.ARGB32)
            {
                name = "DungeonsCrows_Minimap",
                antiAliasing = 1,
                useMipMap = false,
                autoGenerateMips = false
            };

            _texture.Create();
            _camera.targetTexture = _texture;
        }

        private void SnapToTarget()
        {
            if (target == null)
                return;

            transform.position =
                target.position +
                Vector3.up * height;

            transform.rotation =
                Quaternion.Euler(
                    90f,
                    0f,
                    0f);
        }

        private void ReleaseTexture()
        {
            if (_camera != null &&
                _camera.targetTexture == _texture)
            {
                _camera.targetTexture = null;
            }

            if (_texture == null)
                return;

            _texture.Release();

            if (Application.isPlaying)
                Destroy(_texture);
            else
                DestroyImmediate(_texture);

            _texture = null;
        }

        private void OnDestroy()
        {
            ReleaseTexture();
        }
    }
}
