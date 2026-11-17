# Sankofa — Devpost draft

November 16, 2026 · Inspiration updated from the original Sankofa at the entrant's request · Local submission draft, unpublished. Intended entry: **Entertainment / Adapted or Significantly Updated**, pending eligibility and organiser classification. The [current verification record](../testing/2026-11-15-story-table-verification.md) separates software checks, desktop review and open device acceptance. The water telling leads the submission and demonstration; the [story comparison](access-handoff.md#why-the-water-story-leads) records the choice. This copy describes the prepared offline table visit pending the [release-scene decision](../product/delivery-plan.md#submission-plan--november-1618); online generation and spoken questions are outside the demonstration promise.

## Tagline

**For the stories you never got to hear.**

Descriptive card copy: *A paper theatre for the everyday lives behind history. Sit by the fire and turn a meeting to hear its other side.*

## Short description

Sankofa brings the everyday lives behind history to your table. Sit beside a fire and explore three illustrated tellings set around Enkare Nairobi in the 1850s. Follow one person through a meeting, then turn the paper stage with your hand to hear the other side. Documented context is marked; the people and meetings are imagined.

## Inspiration

Sankofa began with the gap between knowing where a family comes from and feeling connected to the lives that came before. A place name or inherited fragment can carry enormous meaning, while the stories around it remain scattered or untold. Voice, imagery and oral storytelling can make that past more approachable.

That motivation carries into Quest through a seated encounter: share the space of a telling, listen to one person, then hear the same meeting from someone else's perspective. The water story asks what it means to receive a memory and eventually become the person carrying it.

Our first collection offers three authored tellings around Enkare Nairobi in the 1850s, alongside sourced historical context. The people and meetings are imagined. This is a way into curiosity about the past; it does not reconstruct a visitor's relatives or establish their ancestry.

## What it does

Choose the water pot. A woman brings a younger traveller to a riverbank she remembers, then doubts she has found the right bend. Follow her as she admits the uncertainty and shares the sound she remembers. Turn the stage toward the younger traveller. Years later, she will carry that memory herself, with uncertainty of her own.

The meeting stays the same. What you understand about it changes.

A bundle and a gourd open two more tellings, about a journey and an exchange. Your chosen interest selects a telling; your hand chooses whose side to hear. Captions accompany prepared narration, pages can turn themselves, and the fire can choose if you prefer to watch. The characters make their own decisions.

Keep deliberately, and a bead marks each side heard. Reopen the saved telling or explore a side still unheard. The full prepared visit runs offline and is designed for seated hands use. An adjustable stand keeps it nearby; **Use my table** offers local surface fitting. Quest hands, room fitting and performance still need hardware verification.

## How we built it

Unity 6 and Meta XR supply the scene, hands and room adapters. Engine-independent C# owns story state, source validation and saved replay. The application controls every accepted action and cancellation. Optional generation is separate from the complete prepared experience.

AI-generated illustrations are layered and staged by hand. Prepared narration uses synthetic speech; environmental recordings are credited in the app. No AI-generated music is used. Room geometry stays in local placement code.

## What's next

Next are headset and listening checks, cultural and pronunciation review, and sessions with newcomers to learn whether the second perspective changes their understanding. Further settings need their own sources and review. We want a visitor to leave remembering a person and carrying a question they want to explore.

## What changed from the original Sankofa

The [original Sankofa](https://devpost.com/software/sankofa-y47f9p) is a web app: a visitor types a family name, a region and a period, and a model writes and narrates a heritage story with generated images. It established the griot, the fire and the invitation to bring a question.

This entry is a new Unity application for Meta Quest, built during the competition window, and shares no code, backend or media with the web app. New in it: a pop-up paper theatre that performs each page on the visitor's table; hand interactions throughout (closing a hand on an heirloom, warming a hand at the fire, setting the gourd down, sweeping the stage round); three prepared tellings that each show one meeting from two sides; a kept strand of beads that records the sides heard across visits; placement on the visitor's real table from the room scan or from a resting hand; and a complete offline experience in which documented context and imagined scenes are always told apart.

## Hand interactions (optional field)

The hand actions carry the meaning of the visit: reach for the water pot to begin and turn the stage toward the person whose side you want to hear. The theatre changes its arrangement to bring that figure forward. Each choice also has a labelled plaque for touch or distance selection. Automatic progression supports a visitor who prefers to watch. The separate gourd telling adds an invited action to set the vessel between the women. These interactions have software coverage with synthetic input; uncoached use and real-hand reliability still need hardware review.

## Judge walkthrough

Sit and set the controllers aside. Choose the **water pot**, labelled **Place & water**, by closing a hand on it or selecting its card. After the documented page, choose **Stay with the one who remembers**. When her side is told and the page offers another side, sweep a hand across the stage or select **Walk with the younger traveller**. Let her final sentence finish. At the ending, choose **Keep this story**; two beads mark the sides heard. Reopen the kept telling from the first page. Optional: try **Use my table**, or the gourd telling's **Set the gourd down** invitation. Pause, Options and Leave stay on the page. The full [testing guide](../product/testing-guide.md) includes the additional checks.

## Private release notes — exclude from public copy

- The target launch date needs an entrant-owned commitment before submission. Do not invent a date from the competition deadline.
- On November 16 the entrant authorised reusing the [original Sankofa inspiration](https://devpost.com/software/sankofa-y47f9p). The section above adapts that published product motivation, not an independently supplied autobiographical incident. It omits the original audience-size statistic, which has not been verified for this entry. Do not invent a lost relative, interview, testimonial or childhood memory.
- The entrant confirmed no headset or remote Quest tester is available. Produce actual XR Simulator footage and identify the simulated input mode; keep device hands/reach, room fitting, frame rate and comfort explicitly unverified. The river remains a release candidate until the scene decision; update the descriptive copy and walkthrough together if it is selected.
- The audience and unmet need above are the product hypothesis, not measured demand. The water selection is an editorial judgement from the authored text and staging, not a measured emotional response. With five unfamiliar viewers, first show the opening and ask what they expect; then show both sides without explanatory pitch narration and ask what changed; finally ask what they would explore next and why. Expectations of recovered biography or physical-object scanning fail the promise check. Remembering only the turn animation fails the intended narrative payoff. This is a small formative check, not population evidence.
- The emotional promise is exploration of context and perspective. The prepared release offers three authored tellings, not recovered family stories or unlimited personal generation. A physical heirloom is not scanned or imported. Keep this distinction clear in the edit.
- Griot is the product's narrator framing, not a claim that the first East African setting represents a single pan-African storytelling tradition. Cultural review must address the framing as well as the story content.
- The water story's payoff is the younger traveller becoming a keeper of the memory, with uncertainty of her own. Hearing a sound is already present in the older woman's side; do not sell it as a new revelation. No family relationship between the two women is established. The [shot list](access-handoff.md#capture-one-story-well--target-230) protects the younger traveller's final sentence and a pause after it.
- A bead records a kept perspective. Six beads are finite content, not evidence of a habit or continuing retention. Personal input can select a prepared thread; the demo does not show fresh generation from a visitor's life.
- Actual Unity checks are recorded in the current verification note. None establishes headset, listening or participant acceptance. Keep the material disclosure beside the public capability claim until the relevant check is complete.
- The division recommends a changelog: [what was added during the window](changelog.md) is written to be quoted or linked.
- The video may use Meta XR Simulator footage and must be labelled as such. The [shot list](access-handoff.md#capture-one-story-well--target-230) follows this copy.
- Active [APK identity](candidate.md): the recorded candidate predates these changes. A new release build is needed after the Unity verification.
- Still needed: Start membership confirmed, the Competition channel and its invite link opened from a second account, a public YouTube or Vimeo link under three minutes, the launch date, cultural-review status for the draft label, and rights confirmation for the generated art and voice.
- See the [manual steps](manual-steps.md) and [official rules](https://start-developer-competition-26.devpost.com/rules). Hard close November 18, 3 p.m. Eastern / noon Pacific.
