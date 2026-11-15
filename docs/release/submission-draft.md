# Sankofa — Devpost draft

November 15, 2026 · Cut back to the [PM pass](../reviews/2026-11-14-assumptions-for-challenge.md#pm-pass) · Local copy. Intended entry: **Entertainment / Adapted or Significantly Updated**, pending eligibility and organiser classification. Nothing here has been published. The copy describes the November 14 build, whose Unity changes are [not yet verified](../testing/2026-11-14-unity-verification.md): cut any sentence whose feature does not survive that check. The story the entry leads with is not settled.

## Tagline

**For the stories you never got to hear.**

Alternate, if the card should say what it is (121 of 140 characters): *A paper theatre rises by a fire. Choose whose side of a meeting to follow, then turn the stage by hand to hear the other.*

## Short description

Sit by a fire and choose an heirloom. A paper theatre rises on the table before you and tells a meeting between two women. Follow one of them. Then sweep your hand across the stage: it folds, turns and tells the same meeting from the other side.

## Inspiration

Sometimes all a family story leaves behind is an object or a question. The original web Sankofa invited people to bring those fragments to a storyteller, a griot. We wanted that invitation to have a place in the room: a small fire and a paper theatre close enough to touch.

The name is an Akan word for going back to fetch what was left behind. What we most often leave behind is the other person's side. So every telling here is one meeting seen twice. A woman arrives with less flour than she promised. Follow her and you learn why she left some at home. Turn the stage and you learn what accepting less will cost the woman who was waiting. Neither is wrong, and you only understand the meeting when you have heard both.

## What it does

A griot's fire welcomes you. Reach for a bundle, a gourd or a water pot and the fire takes it; painted scenery and two figures stand up from the table, and one of three prepared tellings begins, set in Enkare Nairobi around the 1850s.

The pages turn themselves, with captions throughout. At the turning point you choose whose side to follow at her figure, and the stage turns to bring her forward. When her side is told, sweep a hand across the stage, or touch the other woman's label, and it turns again to her. If you would rather only watch, the fire chooses after a pause.

Keeping a telling threads a bead for each side you kept. The strand has room for six, two for each telling, and when you return the griot invites you to a side you have not heard.

It is designed for seated, hands-first play, and the three prepared tellings need no connection. Documented pages are marked and sourced; the people are imagined, and nothing claims to be anyone's ancestry.

## How we built it

Unity 6, Meta XR Core SDK and MR Utility Kit 207, OpenXR hands. The story rules, the session and every gesture are plain C# with no engine dependency, tested outside Unity; the scene, stage and room are thin adapters around them. The application owns what may happen next: no model drives the stage, the choices or the save.

The scenery and figures are AI-generated illustrations cut into layers and staged by hand; the narration is synthetic speech; the fire, the brook, the night and the stage's own rustle are field recordings, credited in the app. There is no AI-generated music. Room data and hand positions never leave the headset.

## What's next

Listening and cultural review with people from the communities the first setting draws on, then more settings only where we can source and review them. Two things are built and unproven: seating the theatre on your own table, and a spoken question to the fire. Both wait for time on a headset.

## What changed from the original Sankofa

The [original Sankofa](https://devpost.com/software/sankofa-y47f9p) is a web app: a visitor types a family name, a region and a period, and a model writes and narrates a heritage story with generated images. It established the griot, the fire and the invitation to bring a question.

This entry is a new Unity application for Meta Quest, built during the competition window, and shares no code, backend or media with the web app. New in it: a pop-up paper theatre that performs each page on a table beside the fire; hand interactions throughout (reaching for an heirloom, warming a hand at the fire, sweeping the stage round); three prepared tellings that each show one meeting from two sides; a kept strand of beads that records the sides kept across visits; and tellings that play with no network connection, in which documented context and imagined scenes are always told apart.

## Hand interactions (optional field)

Sankofa is designed for empty hands, so each step was built around what a seated person already does with them. You reach for the object you want. You hold a hand to the fire and it flares. You sweep the stage round the way you would turn something on a table to see its other side, and it leans with your hand before it gives way. Setting the gourd down between the two women is, for now, a label you touch above it. Each choice also has a labelled plaque that can be touched or pinched from a distance, and a visitor who does nothing after the first touch still hears a whole story. What excites us about hands in VR is stories that ask only for the gestures people already make at a table.

## Judge walkthrough

Sit and set the controllers aside. Reach for the **gourd**, or touch its card. At the turning point, choose **Follow the one who brought less** at her figure. When **Set the gourd down** appears, touch it. When the page says the meeting has another side, sweep a hand across the stage, or touch the other woman's label. At the ending, **Keep this story** and watch two beads join the strand. Pause, Options (recenter, stand height, larger text, still pages, captions only) and Leave stay on the page throughout. The full version is the [testing guide](../product/testing-guide.md).

## Private release notes — exclude from public copy

- Inspiration, What it does, How we built it and What's next come to 471 words; Devpost suggests 500 or fewer. The target launch date is its own field: confirm it with the entrant.
- Cut on November 15 by the PM pass, each to return only with its evidence: “about four minutes” (time a whole run); “everything works with hands alone, seated and offline” (one complete hands-only offline run of the release APK on a Quest); the visitor's own table by scan or resting hand, and any assurance about what is asked at launch (seen on a headset); beads as “the reason to come back” (visitor evidence); “the hands already knew”.
- The PM recommends saying plainly that the footage is from the simulator if no headset becomes available. A sentence ready for that: “All footage is from the Meta XR Simulator; we had no headset during the competition window.” The entrant decides.
- Every interaction named above is implemented and software-tested only. None has run on a headset, and the November 14 changes have not yet run in Unity. Before publishing, decide whether to say so in the entry. Do not add any sentence that implies device testing.
- The division recommends a changelog: [what was added during the window](changelog.md) is written to be quoted or linked.
- The video may use Meta XR Simulator footage and must be labelled as such. The [shot list](access-handoff.md#capture-one-story-well--target-230) follows this copy.
- Active [APK identity](candidate.md): the recorded candidate predates these changes. A new release build is needed after the Unity verification.
- Still needed: Start membership confirmed, the Competition channel and its invite link opened from a second account, a public YouTube or Vimeo link under three minutes, the launch date, cultural-review status for the draft label, and rights confirmation for the generated art and voice.
- See the [manual steps](manual-steps.md) and [official rules](https://start-developer-competition-26.devpost.com/rules). Hard close November 18, 3 p.m. Eastern / noon Pacific.
