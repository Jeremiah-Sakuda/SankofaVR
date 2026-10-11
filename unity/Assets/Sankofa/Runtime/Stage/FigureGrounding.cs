using UnityEngine;

namespace Sankofa.Runtime.Stage
{
    /// <summary>Locks stance feet in the stage frame over the generic walk. Ground height follows the authored
    /// route; there is no physics query, inferred room surface or story dependency.</summary>
    [DefaultExecutionOrder(5)]
    [RequireComponent(typeof(FigureMotion))]
    public sealed class FigureGrounding : MonoBehaviour
    {
        [SerializeField] Transform[] rightLeg = new Transform[0];
        [SerializeField] Transform[] leftLeg = new Transform[0];
        [SerializeField] float rightSole, leftSole;
        [SerializeField] Animator animator;
        FigureMotion motion;
        readonly Vector3[] planted = new Vector3[2];
        readonly bool[] contact = new bool[2];
        readonly int[] contactVersion = new int[2];
        readonly Quaternion[] plantedTurn = new Quaternion[2];
        int lastFrame = -1;
        public bool RightPlanted => contact[0];
        public int RightContactVersion => contactVersion[0];
        public bool LeftPlanted => contact[1];
        void Awake() => motion = GetComponent<FigureMotion>();
        void OnDisable() => ResetContacts();
        internal void ResetContacts() { contact[0] = contact[1] = false; lastFrame = -1; }

        void LateUpdate()
        {
            if (motion.Kneel > 0.05f || animator.IsInTransition(0)) { contact[0] = contact[1] = false; return; }
            Place(rightLeg, rightSole, 0);
            Place(leftLeg, leftSole, 1);
            lastFrame = Time.frameCount;
        }

        void Place(Transform[] leg, float sole, int index)
        {
            if (leg.Length != 3) return;
            var foot = leg[2];
            var height = transform.position.y + sole;
            // A raised foot belongs to the swing phase; release it before posing so its next step stays free.
            if (motion.WalkVelocity > 0.015f && foot.position.y > height + 0.045f) contact[index] = false;
            var frame = transform.parent;
            Vector3 InWorld(Vector3 p) => frame != null ? frame.TransformPoint(p) : p;
            if (contact[index] && Vector3.Distance(InWorld(planted[index]), foot.position) > 0.24f) contact[index] = false;
            if (!contact[index] && foot.position.y <= height + 0.035f)
            {
                var target = foot.position; target.y = height;
                planted[index] = frame != null ? frame.InverseTransformPoint(target) : target;
                plantedTurn[index] = frame != null ? Quaternion.Inverse(frame.rotation) * foot.rotation : foot.rotation;
                contact[index] = true;
                contactVersion[index]++;
            }
            if (!contact[index]) return;
            var goal = InWorld(planted[index]);
            // Uphill/downhill authored routes change the sole height gradually, while X/Z stay planted.
            if (StageClock.DeltaTime > 0 && lastFrame >= 0)
            {
                goal.y = Mathf.MoveTowards(goal.y, height, StageClock.DeltaTime * 0.15f);
                planted[index] = frame != null ? frame.InverseTransformPoint(goal) : goal;
            }
            FigureMotion.SolveArm(leg, goal, 1);
            foot.rotation = (frame != null ? frame.rotation : Quaternion.identity) * plantedTurn[index];
        }
    }
}
