using UnityEngine;

namespace Sankofa.Runtime.Stage
{
    /// <summary>
    /// An animal's walk and rest: the legs swing in diagonal pairs in time with the distance walked, and standing
    /// still it lowers its head to graze now and then. Presentation only; where it walks is <see cref="FigureMotion"/>'s.
    /// </summary>
    public sealed class QuadrupedGait : MonoBehaviour
    {
        [SerializeField] FigureMotion motion;
        [Tooltip("Front left, front right, back left, back right; each pivots at the top of the leg.")]
        [SerializeField] Transform[] legs = new Transform[4];
        [Tooltip("Pivots where the neck meets the body.")]
        [SerializeField] Transform head;
        [SerializeField] float stride = 0.32f;
        [SerializeField] float swing = 26f;
        [SerializeField] float grazeAngle = 50f;

        Quaternion[] legRest;
        Quaternion headRest;
        float stepWeight, graze, grazeTarget, nextGraze = 4f, grazeLeft;

        void Awake()
        {
            legRest = new Quaternion[legs.Length];
            for (var i = 0; i < legs.Length; i++) legRest[i] = legs[i] != null ? legs[i].localRotation : Quaternion.identity;
            if (head != null) headRest = head.localRotation;
        }

        void Update()
        {
            if (motion == null) return;
            var dt = StageClock.DeltaTime;
            var walking = motion.Walking;
            stepWeight = Mathf.MoveTowards(stepWeight, walking ? 1f : 0f, dt / 0.25f);
            var angle = Mathf.Sin(motion.Travelled / stride * Mathf.PI) * swing * stepWeight;
            for (var i = 0; i < legs.Length; i++)
            {
                if (legs[i] == null) continue;
                // Diagonal pairs: front left with back right, front right with back left.
                var sign = i == 0 || i == 3 ? 1f : -1f;
                legs[i].localRotation = legRest[i] * Quaternion.Euler(sign * angle, 0, 0);
            }

            if (walking)
            {
                grazeTarget = 0;
                nextGraze = Random.Range(3f, 7f);
            }
            else if (grazeLeft > 0)
            {
                grazeLeft -= dt;
                if (grazeLeft <= 0) grazeTarget = 0;
            }
            else if ((nextGraze -= dt) <= 0)
            {
                grazeTarget = 1;
                grazeLeft = Random.Range(2f, 3.5f);
                nextGraze = Random.Range(6f, 11f);
            }
            graze = Mathf.MoveTowards(graze, grazeTarget, dt / 0.8f);
            if (head != null) head.localRotation = headRest * Quaternion.Euler(Mathf.SmoothStep(0, 1, graze) * grazeAngle, 0, 0);
        }
    }
}
