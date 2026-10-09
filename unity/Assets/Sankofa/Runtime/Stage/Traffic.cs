using UnityEngine;

namespace Sankofa.Runtime.Stage
{
    /// <summary>
    /// Cars on the far road, seen only as their lights: even-numbered lights come toward the left in their own lane,
    /// odd ones go the other way, each at its own steady pace with a gap behind it, wrapping at the road's ends.
    /// Ambient only; it never reacts to the story.
    /// </summary>
    public sealed class Traffic : MonoBehaviour
    {
        [Tooltip("The road's centre line, local to this object, from one end to the other.")]
        [SerializeField] Vector3[] road = new Vector3[0];
        [SerializeField] Transform[] lights = new Transform[0];
        [SerializeField] float speed = 11f;
        [Tooltip("Half the distance between the two lanes, in metres.")]
        [SerializeField] float lane = 1.6f;
        [SerializeField] int seed;

        float[] along;
        float[] start;
        float[] pace;
        float length;

        void Awake()
        {
            along = new float[road.Length];
            for (var i = 1; i < road.Length; i++) along[i] = along[i - 1] + Vector3.Distance(road[i - 1], road[i]);
            length = road.Length > 1 ? along[road.Length - 1] : 0f;
            var random = new System.Random(seed);
            start = new float[lights.Length];
            pace = new float[lights.Length];
            for (var i = 0; i < lights.Length; i++)
            {
                start[i] = (float)random.NextDouble() * length;
                pace[i] = speed * (0.75f + 0.5f * (float)random.NextDouble());
            }
        }

        void Update()
        {
            if (length <= 0f) return;
            for (var i = 0; i < lights.Length; i++)
            {
                var toward = i % 2 == 0;
                var s = Mathf.Repeat(start[i] + Time.time * pace[i], length);
                if (toward) s = length - s;
                lights[i].localPosition = At(s, toward ? -lane : lane);
            }
        }

        /// <summary>The point at a distance along the road, offset sideways into a lane.</summary>
        Vector3 At(float s, float offset)
        {
            var k = 1;
            while (k < road.Length - 1 && along[k] < s) k++;
            var a = road[k - 1];
            var b = road[k];
            var t = Mathf.InverseLerp(along[k - 1], along[k], s);
            var side = Vector3.Cross(Vector3.up, (b - a).normalized);
            return Vector3.Lerp(a, b, t) + side * offset;
        }
    }
}
