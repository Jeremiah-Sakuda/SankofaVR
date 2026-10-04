using System;
using System.Collections.Generic;
using Sankofa.Interaction;
using Sankofa.Runtime.Stage;
using UnityEngine;
using Num = System.Numerics;

namespace Sankofa.Runtime.Input
{
    /// <summary>
    /// Reports which visible story object the visitor's head has rested toward. Head direction is not eye tracking:
    /// the application only uses it to offer an optional question, never to make or confirm a choice.
    /// </summary>
    public sealed class AttentionSource : MonoBehaviour
    {
        [SerializeField] Transform head;
        [SerializeField] StoryStage stage;

        readonly AttentionTracker tracker = new AttentionTracker();
        readonly List<AttentionCandidate> candidates = new List<AttentionCandidate>();

        /// <summary>Only while a story is playing; otherwise nothing is tracked.</summary>
        public bool Listening { get; set; }
        public string Current => tracker.Current;
        public event Action<string> Noticed;

        void Update()
        {
            if (!Listening)
            {
                tracker.Reset();
                return;
            }
            candidates.Clear();
            foreach (var o in stage.Objects)
                if (o.AttentionRadius > 0 && o.Visible && o.Scene == stage.ActiveScene)
                    candidates.Add(new AttentionCandidate(o.Id, N(o.transform.position), o.AttentionRadius));
            var noticed = tracker.Update(N(head.position), N(head.forward), candidates, Time.deltaTime);
            if (noticed != null) Noticed?.Invoke(noticed);
        }

        static Num.Vector3 N(Vector3 v) => new Num.Vector3(v.x, v.y, v.z);
    }
}
