using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Sankofa.Core.Json;
using Sankofa.Core.Personal;

namespace Sankofa.Tests
{
    public class PersonalStoryTests
    {
        static PersonalStoryRequest Request(string interest = "journeys", string fragment = null, string question = null) =>
            new PersonalStoryRequest("enkare-nairobi", "1850s", interest, fragment, question);
        static PersonalStoryDocument Story(string interest = "journeys") => BundledPersonalStoryComposer.Compose(Request(interest)).Story;
        static string[] Codes(PersonalStoryValidation report) => report.Issues.Select(i => i.Code).ToArray();

        [Test]
        public void ThreeInterestsChangeOpeningContextIllustrationAndQuestionsBeforeEnding()
        {
            var stories = PersonalStoryCatalog.Interests.Select(Story).ToArray();
            Assert.That(stories.Select(s => s.Chapters[0].Narration).Distinct().Count(), Is.EqualTo(3));
            // The journey and the exchange share the documented trade that links their two communities; the water has its name.
            Assert.That(stories.Select(s => s.Chapters[1].Narration).Distinct().Count(), Is.EqualTo(2));
            Assert.That(stories.Select(s => s.Chapters[1].Questions[0].Label).Distinct().Count(), Is.EqualTo(3));
            Assert.That(stories.Select(s => s.Chapters[0].Questions[0].Label).Distinct().Count(), Is.EqualTo(3));
            Assert.That(stories.Select(s => s.Chapters[1].Illustration).Distinct().Count(), Is.GreaterThan(1));
            foreach (var story in stories)
            {
                Assert.That(story.Chapters, Has.Count.EqualTo(4), "three pages on either route, with two perspectives to follow");
                Assert.That(PersonalStoryRoutes.RemainingPages(story, 0), Is.EqualTo(3));
                Assert.That(PersonalStoryValidator.Validate(story).IsValid, Is.True, PersonalStoryValidator.Validate(story).ToString());
                var endings = Enumerable.Range(0, story.Chapters.Count).Where(i => PersonalStoryRoutes.Continue(story, i) < 0).ToArray();
                Assert.That(endings, Has.Length.EqualTo(2));
                foreach (var ending in endings) Assert.That(story.Chapters[ending].Narration, Does.Contain("choose").Or.Contain("choice"));
                Assert.That(story.Mode, Is.EqualTo("bundled"));
            }
        }

        [Test]
        public void EachTellingOffersTwoSidesWhoseEndingsDiffer()
        {
            foreach (var interest in PersonalStoryCatalog.Interests)
            {
                var story = Story(interest);
                var choice = story.Chapters.Single(PersonalStoryRoutes.HasChoices);
                Assert.That(choice.Truth, Is.EqualTo("historical"), "the documented page is where the visitor decides whose side to follow");
                Assert.That(choice.Choices, Has.Count.EqualTo(2));
                Assert.That(choice.Choices.Select(c => c.Label).Distinct().Count(), Is.EqualTo(2));
                var endings = choice.Choices.Select(c => story.Chapters[PersonalStoryRoutes.IndexOf(story, c.Next)]).ToArray();
                Assert.That(endings[0].Narration, Is.Not.EqualTo(endings[1].Narration));
                Assert.That(endings.All(e => e.Truth == "reconstructed" && e.Questions.Count > 0), Is.True);
                Assert.That(PersonalStoryRoutes.Continue(story, PersonalStoryRoutes.IndexOf(story, endings[1].Id)), Is.EqualTo(-1));
            }
        }

        [Test]
        public void LinearTellingsKeepTheirEarlierJsonForm()
        {
            var story = Story();
            story.Chapters = story.Chapters.Take(3).ToArray();
            story.Chapters[1].Choices = new PersonalStoryChoice[0];
            Assert.That(PersonalStoryValidator.Validate(story).IsValid, Is.True, PersonalStoryValidator.Validate(story).ToString());
            var text = PersonalStoryJson.Write(story);
            Assert.That(text, Does.Not.Contain("\"choices\""), "provider tellings and earlier kept files have no choices field");
            Assert.That(PersonalStoryJson.Write(PersonalStoryJson.ReadValidated(text)), Is.EqualTo(text));
            Assert.That(PersonalStoryJson.Write(Story()), Does.Contain("\"choices\""));
        }

        [Test]
        public void ChoicesMustLeadForwardReachEveryChapterAndCompleteEachRoute()
        {
            PersonalStoryDocument Mutate(Action<PersonalStoryDocument> change) { var story = Story(); change(story); return story; }
            var cases = new (string Code, PersonalStoryDocument Story)[]
            {
                ("choices.count", Mutate(s => s.Chapters[1].Choices = s.Chapters[1].Choices.Take(1).ToArray())),
                ("choice.target", Mutate(s => s.Chapters[1].Choices[1].Next = s.Chapters[0].Id)),
                ("choice.target", Mutate(s => s.Chapters[1].Choices[1].Next = "missing.chapter")),
                ("choice.duplicate", Mutate(s => s.Chapters[1].Choices[1].Next = s.Chapters[1].Choices[0].Next)),
                ("id.duplicate", Mutate(s => s.Chapters[1].Choices[1].Id = s.Chapters[0].Id)),
                ("text.unsafe", Mutate(s => s.Chapters[1].Choices[0].Label = "<b>Follow</b>")),
                ("text.length", Mutate(s => s.Chapters[1].Choices[0].Label = new string('a', 81))),
                ("chapters.route", Mutate(s => s.Chapters[0].Choices = new[]
                {
                    new PersonalStoryChoice { Id = "early.a", Label = "One side", Next = s.Chapters[2].Id },
                    new PersonalStoryChoice { Id = "early.b", Label = "The other side", Next = s.Chapters[3].Id },
                })),
                ("chapters.unreachable", Mutate(s => s.Chapters = s.Chapters.Concat(new[]
                {
                    new PersonalStoryChapter
                    {
                        Id = "after.an.ending", Title = "Never reached", Narration = PersonalStoryCatalog.ReconstructionPrefix + "A page after both endings.",
                        Illustration = "scenery-atlas", Truth = "reconstructed",
                    },
                }).ToArray())),
            };
            foreach (var (code, story) in cases)
                Assert.That(Codes(PersonalStoryValidator.Validate(story)), Does.Contain(code), code);
        }

        [Test]
        public void ChoiceJsonIsStrictAndBounded()
        {
            var text = JsonText.Serialize(JsonText.ParseObject(PersonalStoryJson.Write(Story())), false);
            Assert.That(PersonalStoryJson.ReadValidated(text).Chapters[1].Choices, Has.Count.EqualTo(2));
            Assert.Throws<PersonalStoryFormatException>(() => PersonalStoryJson.Parse(text.Replace("\"next\":", "\"script\":\"x\",\"next\":")));
            var many = string.Join(",", Enumerable.Range(0, 4).Select(i => "{\"id\":\"c" + i + "\",\"label\":\"Side\",\"next\":\"x\"}"));
            var start = text.IndexOf("\"choices\":[", StringComparison.Ordinal);
            Assert.That(start, Is.GreaterThan(0));
            var end = text.IndexOf("]", start, StringComparison.Ordinal);
            Assert.Throws<PersonalStoryFormatException>(() => PersonalStoryJson.Parse(text.Substring(0, start) + "\"choices\":[" + many + text.Substring(end)));
        }

        [Test]
        public void TheReasonExplainsWhatTheVisitorsWordsChangedUsingOnlyCatalogVocabulary()
        {
            var question = BundledPersonalStoryComposer.Compose(Request("journeys", question: "My grandmother SampleFamily carried flour in a gourd. Why?"));
            Assert.That(question.Story.Interest, Is.EqualTo("trade-household"));
            Assert.That(question.Reason, Is.EqualTo("Your question mentioned \u201ccarried\u201d, \u201cflour\u201d and \u201cgourd\u201d, so the fire follows trade and household."));
            Assert.That(question.Reason, Does.Not.Contain("SampleFamily").And.Not.Contain("grandmother"));
            Assert.That(question.Cues, Is.EquivalentTo(new[] { "carried", "flour", "gourd" }));

            var fragment = BundledPersonalStoryComposer.Compose(Request("journeys", "Someone remembered the river by its name."));
            Assert.That(fragment.Story.Interest, Is.EqualTo("place-water"));
            Assert.That(fragment.Reason, Does.StartWith("Your fragment mentioned"));

            var unmatched = BundledPersonalStoryComposer.Compose(Request("place-water", question: "What songs did children sing?"));
            Assert.That(unmatched.Story.Interest, Is.EqualTo("place-water"));
            Assert.That(unmatched.Reason, Does.Contain("did not point to one prepared thread").And.Contain("place and water"));
            Assert.That(unmatched.Reason, Does.Not.Contain("songs"));
            Assert.That(unmatched.Cues, Is.Empty);

            Assert.That(BundledPersonalStoryComposer.Compose(Request("journeys")).Reason, Is.EqualTo("You chose journeys."));
            Assert.That(PersonalStoryJson.Write(question.Story), Does.Not.Contain("Your question mentioned"), "the reason is presentation, never kept story content");
        }

        [Test]
        public void TheQuestionAndFragmentChangeTheExplorationWithoutInventingBiography()
        {
            var journey = BundledPersonalStoryComposer.Compose(Request("trade-household", "A relative remembered moving along a path."));
            var water = BundledPersonalStoryComposer.Compose(Request("trade-household", "A relative remembered the river and its name."));
            Assert.That(journey.Story.Interest, Is.EqualTo("journeys"));
            Assert.That(water.Story.Interest, Is.EqualTo("place-water"));
            Assert.That(journey.Story.Chapters[0].Narration, Is.Not.EqualTo(water.Story.Chapters[0].Narration));
            var question = BundledPersonalStoryComposer.Compose(Request("journeys", "The river was remembered.", "What did a household carry in a gourd?"));
            Assert.That(question.Story.Interest, Is.EqualTo("trade-household"), "the deliberate question has priority");
            Assert.That(PersonalStoryJson.Write(question.Story), Does.Not.Contain("The river was remembered."));
            Assert.That(question.Message, Does.Contain("No fresh AI generation"));
        }

        [Test]
        public void NamesAndOriginsDoNotInferIdentityOrAncestry()
        {
            var one = Request("place-water", "A visitor with the invented surname SampleFamily asks for context.");
            var two = Request("place-water", "A visitor with another invented surname ExampleFamily asks for context.");
            var first = BundledPersonalStoryComposer.Compose(one).Story;
            var second = BundledPersonalStoryComposer.Compose(two).Story;
            Assert.That(PersonalStoryJson.Write(first), Is.EqualTo(PersonalStoryJson.Write(second)));
            Assert.That(PersonalStoryJson.Write(first), Does.Not.Contain("SampleFamily"));
            Assert.That(first.Chapters.Any(c => c.Truth == "personal-account"), Is.False);
        }

        [TestCase("Mombasa", "1850s", "coverage.region")]
        [TestCase("Kenya", "1850s", "coverage.region")]
        [TestCase("Nairobi and Ghana", "1850s", "coverage.region")]
        [TestCase("Nairobi", "1899", "coverage.period")]
        [TestCase("Nairobi", "today", "coverage.period")]
        public void UnsupportedCoverageReturnsNoSubstituteStory(string region, string period, string code)
        {
            var result = BundledPersonalStoryComposer.Compose(new PersonalStoryRequest(region, period, "journeys"));
            Assert.That(result.Success, Is.False);
            Assert.That(result.Story, Is.Null);
            Assert.That(result.Code, Is.EqualTo(code));
            Assert.That(result.Message, Is.Not.Empty);
        }

        [Test]
        public void ExplicitAliasesAndOptionalPeriodResolveToTheSameBoundedContext()
        {
            var aliases = new[] { "Nairobi", "Enkare Nairobi", "Nairobi, Kenya", " enkare-nairobi " };
            foreach (var region in aliases)
            {
                var result = BundledPersonalStoryComposer.Compose(new PersonalStoryRequest(region, null, "water"));
                Assert.That(result.Success, Is.True);
                Assert.That(result.Story.Region, Is.EqualTo("enkare-nairobi"));
                Assert.That(result.Story.Period, Is.EqualTo("1850s"));
                Assert.That(result.Story.Interest, Is.EqualTo("place-water"));
            }
            Assert.That(BundledPersonalStoryComposer.Compose(new PersonalStoryRequest("Nairobi", "1850s", "politics")).Code, Is.EqualTo("coverage.interest"));
        }

        [TestCase("<size=300>hello</size>")]
        [TestCase("[open](https://untrusted.invalid)")]
        [TestCase("javascript:alert(1)")]
        [TestCase("A control\u0001 character")]
        [TestCase("A hidden\u202e reversal")]
        public void UntrustedInputCannotBecomePresentationMarkup(string fragment)
        {
            var result = BundledPersonalStoryComposer.Compose(Request(fragment: fragment));
            Assert.That(result.Success, Is.False);
            Assert.That(result.Code, Is.EqualTo("input.invalid"));
        }

        [Test]
        public void EmptyAndOversizeInputsFailWithoutCrashing()
        {
            Assert.That(BundledPersonalStoryComposer.Compose(null).Code, Is.EqualTo("input.invalid"));
            Assert.That(BundledPersonalStoryComposer.Compose(new PersonalStoryRequest()).Code, Is.EqualTo("input.invalid"));
            Assert.That(BundledPersonalStoryComposer.Compose(Request(fragment: new string('x', 1001))).Code, Is.EqualTo("input.invalid"));
            Assert.That(BundledPersonalStoryComposer.Compose(Request(question: new string('x', 501))).Code, Is.EqualTo("input.invalid"));
            Assert.That(BundledPersonalStoryComposer.Compose(Request(fragment: new string('x', 1000))).Success, Is.True);
            Assert.That(BundledPersonalStoryComposer.Compose(Request(fragment: "A plain quotation: \"remember\"; an emoji \U0001f525")).Success, Is.True);
        }

        [Test]
        public void ExactReplaySurvivesWritingAndReadingWithAllSourcesAndAnswers()
        {
            foreach (var interest in PersonalStoryCatalog.Interests)
            {
                var text = PersonalStoryJson.Write(Story(interest));
                var read = PersonalStoryJson.ReadValidated(text);
                Assert.That(PersonalStoryJson.Write(read), Is.EqualTo(text));
                Assert.That(read.Chapters.All(c => c.Questions.Count > 0), Is.True);
                Assert.That(read.Chapters.SelectMany(c => c.Questions).Any(q => !string.IsNullOrWhiteSpace(q.Answer)), Is.True);
            }
        }

        [Test]
        public void ApprovedArtIdsResolveToExistingArtAndDoNotAcceptArbitraryUrls()
        {
            var paths = new Dictionary<string, string>
            {
                { "scenery-atlas", "scenery-atlas.png" }, { "cast-atlas", "cast-atlas.png" }, { "modern-river", "modern-river-v1.png" },
            };
            foreach (var id in PersonalStoryCatalog.Illustrations)
                Assert.That(File.Exists(Fixtures.RepositoryFile("unity/Assets/Sankofa/Art/IllustratedTheatre/" + paths[id])), Is.True, id);
            var story = Story();
            story.Chapters[0].Illustration = "https://untrusted.invalid/art.png";
            Assert.That(Codes(PersonalStoryValidator.Validate(story)), Does.Contain("asset.unapproved"));
        }

        [Test]
        public void CitationsCannotLaunderUnsupportedHistoricalOrCulturalClaims()
        {
            var story = Story();
            story.Chapters[1].Narration = "Every household observed this exact custom.";
            Assert.That(Codes(PersonalStoryValidator.Validate(story)), Does.Contain("history.unapproved"));
            story = Story();
            story.Chapters[1].Sources = new[] { "C1" };
            Assert.That(Codes(PersonalStoryValidator.Validate(story)), Does.Contain("history.unapproved"));
            story = Story();
            story.Chapters[1].Questions[0].Answer = "Your ancestor definitely traded here.";
            Assert.That(Codes(PersonalStoryValidator.Validate(story)), Does.Contain("history.unapproved"));
        }

        [Test]
        public void OnlyCanonicalSourceRecordsAndDeclaredReferencesAreAccepted()
        {
            var story = Story();
            story.Sources[0].Url = "https://untrusted.invalid/";
            Assert.That(Codes(PersonalStoryValidator.Validate(story)), Does.Contain("source.unapproved"));
            story = Story();
            story.Chapters[1].Sources = new[] { "UNKNOWN" };
            Assert.That(Codes(PersonalStoryValidator.Validate(story)), Does.Contain("source.reference"));
            story = Story();
            story.Sources = new[] { story.Sources[0], story.Sources[0] };
            Assert.That(Codes(PersonalStoryValidator.Validate(story)), Does.Contain("source.duplicate"));
            var source = PersonalStoryCatalog.Source("C1");
            source.Title = "modified by caller";
            Assert.That(PersonalStoryCatalog.Source("C1").Title, Does.Not.Contain("modified"), "catalog values are defensive copies");
        }

        [Test]
        public void ReconstructionAndUserAccountsKeepTheirEvidenceBoundaries()
        {
            var story = Story();
            story.Chapters[0].Narration = "An unlabeled scene.";
            Assert.That(Codes(PersonalStoryValidator.Validate(story)), Does.Contain("reconstruction.disclosure"));
            story.Chapters[0].Narration = PersonalStoryCatalog.ReconstructionPrefix + "Your ancestors lived here.";
            Assert.That(Codes(PersonalStoryValidator.Validate(story)), Does.Contain("identity.inferred"));
            var request = Request(fragment: "An invented visitor remembers a small blue cup.");
            story = Story();
            story.Chapters[0].Narration = PersonalStoryCatalog.AccountPrefix + request.Fragment;
            story.Chapters[0].Truth = "personal-account";
            Assert.That(Codes(PersonalStoryValidator.Validate(story)), Does.Contain("account.unverified"));
            Assert.That(PersonalStoryValidator.Validate(story, request).IsValid, Is.True);
            story.Chapters[0].Sources = new[] { "C6" };
            Assert.That(Codes(PersonalStoryValidator.Validate(story, request)), Does.Contain("account.sources"));
            story.Chapters[0].Narration += " An extra invented event.";
            Assert.That(Codes(PersonalStoryValidator.Validate(story, request)), Does.Contain("account.unverified"));
        }

        [TestCase("<link=secret>open</link>")]
        [TestCase("javascript:alert(1)")]
        public void ProviderProseCannotInjectMarkupUrlsOrBrokenUnicode(string body)
        {
            var story = Story();
            story.Mode = "generated";
            story.Chapters[0].Narration = PersonalStoryCatalog.ReconstructionPrefix + body;
            Assert.That(Codes(PersonalStoryValidator.Validate(story)), Does.Contain("text.unsafe"));
        }

        [Test]
        public void BrokenUnicodeCannotCrossProviderBoundary()
        {
            var story = Story();
            story.Chapters[0].Narration = PersonalStoryCatalog.ReconstructionPrefix + new string((char)0xd800, 1);
            Assert.That(Codes(PersonalStoryValidator.Validate(story)), Does.Contain("text.unsafe"));
        }

        [Test]
        public void ProviderStateShapeAndTextLimitsAreValidated()
        {
            var story = Story();
            story.Mode = "ai-verified";
            story.Chapters[0].Truth = "verified";
            story.Chapters[0].Questions[0].Id = story.Chapters[0].Id;
            story.Chapters[0].Narration = new string('x', 2001);
            var codes = Codes(PersonalStoryValidator.Validate(story));
            Assert.That(codes, Does.Contain("mode.invalid").And.Contain("truth.invalid").And.Contain("id.duplicate").And.Contain("text.length"));
            story = Story();
            story.Chapters[1].Illustration = "modern-river";
            Assert.That(Codes(PersonalStoryValidator.Validate(story)), Does.Contain("asset.period"));
            story.Chapters = new PersonalStoryChapter[] { null };
            Assert.That(Codes(PersonalStoryValidator.Validate(story)), Does.Contain("chapters.count").And.Contain("chapter.missing"));
        }

        [TestCase("schema", "sankofa.personal-story.v999", "$.schema")]
        [TestCase("mode", 5, "$.mode")]
        [TestCase("surprise", "do something", "$.surprise")]
        public void JsonBoundaryRejectsUnknownSchemaFieldsAndWrongTypes(string key, object value, string expectedPath)
        {
            var root = JsonText.ParseObject(PersonalStoryJson.Write(Story()));
            root.Add(key, value);
            Assert.That(Assert.Throws<PersonalStoryFormatException>(() => PersonalStoryJson.Parse(JsonText.Serialize(root))).Path, Is.EqualTo(expectedPath));
        }

        [Test]
        public void JsonBoundaryRejectsDuplicateFieldsDeepNestingOversizeAndWrongArrayValues()
        {
            Assert.Throws<PersonalStoryFormatException>(() => PersonalStoryJson.Parse("{\"schema\":\"a\",\"schema\":\"b\"}"));
            Assert.Throws<PersonalStoryFormatException>(() => PersonalStoryJson.Parse(new string('[', 10000) + new string(']', 10000)));
            Assert.Throws<PersonalStoryFormatException>(() => PersonalStoryJson.Parse(new string(' ', 48001)));
            Assert.Throws<PersonalStoryFormatException>(() => PersonalStoryJson.Parse(null));
            var root = JsonText.ParseObject(PersonalStoryJson.Write(Story()));
            ((JsonObject)root.GetArray("chapters")[0]).Add("sources", new object[] { false });
            Assert.That(Assert.Throws<PersonalStoryFormatException>(() => PersonalStoryJson.Parse(JsonText.Serialize(root))).Path, Is.EqualTo("$.chapters[0].sources[0]"));
            root = JsonText.ParseObject(PersonalStoryJson.Write(Story()));
            ((JsonObject)root.GetArray("chapters")[0]).Add("actions", new object[0]);
            Assert.That(Assert.Throws<PersonalStoryFormatException>(() => PersonalStoryJson.Parse(JsonText.Serialize(root))).Path, Is.EqualTo("$.chapters[0].actions"));
        }

        [Test]
        public void SavedOrProviderStoryIsNeverMadePlayableByParsingAlone()
        {
            var story = Story();
            story.Chapters[1].Narration = "Invented content mislabeled historical.";
            var text = PersonalStoryJson.Write(story);
            Assert.That(PersonalStoryJson.Parse(text), Is.Not.Null, "structural parsing is separate from evidence validation");
            Assert.Throws<PersonalStoryFormatException>(() => PersonalStoryJson.ReadValidated(text));
        }
    }
}
