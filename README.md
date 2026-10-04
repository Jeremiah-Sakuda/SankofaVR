# SankofaVR

**Sankofa — a story that notices what you notice.**

A proposed hands-first spatial storytelling experience for Meta Quest. A living fire welcomes you, listens to your interests, and brings a personalised history into the space around you. The fire is the griot. You enter through its flame and return to the hearth with a keepsake and a thread to follow next time.

**SankofaVR** is the repository/workspace name; **Sankofa** is the product name.

## Start here

| Document | Purpose |
|---|---|
| [Product requirements](Sankofa-PRD.md) | Vision, scope, journey, 18 user stories, requirements and acceptance criteria |
| [Delivery plan](Sankofa-Delivery-Plan.md) | Capacity, architecture, staged proofs and release evidence |
| [Backlog](docs/backlog.md) | Current work in dependency order |
| [Decisions](docs/decisions.md) | Confirmed direction, planning defaults and outstanding choices |
| [Development](docs/development.md) | Workspace setup, module layout and checks |
| [Unity application](unity/README.md) | The Quest project: commands, simulator, headset and what is unverified |
| [Modular architecture](docs/architecture.md) | Story core, adapters, content and presentation boundaries |
| [Product stress test](docs/reviews/2026-10-01-product-stress-test.md) | Challenged assumptions and proposed experiments; no validation claimed |
| [Asset register](docs/asset-register.md) | Provenance and acceptance records |
| [References](references/README.md) | Official competition and original web Sankofa sources |
| [Workspace provenance](docs/workspace-provenance.md) | Where the planning documents came from and what was transferred |

## Current status

The first full slice exists in software: a Unity project for Quest and an engine-free story core with .NET tests. It tells **Enkare Nairobi**, a draft story of the cool-water stream that gave Nairobi its name, across two eras and today. The [content](docs/content/enkare-nairobi/README.md) cites a source register.

At the hearth the fire welcomes the visitor and offers a choice of when to go. The visitor enters through the flame, without moving, into a scene where people appear as shapes of firelight. Hands carry, offer and touch things, which changes later lines. Questions on a panel by the right hand are answered, and then the story resumes. The griot's pre-rendered voice comes from the original web Sankofa's direction.

A visit can be kept by an explicit choice, replayed exactly, and continued to today. Passive progression, pause, early return and late-result handling are covered by tests.

Nothing has run on a headset, in the simulator with hands, or with a participant. The stories are unreviewed by family or community, and some claims are gated until verified. The second language is not selected. The planned entry is Adapted / Significantly Updated Entertainment. Weekly hours, budget and current headset access remain open. AI-generated music is excluded.

## Check the workspace

Python 3.10 or newer and Git are sufficient for the document check; the story core needs the .NET 10 SDK.

```sh
python3 scripts/check_workspace.py
git diff --check
dotnet test dotnet/Sankofa.Tests
```

The document check covers tracked Markdown links and local-only file protections. The .NET tests cover package and plan validation, the visit flow, kept visits and gesture logic. Neither validates a VR experience; Unity checks are in the [Unity README](unity/README.md).

## Local-only material

Full supplied rules, description, reference provenance and the original chat are copied into ignored `references/hackathon/` and `references/ideation/` folders in the initial workspace. They are not included in GitHub or fresh clones.

`HACKATHON.local.md` is the local competition brief. Every working agent must read it through the instructions in [AGENTS.md](AGENTS.md). It is excluded from Git and must remain outside application packaging. Follow [local competition setup](docs/development.md#local-competition-context) in a new checkout; the tracked workspace check can verify its presence with `--competition`.

Keep personal input, real recordings, credentials, raw vendor assets and generated builds out of Git. See [working instructions](AGENTS.md). This repository is private; no open-source licence has been selected. Reuse from the separate [web Sankofa project](https://github.com/Jeremiah-Sakuda/Sankofa) requires an explicit code/asset licensing decision.
