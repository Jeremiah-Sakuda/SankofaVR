using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Sankofa.Core.Story;
using Sankofa.Flow;
using static Sankofa.Tests.Fixtures;

namespace Sankofa.Tests
{
    public class LivingGalleryStoryTests
    {
        static PlayablePackage Package() => Playable(StoryReader.ReadPackage(File.ReadAllText(RepositoryFile("unity/Assets/Sankofa/Content/living-gallery.json"))));
        static readonly string[] Invitations = { "receive-ember", "touch-water", "receive-flour", "offer-flour", "receive-milk", "receive-bead" };

        [Test]
        public void FirstHandInvitationPrecedesContextButDisclosurePrecedesSpatialEntry()
        {
            var p=Package().Compose("era.1850s");
            Assert.That(p.Beats[0].Lines.Select(l=>l.Id),Is.EqualTo(new[]{"gallery.sit"}));
            Assert.That(p.Beats[1].Invitation.Id,Is.EqualTo("receive-ember"));
            Assert.That(p.Beats[0].Lines.Concat(p.Beats[1].Lines).Sum(l=>l.Seconds+l.PauseAfter),Is.LessThan(24));
            Assert.That(p.Beats[2].Scene,Is.EqualTo("hearth"));
            Assert.That(p.Beats[2].Lines.Select(l=>l.Id),Does.Contain("e1.gather"));
            Assert.That(p.Beats[3].Scene,Is.EqualTo("story.water.1850s"));
        }

        [TestCase("era.1850s",true)][TestCase("era.1850s",false)]
        [TestCase("era.1850s.remembered",true)][TestCase("era.1850s.remembered",false)]
        public void ReturningTellingRequiresKeptVisitAndReplaysItsShortRealisedPlan(string route,bool receiveBead)
        {
            var package=Package();var store=new MemoryStore();var flow=new VisitFlow(package,store:store);var view=new FakePresentation(flow);
            flow.Start(true);Assert.That(flow.ChooseReturning(route),Is.False);
            flow.Choose(route=="era.1850s"?"era.1850s.remembered":"era.1850s");Finish(flow,view,new string[0]);
            flow.Keep();flow.BeginAgain();Assert.That(flow.ChooseReturning(route),Is.True);
            var realised=flow.Plan.Plan;
            Assert.That(realised.Beats.Count,Is.LessThan(package.Compose(route).Beats.Count));
            Assert.That(realised.Beats.SelectMany(b=>b.Lines).Any(l=>l.Id.StartsWith(route=="era.1850s"?"gallery.portions.":"gallery.waiting.")),Is.True);
            Finish(flow,view,receiveBead?new[]{"receive-bead"}:new string[0]);
            Assert.That(flow.Journal.Heard,Does.Contain("e1.rest").And.Contain("e1.kin"));
            Assert.That(flow.Journal.Outcomes.ContainsKey("touch-water"),Is.False);
            flow.Keep();flow.BeginAgain();Assert.That(flow.Replay(),Is.True);
            Assert.That(flow.Plan.Plan.Beats.Select(b=>b.Id),Is.EqualTo(realised.Beats.Select(b=>b.Id)));
            Finish(flow,view,new string[0]);Assert.That(flow.Journal.Outcomes["receive-bead"],Is.EqualTo(receiveBead?Outcome.Accepted:Outcome.Default));
            flow.BeginAgain();Assert.That(flow.Follow(),Is.True);Finish(flow,view,new string[0]);
            Assert.That(flow.Journal.Heard,Does.Contain(receiveBead?"again.bead.held":"again.bead.left"));
            Assert.That(flow.Journal.Heard,Does.Not.Contain("again.trade.helped").And.Not.Contain("again.trade.quiet"),"the shortened telling never recalls skipped invitations");
        }

        [Test]
        public void InvalidShortTellingCannotBypassPackageValidation()
        {
            var p=Package().Package;p.Routes[0].ReturningBeats=new[]{"home.1850s"};
            Assert.That(PackageValidator.Validate(p).IsValid,Is.False);
        }

        [TestCase("era.1850s")][TestCase("era.1850s.remembered")]
        public void PassiveGuestReachesTheAgreementAndHospitalityWithinTheFirstFiveMinutes(string route)
        {
            var plan=Package().Compose(route);var elapsed=0.0;var agreement=-1.0;var hospitality=-1.0;
            foreach(var beat in plan.Beats)
            {
                foreach(var line in beat.Lines)
                {
                    if(line.When!=null&&line.When.Outcome!=Outcome.Default)continue;
                    if(line.Id=="e1.rest")agreement=elapsed;
                    if(line.Id=="e1.witness.quiet")hospitality=elapsed;
                    elapsed+=line.Seconds+line.PauseAfter;
                }
                if(beat.Invitation!=null)elapsed+=beat.Invitation.WaitSeconds+beat.Invitation.Default.Sum(l=>l.Seconds+l.PauseAfter);
            }
            Assert.That(agreement,Is.InRange(1.0,260.0));Assert.That(hospitality,Is.InRange(1.0,280.0));
            Assert.That(plan.Beats.SelectMany(b=>b.Lines).Select(l=>l.Id),Does.Contain("e1.paths"));
            Assert.That(plan.Beats.First(b=>b.Id=="gallery.shared-landscape").Lines[0].Claims,Does.Contain("R1"));
            Assert.That(plan.Beats.Select(b=>b.Id).ToList().IndexOf("gallery.shared-landscape"),Is.GreaterThan(plan.Beats.Select(b=>b.Id).ToList().IndexOf("e1.after")));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void EntryRetainsWhetherAPerspectiveWasChosenOrDefaulted(bool useDefault)
        {
            var store = new MemoryStore();
            var flow = new VisitFlow(Package(), new FlowSettings { SingleStoryWait = 0 }, store);
            var view = new FakePresentation(flow);flow.Start(true);
            Assert.That(flow.BeginDefault("today.from-1850s"), Is.False, "default entry cannot bypass continuation eligibility");
            Assert.That(useDefault ? flow.BeginDefault("era.1850s") : flow.Choose("era.1850s"), Is.True);
            var selection = useDefault ? PerspectiveSelection.Default : PerspectiveSelection.Explicit;
            Assert.That(flow.Journal.Selection, Is.EqualTo(selection));
            Finish(flow, view, new string[0]);Assert.That(flow.Keep(), Is.True);
            var roundTrip = Core.Visits.SavedVisitCodec.Read(Core.Visits.SavedVisitCodec.Write(flow.KeptVisit));
            Assert.That(roundTrip.Selection, Is.EqualTo(selection));
            flow.BeginAgain();Assert.That(flow.Replay(), Is.True);
            Assert.That(flow.Journal.Selection, Is.EqualTo(selection));
            Assert.That(flow.BeginDefault("era.1850s"), Is.False, "entry cannot replace a playing visit");
        }
        static void Finish(VisitFlow flow, FakePresentation view, IEnumerable<string> accepted)
        {
            var ids = new HashSet<string>(accepted);
            view.RunUntil(() => {
                if(flow.OpenInvitation != null && ids.Remove(flow.OpenInvitation.Id)) Assert.That(flow.Accept(flow.OpenInvitation.Id), Is.True);
                return flow.Phase == VisitPhase.Closing && flow.CurrentLine == null;
            });
        }
        [TestCase("era.1850s")]
        [TestCase("era.1850s.remembered")]
        public void GalleryWaitsForEntryAndEachThreadHasItsOwnRevelation(string route)
        {
            var flow = new VisitFlow(Package(), new FlowSettings { SingleStoryWait = 0 }, new MemoryStore());
            var view = new FakePresentation(flow);flow.Start(true);view.Advance(120);
            Assert.That(flow.Phase, Is.EqualTo(VisitPhase.Choosing));
            Assert.That(flow.Journal, Is.Null);
            Assert.That(flow.Choose(route), Is.True);Finish(flow,view,new string[0]);
            Assert.That(flow.Journal.Heard.Contains("gallery.portions.enter"),Is.EqualTo(route=="era.1850s"));
            Assert.That(flow.Journal.Heard.Contains("gallery.waiting.enter"),Is.EqualTo(route!="era.1850s"));
            Assert.That(flow.Journal.Heard, Does.Contain("e1.rest"));
            Assert.That(flow.Journal.Heard.Contains("e1.remember"),Is.EqualTo(route=="era.1850s.remembered"),"the earlier favour is revealed in Naserian's perspective");
            Assert.That(flow.Journal.Outcomes.Values, Is.All.EqualTo(Outcome.Default));
        }
        static IEnumerable<TestCaseData> Paths => new[]{"era.1850s","era.1850s.remembered"}.SelectMany(route=>Enumerable.Range(0,64).Select(mask=>new TestCaseData(route,mask)));
        [TestCaseSource(nameof(Paths))]
        public void EveryLegalPathIncludesTheGuestAndKeepsTheAgreementIndependent(string route,int mask)
        {
            var accepted=Invitations.Where((id,i)=>(mask&(1<<i))!=0).ToArray();
            var store=new MemoryStore();var flow=new VisitFlow(Package(),store:store);var view=new FakePresentation(flow);
            flow.Start(true);flow.Choose(route);Finish(flow,view,accepted);
            Assert.That(flow.Journal.Completed,Is.True);
            Assert.That(flow.Journal.Heard,Does.Contain("e1.rest").And.Contain("e1.kin"));
            Assert.That(flow.Journal.Outcomes.Keys,Is.EquivalentTo(Invitations));
            Assert.That(flow.Journal.Heard.Contains("e1.witness.helped"),Is.EqualTo(accepted.Contains("offer-flour")));
            Assert.That(flow.Journal.Heard.Contains("gallery.milk.held"),Is.EqualTo(accepted.Contains("receive-milk")));
            Assert.That(flow.Keep(),Is.True);flow.BeginAgain();Assert.That(flow.Follow(),Is.True);
            Assert.That(flow.Plan.Plan.Parent,Is.EqualTo(flow.KeptVisit.Id));
            Assert.That(flow.Plan.Plan.Route,Is.EqualTo(route=="era.1850s"?"today.from-1850s":"today.from-remembered"));
            Finish(flow,view,new string[0]);
            Assert.That(flow.Journal.Heard.Contains("again.trade.helped"),Is.EqualTo(accepted.Contains("offer-flour")));
        }
        [Test]
        public void TheEarlierIllustratedVisitStillReplaysWithoutReplacingItsPlan()
        {
            var original=Playable(StoryReader.ReadPackage(File.ReadAllText(RepositoryFile("unity/Assets/Sankofa/Content/illustrated-theatre-mvp.json"))));
            var store=new MemoryStore();var old=new VisitFlow(original,store:store);var oldView=new FakePresentation(old);
            old.Start(true);old.Choose("era.1850s");Finish(old,oldView,new[]{"receive-flour"});Assert.That(old.Keep(),Is.True);
            var next=new VisitFlow(Package(),store:store);var view=new FakePresentation(next);next.Start(true);
            Assert.That(next.Replay(),Is.True);Assert.That(next.Plan.Plan.Version,Is.EqualTo("0.3.0"));
            Finish(next,view,new string[0]);
            Assert.That(next.Journal.Outcomes["receive-flour"],Is.EqualTo(Outcome.Accepted));
            Assert.That(next.Journal.Outcomes.ContainsKey("receive-milk"),Is.False,"new content is never inserted into old replay");
        }

        [Test]
        public void EveryPlayableGalleryLineHasAnExistingRecordedClip()
        {
            var p=Package().Package;
            var lines=p.Beats.SelectMany(b=>b.Lines.Concat(b.Invitation!=null?b.Invitation.Accepted.Concat(b.Invitation.Default):new Line[0]).Concat(b.Questions.SelectMany(q=>q.Lines)));
            foreach(var line in lines)
            {
                Assert.That(line.Audio,Is.Not.Null,line.Id);
                Assert.That(File.Exists(RepositoryFile("unity/Assets/Sankofa/Resources/Narration/"+line.Audio+".mp3")),Is.True,line.Id);
            }
        }

        [Test]
        public void ConditionalHospitalityCannotLeaveAnUnpresentedOutcome()
        {
            var package=StoryReader.ReadPackage(File.ReadAllText(RepositoryFile("unity/Assets/Sankofa/Content/living-gallery.json")));
            var plan=PlanComposer.Compose(package,"era.1850s");
            var beat=plan.Beats.Single(b=>b.Id=="e1.after");
            beat.Lines=beat.Lines.Where(l=>l.When.Outcome==Outcome.Accepted).ToArray();
            Assert.That(PlanValidator.Validate(plan).Errors.Select(e=>e.Code),Does.Contain("invitation.offer"));
        }

        [Test]
        public void QuestionsAboutEarlierWaitingCannotSpoilTheAgreement()
        {
            var p=Package().Package;
            Assert.That(p.Beats.Single(b=>b.Id=="e1.naserian").Questions.Select(q=>q.Id),Does.Not.Contain("q.e1.friends"));
            foreach(var r in p.Routes.Where(r=>!r.IsContinuation))
                Assert.That(r.Beats.IndexOf("e1.after"),Is.GreaterThan(r.Beats.IndexOf("e1.turn")));
        }

        [TestCase("era.1850s", "e1.portions")]
        [TestCase("era.1850s.remembered", "e1.waiting")]
        public void FocusedReturnReachesItsMemoryWithinThirtySecondsAndSpendsTimeOnNewMaterial(string route,string memory)
        {
            var p=Package().Package;var r=p.Routes.Single(x=>x.Id==route);
            double Duration(Beat b)=>b.Lines.Sum(l=>l.Seconds+l.PauseAfter)+(b.Invitation==null?0:b.Invitation.WaitSeconds+b.Invitation.Default.Sum(l=>l.Seconds+l.PauseAfter));
            var beats=r.ReturningBeats.Select(id=>p.Beats.Single(b=>b.Id==id)).ToArray();
            var before=beats.TakeWhile(b=>b.Id!=memory).Sum(Duration);var total=beats.Sum(Duration);
            Assert.That(before,Is.LessThan(30));Assert.That(total,Is.LessThan(140));
            Assert.That(Duration(beats.Single(b=>b.Id==memory))/total,Is.GreaterThan(0.28));
            Assert.That(beats.First().Scene,Is.EqualTo("hearth"));
            Assert.That(beats.First().Lines.Single().Text,Does.Contain("imagined"),"return visits still disclose reconstruction before entry");
        }

        [Test]
        public void DefaultPerspectiveDoesNotExplainTheEarlierReversalBeforeTheOtherMemory()
        {
            var p=Package();var first=p.Compose("era.1850s");var other=p.Compose("era.1850s.remembered");
            Assert.That(first.Beats.SelectMany(b=>b.Lines).Select(l=>l.Id),Does.Not.Contain("e1.remember").And.Not.Contain("gallery.waiting.short"));
            var lines=other.Beats.SelectMany(b=>b.Lines).Select(l=>l.Id).ToList();
            Assert.That(lines.IndexOf("e1.remember"),Is.GreaterThan(lines.IndexOf("gallery.waiting.return")));
        }
    }
    static class GalleryListExtensions
    {
        public static int IndexOf<T>(this IReadOnlyList<T> list,T item) { for(var i=0;i<list.Count;i++)if(EqualityComparer<T>.Default.Equals(list[i],item))return i;return -1; }
    }
}
