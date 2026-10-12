# Look, ask and act: response audit

October 12, 2026 · Sprint 1, S1-03 · Code and test audit of how the illustrated episode answers each kind of visitor participation, against PRD SK-08 (attention is an invitation; only a deliberate hand action commits). Software evidence only: no headset, hand-tracking or participant observation.

## Findings

| Channel | What should happen | What happened before this audit | Now |
|---|---|---|---|
| **Look** at a person | After 1.4 s of facing her, a question about her glows and she turns toward the visitor | **Never fired for people, in either presentation.** Attention was centred on each object's transform, which for a person is the ground at her feet. Facing her body fell outside the tracker's allowance for all four women; the spring failed by 12° | Attention centres on the middle of what shows of the object (`StageObject.AttentionCentre`). The painted figure turns up to 15° toward the visitor and her light lifts, then she settles back |
| **Look** at a thing | Its question glows; the nearest person follows the look to it | Questions glowed for things (their transforms happened to be near their visible centre); only 3D people could follow the look | The nearest person, 3D or painted, follows the look |
| **Look**, remembered | What was faced most rises from the fire at home and opens the continuation | Worked, but people could rarely be among the things remembered, for the reason above | People can now be remembered |
| **Ask** | A question by hand interrupts the fire, which answers and resumes | Works in both presentations. Questions step aside during touch and receive invitations. Two questions are gated until after the agreement is revealed | Unchanged |
| **Act** (receive, touch, offer) | The gesture or its button resolves the invitation; the illustration changes; the nearest person acknowledges the visitor | Painted poses changed by the response map, live and on replay. **The acknowledgement did nothing in the painted scene** (it looked only for 3D figures) | The nearest painted person turns to the visitor |
| **Wait** | The passive line plays; nothing claims the visitor chose | Works: passive poses differ (Wambui sets the flour down; the hide shows the passive response). Head direction never resolves an invitation | Unchanged |

Looking never advances the story or accepts an invitation in any path; a look only highlights, turns a head, or is remembered for presentation.

## Tests added

- `AttentionSceneTests.FacingTheMiddleOfWhatIsSeenNoticesIt` (EditMode, both scenes): facing the visible centre of every noticeable object counts as facing it, by the tracker's own rule. It failed before the fix for all four women and the spring, and passes after.
- `IllustratedTheatreTests.PaintedPeopleTurnToTheVisitorAndFollowTheirLook` (PlayMode): a looked-at painted woman turns toward the visitor and back; the nearest one follows a look to a gourd; an acknowledgement turns her; pause holds the turn.

## Still open

- **A painted "looking at you" pose.** Turning the card is an interim acknowledgement. A fourth pose row in each cast atlas (head toward the viewer) would read more clearly; it waits on the art provider decision and the family's notes (S2-02).
- **Only the first question about a person glows** when she is looked at (Naserian has two). Acceptable: the other remains on the panel.
- **Head direction is generous.** The allowance is the object's angular radius plus 7°. That is right for a hint and wrong for anything consequential, which is why nothing consequential uses it. Whether it feels attentive or twitchy can only be judged in a headset (S4-01).
- **The sheep.** It can be noticed, but it reads poorly between the women (S2-01).
