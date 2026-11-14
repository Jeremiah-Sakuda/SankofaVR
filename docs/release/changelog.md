# Sankofa on Quest: what was added during the competition window

November 14, 2026. For the Adapted / Significantly Updated division, which asks what is new since the pre-existing project. The pre-existing project is the web app [Sankofa](https://devpost.com/software/sankofa-y47f9p) of March 2026. Everything below was built after the window opened on September 24, 2026, in a Unity project created on September 30 that shares no code, backend or media with the web app. Its only earlier material is general Quest tooling the entrant ported from another project of their own (a poke panel and gesture, a JSON reader, build and setup scripts), listed in [workspace provenance](../workspace-provenance.md). Dates are from the [decision log](../decisions.md). The entry may quote or link this page.

"Software-checked" means automated tests and desktop renders. No headset was available, so nothing here is a claim about a device. The November 14 items were written without the Unity editor and are listed on the understanding that they pass the [verification list](../testing/2026-11-14-unity-verification.md) before the build is made; remove any that does not.

| When | Added | State |
|---|---|---|
| September 30 – October 4 | A new Quest application: seated, hands only, a griot present as a fire. A story core with no engine dependency, tested outside Unity | Software-checked |
| October 4 | The first setting, Enkare Nairobi around the 1850s, with sourced historical passages kept apart from imagined scenes | Sources registered; cultural review open |
| October 18–19 | The story table: three prepared tellings chosen by interest or by a typed question, measured captions, questions with sources, pause and comfort options, and an explicit Keep, reopen and Forget | Software-checked |
| October 26 | Two sides to every telling: the visitor chooses whose side of a meeting to follow, and the ending says honestly who chose | Software-checked |
| October 30 | The pop-up paper stage: painted scenery and figures stand up from the table, perform each page and turn to the chosen side. In the gourd's telling the visitor can set the gourd down | Software-checked |
| November 10 | The griot's spoken welcome, pages that turn themselves when told, a strand of six beads for the sides kept across visits, and a return greeting that names a side not yet heard | Software-checked; nobody has listened |
| November 14 | Both sides of a meeting in one sitting; a hand swept across the stage turns it; a hand closed on an heirloom begins its telling; the story set on the visitor's own table from the room scan or from a resting hand; no permission asked for until the visitor chooses that; short sounds for the stage as it folds and stands | Written and type-checked; not yet run in Unity, and the new sounds not yet listened to |

Earlier presentations of the same story (a hearth, an illustrated theatre and a gallery, October 3–15) remain in the repository as comparison scenes. They are not in the judged build.

Not added, and not claimed: live conversation with a model, generated stories or images at runtime in the judged build, speech recognition, a second language, or any account of a visitor's own ancestry. A spoken question to the fire exists in the code behind a switch and is off.
