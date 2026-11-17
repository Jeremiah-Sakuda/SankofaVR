# Sankofa rebuild: your room, their world, and what you carry home

November 17, 2026 · Direction set by the entrant · A specification. Nothing in it is built or tested

This is the new direction for Sankofa on a headset. It is written to be optimised for the experience. At the entrant's direction it ignores the competition deadline and sets aside the earlier build's product rules; those are [listed at the end](#17-set-aside-for-now-to-revisit-at-the-end) for review once the experience is designed. `AGENTS.md` and the [PRD](prd.md) still describe the earlier build and are unchanged until the entrant adopts this.

Every number here is a starting value chosen by reasoning, to be tuned on a headset.

## 1. Why rebuild

The [original Sankofa](https://github.com/Jeremiah-Sakuda/Sankofa/tree/35fd1d6f9ce32163e9d233aca9453bb521e87d05) moved people because the story was their own. The headset build tells the stories of imagined strangers, and a visitor has no reason to invest in them. Two rounds of internal review [stalled at 79 out of 100](../reviews/2026-11-16-five-rubric-rounds.md) with the same finding: clearer interaction did not create a reason to return.

Reading the original's code shows where its weight came from. It never told a story about the visitor's family. It did three things:

1. **The visitor declared it.** They said their family's name, where their people came from and which era drew them. The app took their word.
2. **The griot spoke to them as an inheritor.** "Tonight, I reach back for you." "Your ancestors lived. They loved. They built. They endured."
3. **It ended on them.** Every telling had three acts: the land, the people, and "the thread that reaches you".

The headset build has none of the three. The rebuild restores all of them and adds what only a headset can do:

1. **You sit with the teller.** The fire is in your own room.
2. **Their world stands around you**, at human scale.
3. **Your hands receive something.** Sankofa means "go back and get it", and that is a gesture.
4. **It comes home with you and stays** where you put it.

The whole visit has one shape: **your room, their world, your room again, carrying something back.**

## 2. The promise

> Tell the fire where your people came from. Their world rises around you. Reach back, take hold of what survived, and set it down in your own home.

The tagline stays: **For the stories you never got to hear.**

## 3. Design tests

Each is a test a build can pass or fail.

| Test | Passes when |
|---|---|
| It is yours | Every telling starts from what this visitor said, and their own words are said back to them |
| The griot speaks to you | Narration is in the second person, uses the family's name, and comes from a presence beside the visitor |
| No page in the air | Nothing in the visit could be done as well by a web page. No panels, docks or labelled buttons. Words are spoken; choices are things touched or said |
| The room is the beginning and the end | The first and last thing the visitor sees is their own room, and something from the telling is left in it |
| Hands receive | In every visit the visitor takes one thing in their hand and puts it somewhere |
| Seated, one hand, in reach | Every action works from a chair with one hand inside 60 cm. Voice makes it richer and is never required |
| A wonder in the first minute | Within 25 seconds a fire is burning in the visitor's own room. Within four minutes a world stands around them |
| The app keeps time | The model writes and paints. The application decides what happens and when, and can always pause, cancel or fall back |

## 4. The first visit

About nine and a half minutes. Times are from launch.

| # | Beat | Time | What happens | What the visitor does |
|---|---|---|---|---|
| 0 | Your room, at night | 0:00–0:25 | The room dims to firelight and a fire kindles on the floor | Sits |
| 1 | Welcome | 0:25–1:00 | "Come. Sit with me." The griot says who they are and why they are here | Listens |
| 2 | Declare | 1:00–2:30 | Three questions and one offered: name, place, how far back, anything you were told | Answers aloud, or with a globe and a thread |
| 3 | The gathering | 2:30–3:10 | The answers go into the fire. The room goes dark. The place is heard before it is seen | Listens |
| 4 | Act 1: the land | 3:10–4:40 | A painted land spreads out from the fire and surrounds the visitor's chair | Looks; may touch the land |
| 5 | Act 2: the people | 4:40–6:20 | A nearer place forms with people at human scale. One of them is doing the work the visitor spoke of | Looks; may ask |
| 6 | Act 3: the thread | 6:20–7:50 | The way out of that place. A thread of gold light runs from the people to the visitor's side | Listens |
| 7 | Go back and get it | 7:50–8:20 | The thread's end glows within reach | Takes it and draws it in; a bead arrives in the hand |
| 8 | Home | 8:20–9:20 | The painted world gives way to the visitor's room. The bead is still in their hand | Sets it down somewhere in their room |
| 9 | Ask the griot | Any time after 3:10 | The fire leans in and answers | Holds a palm toward the fire and speaks, or touches a spark |

### Beat 0 — Your room, at night

- The app opens on the visitor's own room, unaltered, for two seconds. Nothing else is shown.
- The room's light falls. The camera view darkens to about 15% over six seconds and warms toward amber.
- A fire kindles on the floor 1.5 m in front of the seat (1.3–1.8 m, clear of furniture where the room is known). Flames reach about 45 cm. The visitor looks down at it by no more than 30 degrees.
- A pool of warm light lies on the floor around the fire, 1.5 m in radius, and flickers with it. Where the room's shape is known, walls and furniture catch the flicker too.
- The fire is heard from where it is.

If the room's surfaces are not available, the fire sits at the floor height estimated from the seated head and the pool of light is drawn on that plane.

### Beat 1 — Welcome

Recorded lines, trimmed from the original's opening:

> Come. Sit with me.
>
> I am a griot — a keeper of the stories that time tries to scatter.
>
> Tonight, I reach back for you.

The flame moves with the voice: it rises on stressed syllables and settles in pauses. Gold sparks lift as the griot speaks. One instrument enters under the second line.

On a first run only, one more sentence follows: "To reach back, I send what you tell me to those who help me write and paint. If you would rather I did not, touch the dark stone, and I will tell you of fires I already know."

### Beat 2 — You declare

The griot asks. The visitor answers aloud or by hand. After each answer the griot says it back, and a spark carrying it rises from the visitor's side and hangs above the fire.

| Question | Aloud | By hand | Said back |
|---|---|---|---|
| "What name does your family carry?" | Says it | Optional. May be skipped, or written with the system keyboard | "Sakuda." Exactly as heard |
| "Where did your people come from?" | "The coast of Ghana" | **The globe** | "The coast of Ghana." |
| "And how far back shall I reach?" | "My great-grandmother's time", "the 1920s" | **The thread** | "Your great-grandmother's time." |
| "Were you told anything of them? A trade, a town, a saying. Even a little." | Says it | Not required | Kept for Act 2 |

- **The globe.** A sphere of dark glass with coastlines in gold, 26 cm across, appears at chest height 45 cm away. A swipe turns it. A touch held for one second chooses a place. The griot names what was touched, and a second touch or "yes" confirms it.
- **The thread.** A gold thread stands up from the chosen place. The visitor pulls it toward themself, and the griot counts back as it lengthens: "Your parents' time… your grandparents'… their parents'… a hundred and fifty years… two hundred… three hundred… five hundred… as far as the fire can see." There are eight steps, 3.5 cm apart. Letting go chooses.
- **A wrong echo.** The visitor says "no" or waves the spark away with a sideways sweep, and the griot asks again. After a second miss on the name the griot says "Then tonight you are kin to this fire", and uses no name.
- **Silence** for four seconds after the last question: "Then sit. I will listen for you."
- **No microphone.** The questions are still spoken. The name is skipped and the place and era are chosen by hand.

Microphone and room permissions are asked once, by the system, before the room dims on the first run, so nothing interrupts the griot later. Either may be refused.

### Beat 3 — The gathering

Recorded lines from the original:

> Every family has a thread that stretches back — through oceans crossed and borders redrawn, through languages that changed shape but never lost their meaning.
>
> Your ancestors lived. They loved. They built. They endured.
>
> So let me gather the threads now. Let me listen to what the land remembers, what the old songs still carry.

- The sparks circle the fire and sink into it. The fire rises.
- The room goes fully dark except for the fire and **the hearth circle**: an oval of the visitor's own floor about 3.2 m by 2.6 m, holding their chair and the fire, lit at firelight level.
- The place is heard before it is seen. Its sound arrives from the direction the world will open: wind first, then water, voices or animals.

This beat covers the wait for the service. It lasts at least 25 seconds and can hold for 75 with more of the original's lines ("Some of those threads are written in books…", "And their story did not end — it has been waiting, patient as the baobab, for someone to come and listen."). After 75 seconds the griot says "The road is long tonight" and offers the nearest fire they already know; the visitor may accept or keep waiting.

### Beat 4 — Act 1: the land

- From the far side of the fire a wash of colour spreads across the dark like water into paper: sky, then horizon, then ground running up to the edge of the hearth circle. It takes eight seconds.
- The visitor is now sitting in their own chair, on their own floor, in a painted land that surrounds them across 240 degrees. The painted horizon is at their eye height, so the land reads as life-size.
- The griot tells the land in two short paragraphs, 70 to 90 seconds in all, and begins with the name: "This is the land the Sakudas knew."
- The first time the griot states something documented, one line explains the fire: "What the books remember burns gold. What I imagine rises as smoke." From then on, sparks are gold over remembered passages and pale smoke rises over imagined ones.
- The land answers a hand. Reaching toward water sends ripples through it, grass leans, a bird lifts.

### Beat 5 — Act 2: the people

- The land softens as if rewetted and a nearer place forms: a market, a compound, a workshop, a shore. The fragment and the place decide which.
- People stand in it at human scale. One of them, **the focal figure**, is three metres away, lit by the visitor's fire, doing the work the visitor spoke of and holding **the thing**: a cloth, a weight, a net, a gourd.
- The griot says the visitor's fragment back once, in their words, and directs their eyes: "You told me she was a trader. Look to your left — a woman like the one you were told of."
- With no fragment, the service chooses the commonest livelihood for that place and time: "a woman like those your people would have known."

The figure is never said to be the visitor's relative. "Like the one you were told of" lets the visitor complete it, and a wrong detail then cannot break it.

### Beat 6 — Act 3: the thread

- The people's place thins toward the way out of it: a road, a shore, a station.
- As the griot tells of leaving, of what was carried and what survived, a thread of gold light is drawn from the thing in the figure's hands, along the painted ground, over the edge of the hearth circle and onto the visitor's real floor. Its end rises to hover beside them, 45 cm away at chest height, and glows.
- The act ends on the visitor: "…and it did not end there, Sakuda. It reaches you, sitting here tonight."

### Beat 7 — Go back and get it

> The Akan people have a word — Sankofa. It means: go back and get it.

- The visitor closes a hand on the thread's end and draws it toward themself. Either hand; a pinch or a fist within 6 cm of the end takes it.
- As they pull through 20 cm, the thread tightens across the painted ground. The thing leaves the figure's hands and travels along it, becoming **a bead** of painted glass, 4 cm across, that arrives in the visitor's hand. Held up to the eyes, the people's place can be seen inside it.
- After 20 seconds untouched: "Reach for it. It is yours." After 20 more the bead comes to rest beside the fire, where it can be taken later. It is never put into the hand.

### Beat 8 — Home

- The painted world dries, pales to paper and lets the room back in, from the hearth circle outward. The room is still dim and firelit. The bead is in the visitor's hand, with the thread trailing from it to the fire.
- "Set it where you will see it." The visitor opens their hand over any real surface, or in the air. Over a surface the bead settles the last 10 cm by itself, with a small sound, and glows.
- "Come back, and I will reach further." The fire sinks to coals and the room's light returns over six seconds. The bead and its thread remain.

### Beat 9 — Ask the griot

- An open palm held toward the fire for one second makes the fire flare and lean in. The visitor speaks, and the griot answers in two or three sentences.
- During an act the narration stops at the end of its sentence and resumes after the answer.
- After each act three questions rise as sparks beside the fire, such as "What did they eat?", "What was the music?" and "Why did they leave?". Touching one asks it.
- "If I have it wrong, tell me, and I will carry your telling instead." A correction or a family detail given here is kept with the bead as the family's own account and used in later tellings.

## 5. Coming back

The room opens with the beads where they were left and a thread running between them. The fire kindles: "You came back, Sakuda."

| The visitor | What happens |
|---|---|
| Carries a bead to the fire | That world reopens exactly as it was told, with no service. The visitor may hear it again, or only **sit in it**: the land and its sound, with no narration |
| Pulls a bead's thread further | "Further back": the same place in an earlier time |
| Says or touches "the road they took" | A telling of the journey. A later visit can tell the other side of leaving: the ones who stayed |
| Turns the globe | Another line of the family: a new name or place |
| Holds a palm to the fire | Asks anything |
| Holds a bead in the fire for three seconds | The griot warns at one second. At three the bead burns away and its telling is deleted |

- Each telling adds one bead to the same strand, tinted by its world. The strand is the only record of progress. There are no scores, counts or badges.
- The strand can be lifted whole and hung somewhere else.
- In a different room, or when the places cannot be found, the strand lies coiled by the fire: "I carried these for you." The visitor places it again.

## 6. Hands, voice and comfort

| Action | By hand | Aloud | Starting tolerance |
|---|---|---|---|
| Turn the globe | Swipe across it | "Show me Africa" | Follows the fingertip; coasts to rest |
| Choose a place | Touch and hold | Says the place | 1.0 s hold within 2 cm of the surface |
| Choose an era | Pull the thread, let go | Says the era | 3.5 cm per step |
| Confirm what was heard | Touch its spark | "Yes" | Spark is 5 cm across |
| Reject it | Sideways sweep through the spark | "No" | 20 cm in 0.7 s, as the existing sweep |
| Touch the land | Reach toward it | — | Hand raised and extended past 35 cm |
| Take the thread | Pinch or fist at its end | — | Within 6 cm |
| Draw the bead in | Pull toward the body | — | 20 cm |
| Place the bead | Open the hand | — | Settles onto a surface within 10 cm below |
| Ask | Open palm toward the fire, or touch a question spark | Speaks | 1.0 s hold, palm within 40 degrees of the fire |
| Pause, words, leave | Touch a hearth stone | "Pause", "Show the words", "I must go" | Stones are 9 cm across |

- **Hearth stones.** Three stones sit at the near edge of the hearth circle on the side the visitor chooses: Pause, Words and Leave. A word appears over a stone when a hand comes near. Holding the Words stone fans out the few options: text size, volume, which side the stones are on, move the fire. These are the only standing controls.
- **Words.** Captions are off by default and the Words stone glows once during the welcome. When on, one or two lines appear low over the fire as lit text with no panel behind them.
- **The record.** A sheet of paper the visitor can hold lists a telling's words, which were remembered and which imagined, and any sources. It is asked for through the Words stone while holding a bead.
- **Hands.** In the room the visitor sees their own hands. In the painted world, where the camera view is covered, hands are drawn in gold line.
- **Comfort.** The seat never moves and the view is never moved for the visitor. Worlds arrive and leave as washes of colour. The hearth circle of real floor is always visible, so the visitor can always see their feet and what is near them.
- **Field of view.** Everything the visitor must act on sits within 30 degrees of straight ahead. The surround is atmosphere.

## 7. The griot

- **Who.** A keeper of stories from many fires, present as the fire. The griot speaks to the visitor as kin to this fire tonight.
- **Voice.** The same voice as the original and the current build: Gemini speech, voice Kore, with the original's direction ("warm, unhurried… this is oral storytelling, not news reading"). Spoken answers use a live voice session where one is available, so the reply is immediate and a name is said back as it was heard.
- **The name** is spoken at most four times: when it is said back, at the opening of the land, at the thread and at the farewell.

Rules for generated narration, all for how it sounds:

1. Written to be heard. No sentence longer than 22 words.
2. Second person at least once in every paragraph.
3. Concrete things from that place and time: named foods, tools, winds, streets. One fact per paragraph, carried inside an image, never a list.
4. Describe what is painted and what is heard, and direct the eyes at least once per act ("to your left", "beyond the fire").
5. End each act on an image, not a moral.
6. No stock phrases ("children of the sun", "since time immemorial", "rich tapestry") and no borrowed idiom ("hear ye").
7. Where knowledge is thin, say so once, in character: "The books are quiet about this place. Let me tell you what the land suggests."
8. Hard things that belong to a place's thread (enslavement, famine, partition, war) are named plainly and given a breath of silence. They are never painted. The picture shows the shore, the road or the empty house.

## 8. The world

**Style.** The original's watercolour, unchanged: visible brushstrokes, soft wet edges, transparent washes with paper showing through, in burnt sienna, raw umber, yellow ochre and gold. No photorealism. Watercolour hides the flaws of generated pictures and reads as painted, not as a failed attempt at the real.

| Part | What it is | Where |
|---|---|---|
| Backdrop | A wide painting of the place with open ground in its lower third | A curved wall 12 m away spanning 240 degrees, horizon at seated eye height |
| Middle distance | One or two painted layers: trees, roofs, boats | 6 m and 4 m, so they shift against the backdrop when the head moves |
| Focal figure | One person, full length, in Act 2 | 3 m away on the painted ground, life-size, standing, kneeling or seated |
| Ground and sky | Flat washes taken from the painting's own colours | From the hearth circle to the backdrop; overhead |
| Hearth circle | The visitor's real floor, chair and feet | Around the seat and fire, with a 40 cm soft edge |
| Atmosphere | Dust, mist, rain, pollen or embers, as the telling asks | Between the visitor and the middle distance |
| Sound | One bed and two details, placed around the visitor | All sides |

- **Arriving and leaving.** A wet edge spreads out from the fire over eight seconds. Between acts the painting is rewetted and re-forms over five. Going home, it dries and pales over six.
- **Movement.** Nothing moves like film. Pigment breathes slightly, water shimmers, cloth and leaves sway, and firelight flickers on whatever is near. A figure changes pose only by dissolving into another, at most twice.
- **Light.** The fire is the only light source the visitor believes in. It warms the figure and the near ground and nothing far away.
- **The bead.** A glass sphere with the people's place wrapped inside it, tinted by that world's main colour.
- **The thread.** A ribbon of light with a little sag, which tightens when pulled.

Budgets to hold: 72 frames a second and never under 60; no more than four see-through layers overlapping anywhere; one act's pictures in memory at a time, with the next loading behind it.

## 9. What the service makes

The service is the original's pipeline, reshaped to return a world, not a page.

**What the app sends**

```json
{
  "family_name": "Sakuda",
  "place": {"said": "Kenya, near Nairobi", "lat": -1.29, "lon": 36.82},
  "era": {"said": "my great-grandmother's time", "step": 3},
  "fragment": "She sold milk",
  "earlier": ["t-8f2c"]
}
```

A spoken answer is sent as a short recording and comes back as what was heard, what it means and the line to say back.

**Steps**

| Step | Does | From the original |
|---|---|---|
| 1. Hear | Turns a spoken answer into a name, a place or an era, and writes the echo | New |
| 2. Ground | Looks the place and era up in the knowledge base; where that is thin, searches and keeps the links | `lookup_cultural_context`, `research_region_history` |
| 3. Plan | Plans three acts, the focal figure, the thing, the thread, the sounds, the palette and where things stand around the visitor | `plan_narrative_arc`, extended |
| 4. Tell | Writes each act's paragraphs to the rules in section 7, each marked remembered or imagined | `generate_act_segments` |
| 5. Paint | Paints each act's backdrop and middle layers, the figure on plain paper, and the bead | Image generation, extended |
| 6. Speak | Records each paragraph in the griot's voice | `generate_audio_narration` |
| 7. Check | Rereads the telling against the narration rules and rewrites once if it fails | The critic agent |

The knowledge base starts with the original's thirteen regions by decade and the current build's Enkare Nairobi material.

**What comes back.** A telling, sent in pieces as each is ready: the plan first, then Act 1, then the rest while Act 1 plays.

```json
{
  "schema": "sankofa.telling.v1",
  "id": "t-91ab",
  "acts": [
    {
      "id": "land",
      "paragraphs": [
        {"text": "This is the land the Sakudas knew…", "truth": "remembered", "audio": "land-1.mp3", "seconds": 34.1, "look": -40}
      ],
      "world": {
        "backdrop": "land-back.jpg", "middle": ["land-mid.png"],
        "horizon": 0.52, "sky": "#E9C98F", "ground": "#B98A52",
        "sound": {"bed": "wind", "details": ["cattle", "river"]},
        "air": "dust",
        "answers": [{"kind": "water", "from": -70, "to": -20}]
      },
      "questions": ["What did they eat?", "What was the music?", "Who ruled here?"]
    },
    {"id": "people", "focal": {"image": "figure.png", "at": -35, "metres": 3.0, "posture": "kneeling", "holds": "a milk gourd"}},
    {"id": "thread", "survives": "the way a name is spoken"}
  ],
  "keepsake": {"name": "the milk gourd", "bead": "bead.jpg", "tint": "#B5651D"},
  "sources": [{"title": "…", "url": "…"}]
}
```

**Time.** Act 1's first picture and first paragraph must be ready within 35 seconds of the last answer, which the gathering covers. Acts 2 and 3 must be ready before the act ahead of them ends.

**What the app does with it.** The app checks every piece before using it: the shape of the data, the lengths, that each picture and recording opened. A piece that fails is dropped without stopping the visit: no figure, the people's place is told without one; no middle layer, the backdrop stands alone; no recording, the paragraph is asked for again and, failing that, shown as words. If Act 1 cannot be made, the griot offers a fire they already know.

**Keeping.** A finished telling is saved whole on the headset: its words, recordings and pictures. A bead reopens it with no service. Nothing is saved until the visitor has taken the bead.

## 10. Without the service

The app ships with a small number of **known fires**: complete tellings made with the same pipeline and then finished by hand. One of them is Enkare Nairobi, using the river scene already built, which is also the standard a generated world is held to.

- With no connection the griot says "Tonight I can only reach the fires I already know", and the known places are lit on the globe.
- The same fires are what the griot offers when the service is slow or fails.
- A known fire has no name in it, because its lines are recorded in advance. Everything else in the visit is the same, including the thread, the bead and the strand.

## 11. Sound and music

- **Fire, place and touch.** The fire; a library of place sounds extending the original's ten (wind, fire, nature, market, drums, rain, ocean, river, crickets, village); and small sounds for the wash arriving, the thread, the bead and setting it down.
- **Music.** One theme on one instrument, played by a person. It is heard under the welcome, at the thread and on the way home, and nowhere else. Its source is to be settled; the original's opening music has no known provenance.
- **Balance.** The voice is always the loudest thing. Place sounds drop under speech and music sits under both.

## 12. How it is built

One new scene, **Fireside**, made by the scene builder like the others. The existing scenes stay as they are, as comparison records.

| Module | New, kept or changed | Owns |
|---|---|---|
| Telling (engine-free) | New | The telling's data, its checks, what is remembered and imagined, lengths and versions |
| Fireside session (engine-free) | New, on the pattern of the current session | The visit's states, waits, cancellation, fallbacks and exact replay |
| Gestures (engine-free) | Kept, with two added | Touch, sweep and rest as now; pull (thread) and held palm (ask) are new |
| Storage (engine-free) | Changed | Saved tellings; the strand; which bead is which telling and where it was placed |
| Room | New, from the table placement work | Dimming and warming the camera view, the hearth circle, placing the fire, finding surfaces, remembering places |
| Griot fire | Changed, from the river fire | Flame, sparks and smoke; moving with the voice; leaning in to listen |
| Declare | New | The questions, recording, the globe, the era thread and the echoes |
| Telling client | Changed, from the story service client | Starting a telling, receiving its pieces, hearing, asking; the cache |
| Painted world | New, from the river surround | Backdrop, layers, figure, washes, air and the land's answers to a hand |
| Narration | Changed | The speaking queue, stopping at a sentence, balance, words over the fire |
| Thread and bead | New | The thread, taking it, the bead, placing it |
| Strand | Changed, from the bead strand | Beads in the room, reopening, further back, forgetting |
| Hearth stones | New | Pause, words, leave and the few options |
| Service | Changed | Hear, ground, plan, tell, paint, speak, check; the live conversation |

The visit's states, in order: room, kindle, welcome, declare, gathering, land, people, thread, receive, home, rest. A return visit goes room, kindle, greeting, then whichever the visitor chooses. Pause is possible in every state. Leaving a visit before the bead is taken saves nothing.

Platform features this relies on, each present in the Meta SDK version the project already uses: dimming and tinting the camera view; showing the camera view through gaps in what the app draws; reading the room's floor, walls and tables where the visitor has scanned them; finding a real surface under a hand without a scan; and saving a place in the room so that something can be found there again in a later session.

## 13. What carries over and what is retired

| From today's build | Decision |
|---|---|
| The fire, its flame layers, logs and coals; a hand held to the fire | **Kept**, as the griot's presence and the way to ask |
| The griot's voice, its direction and the recording tools | **Kept** |
| The river scene: the curved painted surround, painted figures, solid props near the visitor | **Kept** as the Enkare Nairobi known fire and as the model for a generated world |
| The choice-to-dissolve crossing in the river build | **Kept**, as the way every world arrives |
| Recording a spoken question and the service's ask route | **Kept**, as the start of hearing and asking |
| Saving a realised telling and replaying it exactly | **Kept** |
| Beads on a strand | **Changed**: one bead per telling, placed in the visitor's room |
| Placing the story on the visitor's table | **Changed**: the same surface finding places the bead |
| The sweep gesture | **Changed**: it waves away a wrong echo |
| The service's cancellation, validation and no-logging plumbing | **Kept** |
| The reading panel, its dock, plaques and labelled buttons | **Retired** |
| The miniature paper stage | **Retired** from the visit; its scene stays as a record |
| Choosing a bundle, gourd or water pot to begin | **Retired**; the visitor declares |
| Three prepared tellings as the product; matching a typed question to one of them by keyword | **Retired**; the tellings remain inside the Enkare Nairobi fire |
| One meeting told from two sides, turned by hand | **Retired** as the core. The idea returns in "the ones who stayed" |

## 14. Order of work, and what each step proves

Each step ends with something a person can sit in. The second is the one the whole direction depends on.

| Step | Builds | Proves | Done when |
|---|---|---|---|
| 1. A fire in your room | Dimming, the fire on the floor, its light on the room, the welcome | "You sit with the teller" | A newcomer, unprompted, says the fire is in their room |
| 2. A world from a sentence | The service's plan, Act 1 words, picture and voice from typed input; the surround; the wash | That a generated painting can stand around a person at life size, in time | Three newcomers describe being somewhere, not looking at a picture; first picture inside 35 seconds |
| 3. Declare | Hearing; the globe; the era thread; the echoes | That saying it, or pointing to it, feels like declaring | A complete declaration by voice alone and by one hand alone, and the name said back correctly for eight of ten people |
| 4. Three acts, the thread and the bead | Acts 2 and 3, the focal figure, the thread, taking, the bead | "Your hands receive" | Newcomers reach for the thread without being told twice |
| 5. Home and return | Going home, placing, remembered places, the strand, reopening, further back | "It comes home and stays" | A bead is where it was left a day later, and reopens with no connection |
| 6. Ask the griot | Live conversation, question sparks, corrections kept | The griot as someone, not a recording | A spoken question is answered within two seconds |
| 7. Finish | Known fires, music, words, the record, the hearth stones, speed | The whole visit, offline and on | A full visit with no connection and no microphone, at speed on a headset |

Software checks, simulator runs, headset sessions, listening and newcomers' reactions stay separate kinds of evidence, as now. Steps 1, 2 and 4 cannot be judged without a headset.

## 15. Against the competition's criteria

The private competition brief is not in this clone, so this uses the [official judging page](https://start-developer-competition-26.devpost.com/details/judging).

| Criterion | What this direction offers |
|---|---|
| Innovation and creativity | The room becomes the fireside, the world arrives around the visitor's own chair, and the product's name is its central gesture |
| Experience design | Hands-only from start to finish with no typing; seated and within reach; the room changes what happens, since the fire and the beads live in it |
| Technical implementation | Hand tracking, camera view, room understanding and remembered places are each load-bearing; a generated world streamed and checked by the app |
| Polish and presentation | One painted style, one voice, one instrument; the adaptation from the web original is plain to see |
| A reason to come back | A strand of the visitor's own family's worlds, growing in their home |
| The first five minutes | A fire in the room in 25 seconds, the visitor's own name said back in two minutes, a world around them in four |
| Remove the service | Known fires give a whole visit, including the thread, the bead and the strand |

## 16. What is not known

Ranked by how much depends on each.

1. **Whether a generated painting holds up at life size around a person.** Seams between pictures, the horizon's height, and whether a figure made separately sits in its scene. Step 2 exists to answer this before anything else is built on it.
2. **Whether Act 1 can be ready in 35 seconds**, with pictures as wide as a surround needs.
3. **Whether names and places are heard correctly**, and what a wrong echo costs the moment.
4. **Whether the fire's light on a real room is convincing**, and what the visit looks like in a room that was never scanned.
5. **Whether a bead is found again** in the same place days later, and what happens when furniture moves.
6. **Whether a visitor who knows their history hears something wrong**, and whether "tell me, and I will carry your telling instead" is enough to hold them.
7. **What a telling costs to make**, and how many a day one person should be allowed.
8. **How the visit feels to someone whose people's thread includes great harm.**

## 17. Set aside for now, to revisit at the end

The entrant asked that the earlier build's rules not shape this design. These are the ones it departs from, each to be looked at again once the experience exists.

| Earlier rule or caution | What this spec does instead |
|---|---|
| Historical statements are exact approved passages with sources | The model writes history from its own knowledge, the knowledge base and search, and marks its own passages as remembered or imagined |
| A model's label is not verification | The fire's gold and smoke follow the model's own marks |
| Nothing infers ancestry from a name or place; no family name is required | The family name is asked for first, spoken back and used in the telling |
| The griot does not claim kinship or one tradition for every setting | One griot, kin to the visitor at this fire, tells of any land |
| Only reviewed places are told, and coverage is published | Any place is told; thin knowledge is admitted once, in character |
| No generated pictures at run time | Every world is painted for its visitor |
| No AI-generated music | Music is human-performed, but its source is not yet settled |
| Consent before anything leaves the headset; minimal stored personal data | One spoken sentence on the first run; name, place, fragment and questions are sent, and tellings are kept on the headset |
| The web app's code and assets are not imported without a licence and ownership check | The pipeline, knowledge base, opening lines and recording are reused; the check is still owed |
| Cultural review before release | Not addressed here |

## 18. Later, not in this rebuild

- **Keep a real voice.** Record a relative telling something true into a clear bead that hangs on the same strand. It is the most direct answer to "the stories you never got to hear", and worth its own design.
- **Sit together.** A relative in another home at the same fire.
- **Give a bead away.**
- **Tellings in the family's own language.**
