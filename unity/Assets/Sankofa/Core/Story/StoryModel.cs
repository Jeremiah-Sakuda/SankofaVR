using System;
using System.Collections.Generic;

namespace Sankofa.Core.Story
{
    /// <summary>How the fire behaves while a line is spoken or the story waits. Presentation decides the look.</summary>
    public enum FireState { Resting, Welcoming, Listening, Gathering, Telling, Holding, Transitioning }

    /// <summary>The hearth is where every visit begins and ends; story scenes surround the stationary visitor.</summary>
    public enum SceneKind { Hearth, Story }

    /// <summary>Documented context, a visitor's own account, or imaginative reconstruction. Never inferred from a model label.</summary>
    public enum EvidenceState { Documented, VisitorProvided, Reconstruction }

    /// <summary>The bounded set of things a story may do to a staged object.</summary>
    public enum ActionKind { Reveal, Illuminate, Offer, Place }

    /// <summary>A deliberate physical invitation. Receive: an open palm (or a nearby select) takes the offered object.</summary>
    public enum InvitationKind { Receive }

    /// <summary>Accepted is the visitor's explicit action. Default is the authored passive path, never presented as a choice.</summary>
    public enum Outcome { Accepted, Default }

    /// <summary>Review status of a content package. Only reviewed content may be presented as release content.</summary>
    public enum ContentStatus { Placeholder, Draft, Reviewed }

    /// <summary>
    /// A complete bundled story package: its scenes, staged objects, claims, the fire's welcome and the beats of
    /// the story. Built by <see cref="EpisodeReader"/>; playable only after <see cref="EpisodeValidator"/> accepts it.
    /// </summary>
    public sealed class Episode
    {
        public int Schema { get; internal set; }
        public string Id { get; internal set; }
        public string Version { get; internal set; }
        public string Title { get; internal set; }
        public string Locale { get; internal set; }
        public ContentStatus Status { get; internal set; }
        public string Note { get; internal set; }
        public IReadOnlyList<SceneDef> Scenes { get; internal set; } = Array.Empty<SceneDef>();
        public IReadOnlyList<StoryObject> Objects { get; internal set; } = Array.Empty<StoryObject>();
        public IReadOnlyList<Claim> Claims { get; internal set; } = Array.Empty<Claim>();
        /// <summary>The fire's welcome at the hearth before the visitor begins.</summary>
        public IReadOnlyList<Line> Welcome { get; internal set; } = Array.Empty<Line>();
        public IReadOnlyList<Beat> Beats { get; internal set; } = Array.Empty<Beat>();
        /// <summary>Spoken at the hearth when the visitor leaves the story before its ending.</summary>
        public IReadOnlyList<Line> EarlyReturn { get; internal set; } = Array.Empty<Line>();
        /// <summary>Object the visitor carries back to the hearth, if any.</summary>
        public string Keepsake { get; internal set; }
    }

    public sealed class SceneDef
    {
        public string Id { get; internal set; }
        public SceneKind Kind { get; internal set; }
    }

    /// <summary>A staged object with the actions it supports. <see cref="Continues"/> names the object it becomes across a transition.</summary>
    public sealed class StoryObject
    {
        public string Id { get; internal set; }
        public string Scene { get; internal set; }
        public IReadOnlyList<ActionKind> Capabilities { get; internal set; } = Array.Empty<ActionKind>();
        /// <summary>Id of an object in another scene that this one continues (an ember becoming a lantern), or null.</summary>
        public string Continues { get; internal set; }
    }

    public sealed class Claim
    {
        public string Id { get; internal set; }
        public EvidenceState State { get; internal set; }
        public string Text { get; internal set; }
        public IReadOnlyList<string> Sources { get; internal set; } = Array.Empty<string>();
    }

    public sealed class Beat
    {
        public string Id { get; internal set; }
        public string Scene { get; internal set; }
        public IReadOnlyList<Line> Lines { get; internal set; } = Array.Empty<Line>();
        public Invitation Invitation { get; internal set; }
    }

    /// <summary>One spoken line with its caption, timing fallback, fire cue, actions and evidence links.</summary>
    public sealed class Line
    {
        public string Id { get; internal set; }
        public string Text { get; internal set; }
        /// <summary>Narration clip key, or null when the line is caption-only.</summary>
        public string Audio { get; internal set; }
        /// <summary>Caption duration without audio, and the basis of the playback watchdog.</summary>
        public double Seconds { get; internal set; }
        /// <summary>Silence held after the line before the story moves on.</summary>
        public double PauseAfter { get; internal set; }
        public FireState Fire { get; internal set; } = FireState.Telling;
        /// <summary>Plays only when an earlier invitation had this outcome; null plays always.</summary>
        public Condition When { get; internal set; }
        public IReadOnlyList<StoryAction> Actions { get; internal set; } = Array.Empty<StoryAction>();
        public IReadOnlyList<string> Claims { get; internal set; } = Array.Empty<string>();
    }

    public sealed class Condition
    {
        public Condition(string invitation, Outcome outcome)
        {
            Invitation = invitation;
            Outcome = outcome;
        }

        public string Invitation { get; }
        public Outcome Outcome { get; }

        public override string ToString() => Invitation + ":" + (Outcome == Outcome.Accepted ? "accepted" : "default");
    }

    public sealed class StoryAction
    {
        public StoryAction(ActionKind kind, string objectId)
        {
            Kind = kind;
            Object = objectId;
        }

        public ActionKind Kind { get; }
        public string Object { get; }

        public override string ToString() => Kind + " " + Object;
    }

    /// <summary>
    /// An optional, consequential hand action. The visitor can accept while the offer is open; otherwise the
    /// authored default path plays after <see cref="WaitSeconds"/>. <see cref="Alternative"/> labels the nearby
    /// select control that does the same as the gesture.
    /// </summary>
    public sealed class Invitation
    {
        public string Id { get; internal set; }
        public InvitationKind Kind { get; internal set; }
        public string Object { get; internal set; }
        public string Alternative { get; internal set; }
        public double WaitSeconds { get; internal set; }
        public IReadOnlyList<Line> Accepted { get; internal set; } = Array.Empty<Line>();
        public IReadOnlyList<Line> Default { get; internal set; } = Array.Empty<Line>();
    }
}
