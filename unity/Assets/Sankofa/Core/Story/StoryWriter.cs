using System.Collections.Generic;
using System.Linq;
using Sankofa.Core.Json;

namespace Sankofa.Core.Story
{
    /// <summary>
    /// Writes a realised plan in the format <see cref="StoryReader.ReadPlan(JsonObject)"/> reads, so a kept visit
    /// carries exactly the content it played. Optional fields are omitted when empty; field order is stable.
    /// </summary>
    public static class StoryWriter
    {
        public static string PlanJson(StoryPlan plan) => JsonText.Serialize(Plan(plan));

        public static JsonObject Plan(StoryPlan plan)
        {
            var o = new JsonObject()
                .Add("schema", StoryReader.SupportedSchema)
                .Add("kind", "plan")
                .Add("package", plan.Package)
                .Add("version", plan.Version)
                .Add("route", plan.Route)
                .Add("label", plan.Label);
            Optional(o, "parent", plan.Parent);
            Optional(o, "next", plan.Next);
            o.Add("title", plan.Title)
                .Add("locale", plan.Locale)
                .Add("status", StoryReader.Name(plan.Status, StoryReader.ContentStatuses));
            Optional(o, "note", plan.Note);
            o.Add("scenes", Items(plan.Scenes.Select(s => new JsonObject().Add("id", s.Id).Add("kind", StoryReader.Name(s.Kind, StoryReader.SceneKinds)))));
            o.Add("objects", Items(plan.Objects.Select(Object)));
            if (plan.Claims.Count > 0) o.Add("claims", Items(plan.Claims.Select(Claim)));
            o.Add("beats", Items(plan.Beats.Select(Beat)));
            if (plan.EarlyReturn.Count > 0) o.Add("early_return", Lines(plan.EarlyReturn));
            Optional(o, "keepsake", plan.Keepsake);
            return o;
        }

        static JsonObject Object(StoryObject s)
        {
            var o = new JsonObject()
                .Add("id", s.Id)
                .Add("scene", s.Scene)
                .Add("capabilities", Items(s.Capabilities.Select(c => StoryReader.Name(c, StoryReader.ActionKinds))));
            Optional(o, "continues", s.Continues);
            return o;
        }

        static JsonObject Claim(Claim c)
        {
            var o = new JsonObject()
                .Add("id", c.Id)
                .Add("state", StoryReader.Name(c.State, StoryReader.EvidenceStates))
                .Add("text", c.Text);
            if (c.Sources.Count > 0) o.Add("sources", Items(c.Sources));
            return o;
        }

        static JsonObject Beat(Beat b)
        {
            var o = new JsonObject()
                .Add("id", b.Id)
                .Add("scene", b.Scene)
                .Add("lines", Lines(b.Lines));
            if (b.Invitation != null) o.Add("invitation", Invitation(b.Invitation));
            if (b.Questions.Count > 0) o.Add("questions", Items(b.Questions.Select(Question)));
            return o;
        }

        static JsonObject Invitation(Invitation i) => new JsonObject()
            .Add("id", i.Id)
            .Add("kind", StoryReader.Name(i.Kind, StoryReader.InvitationKinds))
            .Add("object", i.Object)
            .Add("alternative", i.Alternative)
            .Add("wait_seconds", i.WaitSeconds)
            .Add("accepted", Lines(i.Accepted))
            .Add("default", Lines(i.Default));

        static JsonObject Question(Question q)
        {
            var o = new JsonObject().Add("id", q.Id).Add("label", q.Label);
            Optional(o, "about", q.About);
            o.Add("lines", Lines(q.Lines));
            return o;
        }

        static List<object> Lines(IReadOnlyList<Line> lines) => Items(lines.Select(Line));

        static JsonObject Line(Line l)
        {
            var o = new JsonObject().Add("id", l.Id).Add("text", l.Text);
            Optional(o, "audio", l.Audio);
            o.Add("seconds", l.Seconds);
            if (l.PauseAfter > 0) o.Add("pause_after", l.PauseAfter);
            o.Add("fire", StoryReader.Name(l.Fire, StoryReader.FireStates));
            if (l.When != null) o.Add("when", l.When.ToString());
            if (l.Actions.Count > 0)
                o.Add("actions", Items(l.Actions.Select(a =>
                {
                    var action = new JsonObject().Add("do", StoryReader.Name(a.Kind, StoryReader.ActionKinds)).Add("object", a.Object);
                    Optional(action, "at", a.At);
                    return action;
                })));
            if (l.Claims.Count > 0) o.Add("claims", Items(l.Claims));
            return o;
        }

        /// <summary>Arrays as the reader produces them, so a written plan can be read back without reparsing.</summary>
        static List<object> Items<T>(IEnumerable<T> items) => items.Cast<object>().ToList();

        static void Optional(JsonObject o, string key, string value)
        {
            if (value != null) o.Add(key, value);
        }
    }
}
