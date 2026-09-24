using System.Collections;
using UnityEngine;

namespace DungeonsCrows.World
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    public sealed class MorphicLandscapeController : MonoBehaviour
    {
        [SerializeField, Min(8f)] private float width = 28f;
        [SerializeField, Min(8f)] private float depth = 28f;
        [SerializeField, Range(9, 65)] private int resolution = 29;
        [SerializeField, Min(0.05f)] private float morphDuration = 2.8f;
        [SerializeField] private AnimationCurve morphCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private MeshFilter _filter;
        private MeshCollider _collider;
        private Mesh _mesh;
        private Vector3[] _vertices;
        private Vector3[] _startVertices;
        private Vector3[] _targetVertices;
        private int[] _triangles;
        private Coroutine _morph;
        private int _chapter = 1;

        public int CurrentChapter => _chapter;
        public bool IsMorphing => _morph != null;
        public int VertexCount =>
            _vertices != null ? _vertices.Length : 0;

        private void Awake()
        {
            EnsureMesh();
        }

        public void Configure(
            Material material,
            float landscapeWidth = 28f,
            float landscapeDepth = 28f,
            int gridResolution = 29)
        {
            width = Mathf.Max(8f, landscapeWidth);
            depth = Mathf.Max(8f, landscapeDepth);
            resolution = Mathf.Clamp(
                gridResolution,
                9,
                65);

            MeshRenderer renderer =
                GetComponent<MeshRenderer>();
            if (material != null)
                renderer.sharedMaterial = material;

            RebuildMesh();
            SnapToChapter(_chapter);
        }

        public void CommitStoryBoundary(int chapter)
        {
            int next = Mathf.Clamp(chapter, 1, 3);

            if (_morph != null)
                StopCoroutine(_morph);

            EnsureMesh();
            _morph = StartCoroutine(
                MorphRoutine(next));
        }

        public void SnapToChapter(int chapter)
        {
            _chapter = Mathf.Clamp(chapter, 1, 3);
            EnsureMesh();

            FillChapterVertices(
                _chapter,
                _vertices);

            ApplyVertices(
                updateCollider: true);
        }

        private IEnumerator MorphRoutine(int nextChapter)
        {
            EnsureMesh();

            if (_startVertices == null ||
                _startVertices.Length != _vertices.Length)
            {
                _startVertices =
                    new Vector3[_vertices.Length];
                _targetVertices =
                    new Vector3[_vertices.Length];
            }

            System.Array.Copy(
                _vertices,
                _startVertices,
                _vertices.Length);

            FillChapterVertices(
                nextChapter,
                _targetVertices);

            float elapsed = 0f;

            while (elapsed < morphDuration)
            {
                elapsed += Time.deltaTime;

                float normalized =
                    Mathf.Clamp01(
                        elapsed / morphDuration);
                float t =
                    morphCurve.Evaluate(normalized);

                for (int i = 0;
                     i < _vertices.Length;
                     i++)
                {
                    _vertices[i] =
                        Vector3.LerpUnclamped(
                            _startVertices[i],
                            _targetVertices[i],
                            t);
                }

                ApplyVertices(
                    updateCollider: false);

                yield return null;
            }

            System.Array.Copy(
                _targetVertices,
                _vertices,
                _vertices.Length);

            _chapter = nextChapter;
            ApplyVertices(
                updateCollider: true);
            _morph = null;
        }

        private void EnsureMesh()
        {
            if (_filter == null)
                _filter = GetComponent<MeshFilter>();

            if (_collider == null)
                _collider = GetComponent<MeshCollider>();

            if (_mesh == null ||
                _vertices == null ||
                _vertices.Length !=
                resolution * resolution)
            {
                RebuildMesh();
            }
        }

        private void RebuildMesh()
        {
            if (_filter == null)
                _filter = GetComponent<MeshFilter>();

            if (_collider == null)
                _collider = GetComponent<MeshCollider>();

            if (_mesh != null)
            {
                if (Application.isPlaying)
                    Destroy(_mesh);
                else
                    DestroyImmediate(_mesh);
            }

            _mesh = new Mesh
            {
                name = "DungeonsCrows_MorphicLandscape"
            };
            _mesh.MarkDynamic();

            int vertexCount =
                resolution * resolution;
            _vertices =
                new Vector3[vertexCount];
            _startVertices =
                new Vector3[vertexCount];
            _targetVertices =
                new Vector3[vertexCount];

            _triangles =
                new int[
                    (resolution - 1) *
                    (resolution - 1) *
                    6];

            int triangle = 0;

            for (int z = 0;
                 z < resolution - 1;
                 z++)
            {
                for (int x = 0;
                     x < resolution - 1;
                     x++)
                {
                    int i =
                        z * resolution + x;

                    _triangles[triangle++] = i;
                    _triangles[triangle++] =
                        i + resolution;
                    _triangles[triangle++] =
                        i + 1;

                    _triangles[triangle++] =
                        i + 1;
                    _triangles[triangle++] =
                        i + resolution;
                    _triangles[triangle++] =
                        i + resolution + 1;
                }
            }

            _mesh.vertices = _vertices;
            _mesh.triangles = _triangles;
            _mesh.RecalculateBounds();
            _mesh.RecalculateNormals();

            _filter.sharedMesh = _mesh;
        }

        private void FillChapterVertices(
            int chapter,
            Vector3[] destination)
        {
            float halfWidth = width * 0.5f;
            float halfDepth = depth * 0.5f;

            for (int z = 0;
                 z < resolution;
                 z++)
            {
                float vz =
                    (float)z /
                    (resolution - 1);
                float localZ =
                    Mathf.Lerp(
                        -halfDepth,
                        halfDepth,
                        vz);

                for (int x = 0;
                     x < resolution;
                     x++)
                {
                    float vx =
                        (float)x /
                        (resolution - 1);
                    float localX =
                        Mathf.Lerp(
                            -halfWidth,
                            halfWidth,
                            vx);

                    float height =
                        HeightFor(
                            chapter,
                            localX,
                            localZ);

                    destination[
                        z * resolution + x] =
                        new Vector3(
                            localX,
                            height,
                            localZ);
                }
            }
        }

        private static float HeightFor(
            int chapter,
            float x,
            float z)
        {
            float radial =
                Mathf.Sqrt(x * x + z * z);

            float baseWave =
                Mathf.Sin(x * 0.22f) * 0.14f +
                Mathf.Cos(z * 0.19f) * 0.12f;

            float centerSafe =
                Mathf.Clamp01(
                    Mathf.Max(
                        Mathf.Abs(x) - 2.2f,
                        Mathf.Abs(z + 2f) - 5f) /
                    4f);

            float height = baseWave * centerSafe;

            if (chapter >= 2)
            {
                float rookeryRidge =
                    Mathf.Sin(
                        (x + z) * 0.33f) *
                    0.34f;

                float cryptSink =
                    -Mathf.Exp(
                        -radial * 0.18f) *
                    0.42f;

                height +=
                    (rookeryRidge +
                     cryptSink) *
                    centerSafe;
            }

            if (chapter >= 3)
            {
                float throneRing =
                    Mathf.Sin(
                        radial * 0.72f) *
                    0.38f;

                float fault =
                    Mathf.Sin(
                        x * 0.48f -
                        z * 0.27f) *
                    0.22f;

                height +=
                    (throneRing + fault) *
                    centerSafe;
            }

            return height;
        }

        private void ApplyVertices(
            bool updateCollider)
        {
            _mesh.vertices = _vertices;
            _mesh.RecalculateBounds();
            _mesh.RecalculateNormals();

            if (updateCollider)
            {
                _collider.sharedMesh = null;
                _collider.sharedMesh = _mesh;
            }
        }

        private void OnDestroy()
        {
            if (_mesh == null)
                return;

            if (Application.isPlaying)
                Destroy(_mesh);
            else
                DestroyImmediate(_mesh);
        }
    }
}
