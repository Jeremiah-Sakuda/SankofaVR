"""Bounded Enkare Nairobi context; community/cultural acceptance remains pending.

Only these exact historical passages may enter the personal-story response. They
mirror PersonalStoryCatalog in the engine-free core. No research files or private
development brief are read or sent to the provider.

authored_tellings.json holds the app's three prepared tellings exactly as
BundledPersonalStoryComposer writes them, so a question about one can be accepted
without a story token. The .NET suite rewrites and checks that file; it is never
edited by hand.
"""

import json
from pathlib import Path

SCHEMA = "sankofa.personal-story.v1"
REGION = "enkare-nairobi"
PERIOD = "1850s"
INTERESTS = ("journeys", "trade-household", "place-water")
ART_IDS = ("scenery-atlas", "cast-atlas", "modern-river")
HISTORICAL = {
    "C1": "Nairobi's name is commonly traced to a Maa phrase meaning cool waters. Spelling and pronunciation still need review by a Maa speaker.",
    "C6": "Before 1900, trade linked pastoral Maasai and predominantly agricultural Kikuyu communities around Kiambu; settlement and economic activity overlapped.",
    "C7": "A National Museums of Kenya exhibit describes a Maasai gourd used for milk or blood and smoked inside with oloirien wood. This museum example does not establish the design of every vessel used in the 1850s.",
}
# A passage as the griot tells it, where that is shorter than the passage: its first sentence, word for word.
# PersonalStoryCatalog.NamingClaim mirrors this. The review note stays with the whole passage for a visitor who asks.
TOLD = {
    "C1": "Nairobi's name is commonly traced to a Maa phrase meaning cool waters.",
}


def approved_forms(claim):
    """Every exact wording under which a claim may be labelled historical."""
    return (HISTORICAL[claim],) + ((TOLD[claim],) if claim in TOLD else ())


SOURCES = {
    "C1": {"id": "C1", "title": "Nairobi City County Assembly: clean drinking water motion", "url": "https://nairobiassembly.go.ke/motion/provision-of-free-clean-drinking-water-in-hotels-and-other-public-areas/"},
    "C6": {"id": "C6", "title": "John Overton (1988): The Origins of the Kikuyu Land Problem", "url": "https://www.cambridge.org/core/journals/african-studies-review/article/abs/origins-of-the-kikuyu-land-problem-land-alienation-and-land-use-in-kiambu-kenya-18951920/01BDC3BCB4D2A9AC40F00F52500F1916"},
    "C7": {"id": "C7", "title": "National Museums of Kenya: 28 Incredible Containers Used by Kenyan Communities", "url": "https://artsandculture.google.com/story/_gUx7TIiep4KKA"},
}
DISCLOSURE = "Imagined scene: "
ACCOUNT_DISCLOSURE = "Visitor-provided account, not independently verified: "
UNANSWERED = "Imagined scene: This telling cannot establish an answer to your question from its reviewed context. A personal account is not independent historical evidence."
ANSWER_SCHEMA = "sankofa.personal-answer.v1"
# The fire's fixed reply to a question outside this place and period. PersonalAnswer.UnknownReply mirrors it.
UNKNOWN_STORY = "The fire does not know that story. Tonight it keeps only the tellings of Enkare Nairobi, before the railway. Ask me about this one."


def _authored_tellings():
    with open(Path(__file__).with_name("authored_tellings.json"), encoding="utf-8") as source:
        document = json.load(source)
    if document.get("schema") != "sankofa.authored-tellings.v1":
        raise ValueError("Unsupported authored-tellings file")
    return {story["id"]: story for story in document["tellings"]}


AUTHORED_TELLINGS = _authored_tellings()


def coverage():
    return {
        "schema": SCHEMA,
        "regions": [{"id": REGION, "label": "Enkare Nairobi, Kenya", "periods": [PERIOD], "interests": list(INTERESTS)}],
        "language": "en",
        "review_status": "Draft sourced context; family, community and pronunciation review pending.",
        "capabilities": {"generated_text": True, "approved_illustrations": True, "generated_images": False, "generated_audio": False, "generated_answers": False, "spoken_questions": False, "live_conversation": False},
        "privacy": "This service stores no story requests, questions or recordings and logs no request content. Live generation sends the fragment and question to Google; a question asked of the fire, typed or recorded, is sent to Google with the pages read so far when answers are generated. Provider retention and applicable terms require separate review.",
    }
