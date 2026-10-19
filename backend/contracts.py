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
    def pairs(items):
        result = {}
        for key, value in items:
            if key in result:
                raise ValueError("Duplicate JSON property")
            result[key] = value
        return result
    return json.loads(raw, object_pairs_hook=pairs, parse_constant=lambda _: (_ for _ in ()).throw(ValueError("Non-finite JSON value")))


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
        chapters = data["chapters"]
        if not isinstance(chapters, list) or len(chapters) != 2:
            raise ValueError()
        for chapter in chapters:
            if not isinstance(chapter, dict) or set(chapter) != {"title", "narration", "illustration"}:
                raise ValueError()
            text(chapter["title"], 100)
            narration = text(chapter["narration"], 1000)
            if not narration.startswith(DISCLOSURE) or chapter["illustration"] not in {"scenery-atlas", "cast-atlas"}:
                raise ValueError()
            # Reject obvious capability/identity laundering. These checks cannot
            # establish that all meaning in generated prose is culturally sound.
            if re.search(r"https?://|www\.|\d|your ancestors|your ancestor was|your bloodline|your tribe|your lineage|your family lived|your family came from|your grandmother was|your grandfather was|you descend from|this proves your|historically verified|verified by ai", narration, re.I):
                raise ValueError()
        return deepcopy(data)
    except (ValueError, KeyError, TypeError):
        raise ProposalError("Provider proposal failed validation") from None


def bundled_proposal(interest):
    # Fixed authored fallback does not incorporate or claim to interpret input.
    passages = {
        "journeys": ("A path to the water", "A traveller pauses where a path meets the water. There is no map in this imagined moment, only a bundle set down and a decision to wait.", "Room for another", "Another traveller arrives. Neither asks for a family name. A space opens beside the first bundle, and the journey briefly becomes company."),
        "trade-household": ("A household's measure", "A trader sets a bundle down, then hesitates. She has brought less than she hoped. The other woman watches her hands before looking at the bundle.", "An agreement to carry", "The women make room for a smaller exchange. Their agreement belongs to these fictional people; it is not a recovered account of a historical meeting."),
        "place-water": ("The sound before the name", "Water moves beyond two travellers. One pauses to listen while the other lowers a bundle. Their invented meeting begins with shared attention.", "A place to return to", "When the travellers rise, the water keeps moving. One looks back toward the place where they sat. The place has become a memory without becoming proof of an event."),
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
        return {"id": "visitor-question", "label": "Your question: evidence limit", "answer": UNANSWERED, "truth": "reconstructed", "sources": []}
    return {"id": "visitor-question", "label": label, "answer": HISTORICAL[claim], "truth": "historical", "sources": [claim]}


def compose_story(request, proposal, mode):
    proposal = validate_proposal(proposal)
    if mode not in {"generated", "bundled"}:
        raise ProposalError("Invalid story mode")
    chapters = [{"id": "context-water", "title": "The name of the water", "narration": HISTORICAL["C1"], "illustration": "scenery-atlas", "truth": "historical", "sources": ["C1"], "questions": []}]
    chapters.append({"id": "context-trade", "title": "A connected region", "narration": HISTORICAL["C6"], "illustration": "scenery-atlas", "truth": "historical", "sources": ["C6"], "questions": []})
    for index, item in enumerate(proposal["chapters"]):
        chapters.append({"id": f"imagined-{index + 1}", **item, "truth": "reconstructed", "sources": [], "questions": []})
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
