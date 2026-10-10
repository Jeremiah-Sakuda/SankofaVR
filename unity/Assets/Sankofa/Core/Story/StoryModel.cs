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

    /// <summary>
    /// The bounded set of things a plan may do with a staged object. Reveal, illuminate, offer, place and withdraw
    /// are story actions on lines; touch is a capability only: the visitor may be invited to touch the object.
    /// </summary>
    /// <summary>
    /// What a line does on the stage. Move sends a figure to stand or kneel where another object is used (or home,
    /// without a target); it changes what the visitor sees, never what they have.
    /// </summary>
    public enum ActionKind { Reveal, Illuminate, Offer, Place, Withdraw, Touch, Move }

    /// <summary>
    /// A deliberate physical invitation. Receive: an open palm (or a nearby select) takes the offered object.
    /// Touch: the visitor reaches out to a visible object within reach (or uses the nearby select).
    /// </summary>
    public enum InvitationKind { Receive, Touch }

    /// <summary>Accepted is the visitor's explicit action. Default is the authored passive path, never presented as a choice.</summary>
    public enum Outcome { Accepted, Default }

    /// <summary>Review status of a content package. Only reviewed content may be presented as release content.</summary>
    public enum ContentStatus { Placeholder, Draft, Reviewed }

    /// <summary>
    /// A content package: the scenes and objects the stage can show, the fire's lines at the hearth, a pool of
    /// beats and the routes that select from it. A route is either a first story chosen by interest or a
    /// continuation that follows a kept visit. Plans are composed from it by <see cref="PlanComposer"/>; the
    /// package is usable only after <see cref="PackageValidator"/> has composed and checked every route.
    /// </summary>
    public sealed class StoryPackage
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
        /// <summary>The fire's welcome on a first visit, before the visitor chooses.</summary>
        public IReadOnlyList<Line> Welcome { get; internal set; } = Array.Empty<Line>();
        /// <summary>The welcome when a kept visit is waiting at the hearth. Never recalls anything that was not kept.</summary>
        public IReadOnlyList<Line> WelcomeBack { get; internal set; } = Array.Empty<Line>();
        /// <summary>Spoken when the visitor is choosing again (after a story, or a new story instead of a kept thread).</summary>
        public IReadOnlyList<Line> Choose { get; internal set; } = Array.Empty<Line>();
        public IReadOnlyList<Route> Routes { get; internal set; } = Array.Empty<Route>();
        public IReadOnlyList<Beat> Beats { get; internal set; } = Array.Empty<Beat>();
        public IReadOnlyList<Line> EarlyReturn { get; internal set; } = Array.Empty<Line>();
    }

    /// <summary>
    /// A selection of beats from the package. <see cref="Follows"/> names the route a continuation follows; such
    /// a route is offered only to a visitor who kept a visit of that route. <see cref="Next"/> names the
    /// continuation offered after this route.
    /// </summary>
    public sealed class Route
    {
        public string Id { get; internal set; }
        /// <summary>What the visitor chooses, as presented on the control.</summary>
        public string Label { get; internal set; }
        public string Follows { get; internal set; }
        public IReadOnlyList<string> Beats { get; internal set; } = Array.Empty<string>();
        /// <summary>Hearth object the visitor brings home, if any.</summary>
        public string Keepsake { get; internal set; }
        public string Next { get; internal set; }
        public bool IsContinuation => Follows != null;
    }

    /// <summary>
    /// A realised story plan: exactly what one visit plays, composed from a package for one route, with lines
    /// that recall a kept visit already resolved. Playable only after <see cref="PlanValidator"/> accepts it, and
    /// saved as-is so a replay never regenerates its content.
    /// </summary>
    public sealed class StoryPlan
    {
        public int Schema { get; internal set; }
        /// <summary>Package the plan was composed from, and its version.</summary>
        public string Package { get; internal set; }
        public string Version { get; internal set; }
        public string Route { get; internal set; }
        public string Label { get; internal set; }
        /// <summary>The kept visit this plan continues, or null for a first story.</summary>
        public string Parent { get; internal set; }
        /// <summary>The continuation route offered after this plan, or null.</summary>
        public string Next { get; internal set; }
        public string Title { get; internal set; }
        public string Locale { get; internal set; }
        public ContentStatus Status { get; internal set; }
        public string Note { get; internal set; }
        public IReadOnlyList<SceneDef> Scenes { get; internal set; } = Array.Empty<SceneDef>();
        public IReadOnlyList<StoryObject> Objects { get; internal set; } = Array.Empty<StoryObject>();
        public IReadOnlyList<Claim> Claims { get; internal set; } = Array.Empty<Claim>();
        public IReadOnlyList<Beat> Beats { get; internal set; } = Array.Empty<Beat>();
        /// <summary>Spoken at the hearth when the visitor leaves the story before its ending.</summary>
        public IReadOnlyList<Line> EarlyReturn { get; internal set; } = Array.Empty<Line>();
        /// <summary>Hearth object the visitor carries home, if any.</summary>
        public string Keepsake { get; internal set; }
    }

    public sealed class SceneDef
    {
        public string Id { get; internal set; }
        public SceneKind Kind { get; internal set; }
    }

    /// <summary>
    /// A staged object with the actions it supports. <see cref="Continues"/> names the object in another scene
    /// that this one corresponds to: an ember becomes a lantern when the visitor enters the lantern's scene, and the
    /// lantern becomes the ember again on the way back. Only something the visitor has with them crosses over.
    /// </summary>
    public sealed class StoryObject
    {
        public string Id { get; internal set; }
        public string Scene { get; internal set; }
        public IReadOnlyList<ActionKind> Capabilities { get; internal set; } = Array.Empty<ActionKind>();
        public string Continues { get; internal set; }
        /// <summary>How the story names it to the visitor ("the ewe", "Naserian"), or null.</summary>
        public string Name { get; internal set; }
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
        /// <summary>Contextual questions the visitor may ask by hand while this beat is current.</summary>
        public IReadOnlyList<Question> Questions { get; internal set; } = Array.Empty<Question>();
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
        /// <summary>Plays only when an earlier invitation of this visit had this outcome; null plays always.</summary>
        public Condition When { get; internal set; }
        /// <summary>
        /// Package lines only: kept in a continuation's plan only when the kept parent visit had this outcome.
        /// The composer resolves it, so a plan never recalls something that did not happen.
        /// </summary>
        public Condition Recall { get; internal set; }
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
        public StoryAction(ActionKind kind, string objectId, string at = null)
        {
            Kind = kind;
            Object = objectId;
            At = at;
        }

        public ActionKind Kind { get; }
        public string Object { get; }
        /// <summary>
        /// For <see cref="ActionKind.Place"/>: the visible object it is set on, or null for beside the visitor. For
        /// <see cref="ActionKind.Move"/>: the visible object the figure goes to, or null for its own place.
        /// </summary>
        public string At { get; }

        public override string ToString() => At == null ? Kind + " " + Object : $"{Kind} {Object} at {At}";
    }

    /// <summary>
    /// An optional, consequential hand action. The visitor can accept while the invitation is open; otherwise the
    /// authored default path plays after <see cref="WaitSeconds"/>. <see cref="Alternative"/> labels the nearby
    /// select control that does the same as the gesture.
    /// </summary>
    public sealed class Invitation
    {
        public string Id { get; internal set; }
        public InvitationKind Kind { get; internal set; }
        /// <summary>The object offered to the visitor (receive) or the object to touch (touch).</summary>
        public string Object { get; internal set; }
        public string Alternative { get; internal set; }
        public double WaitSeconds { get; internal set; }
        public IReadOnlyList<Line> Accepted { get; internal set; } = Array.Empty<Line>();
        public IReadOnlyList<Line> Default { get; internal set; } = Array.Empty<Line>();
    }

    /// <summary>
    /// A question the visitor can ask by hand. Asking interrupts the fire, which answers and then resumes the
    /// story from a safe boundary. <see cref="About"/> names the object it concerns, offered only while that
    /// object is visible.
    /// </summary>
    public sealed class Question
    {
        public string Id { get; internal set; }
        public string Label { get; internal set; }
        public string About { get; internal set; }
        public IReadOnlyList<Line> Lines { get; internal set; } = Array.Empty<Line>();
    }
}
