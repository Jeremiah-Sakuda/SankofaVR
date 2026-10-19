"""Request and response validation independent of HTTP and model providers."""

from copy import deepcopy
from dataclasses import dataclass
import json
import re
import uuid

from .content import ACCOUNT_DISCLOSURE, ART_IDS, DISCLOSURE, HISTORICAL, INTERESTS, PERIOD, REGION, SCHEMA, SOURCES, UNANSWERED


class RequestError(ValueError):
    def __init__(self, code, message, status=400):
        super().__init__(message)
        self.code, self.message, self.status = code, message, status


class ProposalError(ValueError):
    pass


def strict_json(raw):
    if isinstance(raw, (bytes, bytearray)):
        raw = raw.decode("utf-8")
    depth, quoted, escaped = 0, False, False
    for char in raw:
        if quoted:
            if escaped:
                escaped = False
            elif char == "\\":
                escaped = True
            elif char == '"':
                quoted = False
        elif char == '"':
            quoted = True
        elif char in "[{":
            depth += 1
            if depth > 16:
                raise ValueError("JSON nesting exceeds the supported contract")
        elif char in "]}":
            depth -= 1
    def pairs(items):
        result = {}
        for key, value in items:
            if key in result:
                raise ValueError("Duplicate JSON property")
            result[key] = value
        return result
    try:
        return json.loads(raw, object_pairs_hook=pairs, parse_constant=lambda _: (_ for _ in ()).throw(ValueError("Non-finite JSON value")))
    except (RecursionError, UnicodeError):
        raise ValueError("Invalid JSON encoding or nesting") from None


def text(value, limit, *, empty=False):
    if not isinstance(value, str) or (not empty and not value.strip()):
        raise ValueError("Invalid text length or type")
    if any((ord(c) < 32 and c not in "\r\n\t") or 0xD800 <= ord(c) <= 0xDFFF or 0x202A <= ord(c) <= 0x202E or 0x2066 <= ord(c) <= 0x2069 for c in value):
        raise ValueError("Control characters are unsupported")
    if len(value.encode("utf-16-le")) // 2 > limit:
        raise ValueError("Invalid text length")
    # Unity text must be plain; provider/user material cannot inject rich text.
    if any(c in value for c in "<>`[]") or re.search(r"(?:https?|javascript|data):", value, re.I):
        raise ValueError("Markup is unsupported")
    return value.strip()


@dataclass(frozen=True)
class StoryRequest:
    region: str
    period: str
    interest: str
    fragment: str = ""
    question: str = ""

    @classmethod
    def parse(cls, data):
        if not isinstance(data, dict) or set(data) - {"region", "period", "interest", "fragment", "question"}:
            raise RequestError("invalid_request", "Expected only region, period, interest, fragment and question.")
        try:
            region = text(data.get("region"), 80)
            period = text(data.get("period"), 80)
            interest = text(data.get("interest"), 80)
            fragment = text(data.get("fragment", ""), 1000, empty=True)
            question = text(data.get("question", ""), 500, empty=True)
        except ValueError:
            raise RequestError("invalid_request", "Use plain text; fragment is limited to 1000 characters and question to 500.") from None
        if region.casefold() not in {REGION, "nairobi", "enkare nairobi", "nairobi, kenya"}:
            raise RequestError("unsupported_region", "Only Enkare Nairobi is supported. Choose a listed context.", 422)
        if period.casefold() not in {PERIOD, "before the railway"}:
            raise RequestError("unsupported_period", "Only the 1850s / before the railway context is supported.", 422)
        if interest not in INTERESTS:
            raise RequestError("unsupported_interest", "Choose journeys, trade-household or place-water.", 422)
        return cls(REGION, PERIOD, interest, fragment, question)


def proposal_schema():
    chapter = {"type": "object", "properties": {
        "title": {"type": "string"}, "narration": {"type": "string"},
        "illustration": {"type": "string", "enum": ["scenery-atlas", "cast-atlas"]},
    }, "required": ["title", "narration", "illustration"], "additionalProperties": False}
    return {"type": "object", "properties": {
        "title": {"type": "string"},
        "chapters": {"type": "array", "minItems": 2, "maxItems": 2, "items": chapter},
    }, "required": ["title", "chapters"], "additionalProperties": False}


def validate_proposal(data):
    """Structural/capability guard, not semantic or independent fact verification."""
    try:
        if not isinstance(data, dict) or set(data) != {"title", "chapters"}:
            raise ValueError()
        text(data["title"], 100)
        identity = re.compile(r"your ancestor|your bloodline|your tribe|your lineage|your family lived|your family came from|your grandmother was|your grandfather was|you descend from|this proves your|historically verified|verified by ai", re.I)
        if identity.search(data["title"]):
            raise ValueError()
        chapters = data["chapters"]
        if not isinstance(chapters, list) or len(chapters) != 2:
            raise ValueError()
        for chapter in chapters:
            if not isinstance(chapter, dict) or set(chapter) != {"title", "narration", "illustration"}:
                raise ValueError()
            text(chapter["title"], 100)
            if identity.search(chapter["title"]):
                raise ValueError()
            narration = text(chapter["narration"], 1000)
            if not narration.startswith(DISCLOSURE) or chapter["illustration"] not in {"scenery-atlas", "cast-atlas"}:
                raise ValueError()
            # Reject obvious capability/identity laundering. These checks cannot
            # establish that all meaning in generated prose is culturally sound.
            if identity.search(narration) or re.search(r"https?://|www\.|\d", narration, re.I):
                raise ValueError()
            # The available illustrations show an outdoor bank. This is a bounded
            # known-mismatch guard, not proof of complete visual/semantic continuity.
            if re.search(r"\b(door|doorway|indoors|interior|inside|dwelling|homestead|courtyard|marketplace|building)\b", narration, re.I):
                raise ValueError()
        return deepcopy(data)
    except (ValueError, KeyError, TypeError):
        raise ProposalError("Provider proposal failed validation") from None


def bundled_proposal(interest):
    # Fixed authored fallback does not incorporate or claim to interpret input.
    passages = {
        "journeys": ("A path to the water", "A woman has reached the water before her travelling companion. The path is still empty. If she leaves now, she can reach home before dark. She lifts her bundle, takes a step, then looks back toward the bend.", "Room for another", "She chooses to put the bundle down. At last the missing woman comes round the bend, holding a gourd close. 'I thought you would have gone.' The waiting woman moves her bundle aside. 'I nearly did.' When they rise, the path home is darker, but neither follows it alone."),
        "trade-household": ("A household's measure", "A woman tips her flour gourd. Less than she thought. A portion still waits beside the hearth. She leaves it there and carries the lighter gourd to a meeting, wondering how to explain the shortfall.", "An agreement to carry", "'This is all I could bring.' She opens the gourd and waits. The other woman draws her bundle back, then sets a smaller share between them. They choose the smaller exchange together. Back at the hearth, the gourd rests beside the portion she did not take. Neither is full. Together, they are enough for tonight."),
        "place-water": ("The sound before the name", "A woman brings a traveller to the bank she remembers. 'Here.' The traveller points to the next bend. 'You said the path opened beside the reeds.' The woman stops beside her bundle. She had promised to show a place. Now she is no longer sure she has found it.", "A place to return to", "She chooses not to pretend. 'I remember the water. I may have the bend wrong.' Instead of pointing, she describes the sound she remembers. The traveller sits beside her to listen. The bend remains uncertain, but the memory is no longer carried by one voice alone."),
    }
    a, b, c, d = passages[interest]
    return {"title": a, "chapters": [
        {"title": a, "narration": DISCLOSURE + b, "illustration": "scenery-atlas"},
        {"title": c, "narration": DISCLOSURE + d, "illustration": "cast-atlas"},
    ]}


def contextual_question(question):
    """Offer known context, without claiming arbitrary questions were answered."""
    folded = question.casefold()
    if re.search(r"\b(gourd|vessel)\b", folded):
        claim, label = "C7", "Context for your question: the gourd"
    elif re.search(r"\b(trade|trading|household)\b", folded):
        claim, label = "C6", "Context for your question: regional trade"
    elif ("nairobi" in folded and re.search(r"\b(name|meaning|mean)\b", folded)) or "cool water" in folded:
        claim, label = "C1", "Context for your question: Nairobi's name"
    else:
        if re.search(r"\b(ancestor|ancestors|ancestry|grandmother|grandfather|bloodline|genealogy|biography|lineage)\b", folded):
            return {"id": "visitor-question", "label": "Where the evidence stops", "answer": UNANSWERED, "truth": "reconstructed", "sources": []}
        return {"id": "visitor-question", "label": "What came from my question?", "answer": DISCLOSURE + "Your question guided a fictional possibility in these pages. The people and events are invented. They are not a recovered account or a verified answer about your family's past.", "truth": "reconstructed", "sources": []}
    return {"id": "visitor-question", "label": label, "answer": HISTORICAL[claim], "truth": "historical", "sources": [claim]}


def compose_story(request, proposal, mode):
    proposal = validate_proposal(proposal)
    if mode not in {"generated", "bundled"}:
        raise ProposalError("Invalid story mode")
    imagined = [{"id": f"imagined-{index + 1}", **item, "truth": "reconstructed", "sources": [], "questions": []} for index, item in enumerate(proposal["chapters"])]
    chapters = [
        imagined[0],
        {"id": "context-water", "title": "The name of the water", "narration": HISTORICAL["C1"], "illustration": "scenery-atlas", "truth": "historical", "sources": ["C1"], "questions": []},
        {"id": "context-trade", "title": "A connected region", "narration": HISTORICAL["C6"], "illustration": "scenery-atlas", "truth": "historical", "sources": ["C6"], "questions": []},
        imagined[1],
    ]
    chapters[-1]["questions"].append({"id": "museum-gourd", "label": "What is documented about the gourd?", "answer": HISTORICAL["C7"], "truth": "historical", "sources": ["C7"]})
    # A bundled fallback is deliberately complete and never presented as a new
    # personal generation. Do not attach the fragment to a failed generation.
    if mode == "generated" and request.question:
        chapters[-1]["questions"].append(contextual_question(request.question))
    story = {"schema": SCHEMA, "id": "personal-" + uuid.uuid4().hex, "title": proposal["title"], "region": request.region, "period": request.period, "interest": request.interest, "mode": mode, "chapters": chapters, "sources": [deepcopy(SOURCES[key]) for key in ("C1", "C6", "C7")]}
    validate_story(story)
    return story


def validate_story(story):
    """Validate source identity plus exact historical inventory, never model labels."""
    if story["schema"] != SCHEMA or story["region"] != REGION or story["period"] != PERIOD or story["interest"] not in INTERESTS or story["mode"] not in {"generated", "bundled"}:
        raise ProposalError("Invalid story coverage")
    if story["sources"] != [SOURCES[key] for key in ("C1", "C6", "C7")]:
        raise ProposalError("Unapproved source metadata")
    ids = set()
    for chapter in story["chapters"]:
        if chapter["id"] in ids or chapter["illustration"] not in ART_IDS:
            raise ProposalError("Invalid chapter identity or illustration")
        ids.add(chapter["id"])
        for item, prose in [(chapter, chapter["narration"])] + [(q, q["answer"]) for q in chapter["questions"]]:
            refs = item["sources"]
            if any(ref not in SOURCES for ref in refs):
                raise ProposalError("Unknown source reference")
            if item["truth"] == "historical":
                if len(refs) != 1 or prose != HISTORICAL[refs[0]]:
                    raise ProposalError("Historical wording must match the reviewed inventory")
            elif item["truth"] == "reconstructed":
                if refs or (not prose.startswith(DISCLOSURE) and prose != UNANSWERED):
                    raise ProposalError("Reconstruction must be disclosed")
            elif item["truth"] == "personal-account":
                if refs or not prose.startswith(ACCOUNT_DISCLOSURE):
                    raise ProposalError("Personal account must be disclosed")
            else:
                raise ProposalError("Unknown truth label")
