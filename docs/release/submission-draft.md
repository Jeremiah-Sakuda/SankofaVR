# Sankofa — Devpost draft

November 8, 2026 · Pop-up theatre · Local copy. Intended entry: **Entertainment / Adapted or Significantly Updated**, pending eligibility and organiser classification. The available edit session was previously signed out; no live page, release upload or entry submission is recorded here.

## Tagline

**For the stories you never got to hear.**

## Short description

Choose an heirloom at the fire. A story rises from your table. Follow one woman through a difficult exchange, then turn the stage to see what the same meeting meant to the other.

## Inspiration

Sometimes the only thing a family story leaves behind is an object or a question. The original web Sankofa invited people to bring those fragments to a storyteller. We wanted to give that invitation a place in the room: a small fire and a paper theatre close enough to reach.

Our first setting is Enkare Nairobi in the 1850s. In one telling, a woman arrives with less flour than she promised. Follow her and you learn why she kept some at home. Follow the woman who expected more and you learn what accepting less will cost her. The story turns on seeing both acts of care.

## What it does

Choose a bundle, a gourd or a water pot to begin one of three prepared tellings. Painted scenery and figures rise from the table. At the turning point, choose whose side to follow. The stage folds, turns and brings the chosen character forward. You can also let the fire choose.

When the women reach their agreement, an invitation lets you set the gourd between them with a hand action. The scene also continues if you wait. The women decide the terms themselves. At the end, Keep saves that telling on the device, and a new visit can show the side you haven’t heard.

You can also type a question to steer the choice of prepared telling. The judged experience works offline and uses authored stories. Captions distinguish imagined scenes from documented context, and source details are available as you read. The stories do not identify relatives or establish ancestry.

Optionally, on Quest 3 and 3S, the theatre can fit itself to a detected table; this mode has not yet been tried on a headset. The adjustable stand keeps the seated visit available when room data is missing or access is denied.

## How we built it

We built a Unity paper theatre with staged scenery, painted figures, captions and a voice at the fire. The application controls the story, the perspective turn, the optional gourd cue, pause and local replay. The story contract separates source-linked history from imagined characters and dialogue. Meta OpenXR handles hands, and MR Utility Kit handles optional local table fitting. No room geometry is sent to a story provider.

The painted scenery and the two women are AI-generated illustrations (OpenAI image generation) that we cut into layers and staged; the stage, motion and choices are built in Unity. The narration is synthetic speech from Google Cloud Text-to-Speech. The fire, river and night ambience are attributed field recordings. No AI-generated music is used.

## What changed from the original Sankofa

The [original web project](https://github.com/jeremiah-Sakuda/sankofa) established the warm storyteller and the invitation to bring a question. SankofaVR is a separate Unity implementation and imports no web code, backend or media. It adds a theatre that unfolds in front of you, hands-only perspective choices that physically turn the stage, participation in the gourd scene, optional local table fitting and offline replay.

## What's next

The next work is listening, cultural and first-time visitor review, then more stories only where we can source and review them properly. Target public launch: **confirm date with the entrant before publishing**.

## Judge walkthrough

Start seated. Set the controllers aside and choose the **gourd**. At the perspective choice, follow the woman who brought less. At **Set the gourd down**, use the gourd or its labeled action. Finish and Keep the telling. Start the gourd again and choose the woman who expected more. The stage will turn to her side. Sources, captions, pause, recenter and the virtual stand remain available. No login to a story service is required.

## Private release notes — exclude from public copy

- Active [APK identity](candidate.md): pop-up build from `90891cd`, verified local Android Debug signature; release signing/version and Competition-channel acceptance are pending.
- The actual device pass, independent install through the judge link, permission recovery, real frame rate, human listening, cultural/source review and rights clearance are pending. Remove or qualify device claims that cannot be established.
- Current test totals belong in the candidate record, separate from the public pitch. The older Unity HTTP smoke demonstrates an optional transport only. Hosted story generation is outside the proposed judged path.
- Still needed: authenticated Devpost draft link, active Meta Start/dashboard access, confirmed launch plan, final Competition link, and public YouTube/Vimeo link to a demo under three minutes. No desktop synthetic motion study is eligible as headset/simulator footage.
- See the [release handoff](access-handoff.md) and [official rules](https://start-developer-competition-26.devpost.com/rules). Target submission November 16; hard close November 18, 3 p.m. Eastern / noon Pacific.
