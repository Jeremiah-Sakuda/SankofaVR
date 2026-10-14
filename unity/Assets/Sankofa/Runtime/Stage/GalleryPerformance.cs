using System;
using Sankofa.Core.Story;
using UnityEngine;

namespace Sankofa.Runtime.Stage
{
    [Serializable] public sealed class NarrativePose
    {
        public string Line;
        public IllustratedFigure Figure;
        public int Pose;
    }

    /// <summary>Authored line cues perform the story. Only VisitFlow resolves the visitor's invitations.</summary>
    public sealed class GalleryPerformance : MonoBehaviour
    {
        [SerializeField] NarrativePose[] poses = new NarrativePose[0];
        [SerializeField] MemoryPerformance[] memories = new MemoryPerformance[0];
        public void Speak(Line line)
        {
            foreach (var cue in poses) if (cue.Line == line.Id) cue.Figure.SetPose(cue.Pose);
            foreach (var memory in memories) memory.Speak(line.Id);
        }
        public void ResetState() { foreach (var memory in memories) memory.ResetState(); }
    }
}
