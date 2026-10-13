# Modular architecture

September 30, 2026 · Implementation contract. The story core, application flow, gesture logic, Unity presentation and editor tooling exist for the first slice. It plays the draft Enkare Nairobi package; see the [development guide](development.md#implementation-layout-and-checks). A local file store for kept visits implements the persistence contract. Narration is rendered offline by a production script, not by a runtime adapter. Runtime provider and content-service adapters do not exist yet.

Sankofa should support different reviewed stories, languages and service providers without rebuilding the visitor journey. The [PRD](product/prd.md) defines behaviour and acceptance; the [delivery plan](product/delivery-plan.md) sequences delivery. Competition strategy stays in the ignored local brief described in [development](development.md#local-competition-context).

## Responsibilities and dependencies

| Module | Owns | Boundary |
|---|---|---|
| Story core | Intent, claims, capabilities, validated plans, beat state, legal actions, checkpoints and replay records | Plain data and deterministic logic; no Unity, network, provider SDK or file-system dependency |
| Application flow | Welcome → preparation → story → return, consent, cancellation, timeouts and fallback selection | Calls core logic and narrow adapter contracts; owns accepted state changes, never model prose |
| XR input and presentation | Hands/head-direction input, accessibility controls, fire, scenes, audio, captions and stationary transitions | Translates input into intent and renders accepted events; no embedded provider calls, historical claims or episode-specific branching rules |
| AI adapters | Planning, narration and optional live conversation | Translate provider formats into shared contracts; planning, TTS and live conversation can be selected independently; credentials stay server-side |
| Content packages | Reviewed context, claim/source links, scene capabilities, assets, locale coverage, captions and bundled story | Versioned data selected by stable IDs; no executable model-generated scripts or arbitrary asset URLs |
| Persistence adapter | Opt-in saved visits, realised narration, keepsakes, deletion and schema migration | Implements storage contracts; preserves replay without new inference and keeps continuation separate |
| Production and release tooling | Asset import, provenance, optimisation, build selection and evidence collection | Runs outside the visitor experience; private strategy and raw research are excluded from published artifacts |

Dependency direction: core depends on no outer module. Application flow depends on core and its contracts. Provider, storage and XR adapters depend on those contracts; the composition point wires implementations together. The Unity host may perform composition, but scene scripts must not reach into provider SDKs or storage internals. Content is validated before use. Logical separation does not require one deployed service per module.

## Contracts to introduce with the first vertical slice

- **Story request:** confirmed context, interest and locale, supported coverage, consent and a request ID.
- **Content manifest:** package/version IDs, approved claims and assets, supported actions, narration/live-language capabilities and provenance references.
- **Plan proposal:** source-linked beats, referenced asset IDs, permitted actions and an ending. An unvalidated proposal is never playable state.
- **Accepted story events:** state/version, visit and beat IDs, cues and resolved choices. Presentation consumes these; model suggestions cannot bypass them.
- **Narration/conversation result:** linked request/visit/beat IDs, locale, text/caption/audio references and cancellation status. Late results cannot resurrect an interrupted beat.
- **Saved visit:** schema/content versions, realised plan and permitted replay media. Missing content or incompatible versions require an explicit recovery path.

Keep contracts small, serialisable and versioned at persistence/service boundaries. Deterministic tests use synthetic records and fake clocks/adapters. Add concrete schemas and interfaces when they carry actual behaviour; this document does not authorise a framework build or unneeded plugin system.

## Replacement and failure tests

| Change or failure | Expected independence |
|---|---|
| Replace a model/TTS vendor | Adapter/configuration changes; core plans, scene rendering and saved replay still work |
| Add a reviewed setting or narration language | New content package and coverage entries; shared visitor flow remains; language support is evaluated independently |
| No network or microphone permission | Local hands and complete bundled story remain available; fallback is disclosed |
| Swap simulator input for Quest hands | Input adapter changes; accepted story actions keep their meaning |
| Replace rigged figures with illustrated theatre | A content layout selects cards, poses and tangible props; accepted story events, hand gestures, storage and replay retain their meaning |
| Enable or remove room-aware MR | Placement adapter changes; full VR remains usable and story logic is unchanged |
| Interruption, timeout or out-of-order response | Application invalidates request; stale cues never reach the active scene |
| Replay after service changes | Saved realised content plays; a new generation is never represented as an exact replay |

These are implementation acceptance intentions, not completed checks. For each substantial change, record its owning module, affected requirement, observable behaviour and relevant evidence. A feature that crosses boundaries should have one owner and explicit contracts, not duplicate state in several scenes.

The illustrated-theatre MVP implements that presentation replacement for one historical chapter. `IllustratedFigure` owns painted poses and the origin of a physical gift. `IllustratedTheatre` consumes resolved invitations to select cast responses and scenic emphasis. Layout and response mappings are authored content; neither renderer accepts a story choice. `SankofaApp` sends the same resolved outcome during a live visit and replay. Its local shelf uses a separate namespace from the baseline scene. The scene builder composes this version from the existing foundation without regenerating or replacing the baseline scene.

## Selected gallery direction

October 13: the [living gallery scope](product/illustrated-theatre-scope.md) makes gallery preview, explicit exhibit entry and reframed return the outer journey. The `LivingGallery` candidate now implements that journey with the v0.4.0 authored package; the older theatre remains a comparison. Software checks do not establish device or cultural acceptance.

For the current single exhibit, the editor builder composes title/placard and the canonical scene snapshot; `LivingGallery` holds its two route IDs and presentation references. The story package owns the routes and continuation eligibility. There is no general exhibit catalogue schema yet. The gallery uses the existing kept-visit adapter and illustrated shelf namespace; it does not own a duplicate story engine or store. Application flow owns entry, route/default provenance, cancellation, closing and save. Presentation owns the correspondence between framed composition and spatial layers. Narration/validated content own what context is safe at each revealed beat; the renderer never decides that attention authorises a spoiler or choice. The first artwork uses fire as the griot's visible presence.

Preserve old saved route/package identities when mapping them to the exhibit. A canonical painting is independent of the visitor's saved memento. No catalogue backend, network service, locomotion system or additional artwork is implied by this boundary.

## Competition context is removable

The tracked agent entry point requires a local competition brief during development. The product runtime never reads that file, and the file is not a feature flag or deployed model prompt. Durable product requirements such as accessible interaction, reliable recovery and accurate content live in the PRD. Prize strategy, current scoring and submission notes remain in the local brief. Changing a future competition profile must not require rewriting the story engine.
