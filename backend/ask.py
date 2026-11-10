"""Ask the fire: one bounded question about the page a visitor is on.

Request checks, the authored fallback and answer acceptance, independent of HTTP
and model providers. Every evidence label is decided here from content; a model's
own label is never trusted. No question, recording or answer is stored or logged.
"""

import base64
import binascii
from dataclasses import dataclass
import re
import struct

from .content import ANSWER_SCHEMA, DISCLOSURE, HISTORICAL, SOURCES, UNKNOWN_STORY
from .contracts import ProposalError, RequestError, text

MAX_QUESTION_UNITS = 500
MAX_ANSWER_WORDS = 60
MAX_ANSWER_UNITS = 600
AUDIO_RATE = 16000
MAX_AUDIO_SECONDS = 12
MAX_AUDIO_DATA_BYTES = AUDIO_RATE * 2 * MAX_AUDIO_SECONDS
# A little room for the header and a recorder's own note; PersonalAnswerAudio allows the same.
MAX_WAV_BYTES = MAX_AUDIO_DATA_BYTES + 256
MAX_AUDIO_BASE64 = 4 * ((MAX_WAV_BYTES + 2) // 3)
TRUTHS = ("historical", "reconstructed", "unknown")
AUDIO_MESSAGE = "A recorded question is base64 16 kHz mono 16-bit PCM WAV of at most twelve seconds."

# Assertions about the visitor, not mentions: an honest "I cannot speak of your ancestors" passes.
# PersonalAnswerValidator refuses the same phrases.
IDENTITY_ASSERTIONS = (
    "your ancestor was", "your ancestors were", "your ancestors lived", "your ancestors came", "your family lived", "your family came from",
    "your grandmother was", "your grandfather was", "you descend from", "you are descended from", "your lineage is", "your bloodline",
    "your tribe", "this proves your", "historically verified", "verified by ai",
)


@dataclass(frozen=True)
class Inquiry:
    """A checked question. ``pages`` are the chapters read to reach the current one, in order."""
    story: dict
    chapter: dict
    pages: tuple
    question: str = ""
    audio: bytes = b""


def ask_fields(data):
    """Shape and bounds only: (story, token or None, chapter_id, question, audio)."""
    if not isinstance(data, dict) or set(data) - {"story", "token", "chapter_id", "question", "audio"} or not isinstance(data.get("story"), dict) or not isinstance(data.get("chapter_id"), str) or not isinstance(data.get("token", ""), str):
        raise RequestError("invalid_request", "Supply a story, its chapter_id, the story token when one was issued, and one question or recording.")
    if ("question" in data) == ("audio" in data):
        raise RequestError("invalid_request", "Send exactly one of question or audio.")
    question, audio = "", b""
    if "question" in data:
        try:
            question = text(data["question"], MAX_QUESTION_UNITS)
        except ValueError:
            raise RequestError("invalid_question", "A question is plain text of at most 500 characters.") from None
    else:
        audio = question_audio(data["audio"])
    return data["story"], data.get("token") or None, data["chapter_id"], question, audio


def question_audio(value):
    """Decode and check a recording. It is held in memory for one provider call and never written anywhere."""
    if not isinstance(value, str) or not value:
        raise RequestError("invalid_audio", AUDIO_MESSAGE)
    if len(value) > MAX_AUDIO_BASE64:
        raise RequestError("audio_too_large", AUDIO_MESSAGE, 413)
    try:
        raw = base64.b64decode(value, validate=True)
        size = _wav_data_bytes(raw)
    except (binascii.Error, ValueError, struct.error):
        raise RequestError("invalid_audio", AUDIO_MESSAGE) from None
    if size > MAX_AUDIO_DATA_BYTES or len(raw) > MAX_WAV_BYTES:
        raise RequestError("audio_too_large", AUDIO_MESSAGE, 413)
    return raw


def _wav_data_bytes(raw):
    if len(raw) < 44 or raw[:4] != b"RIFF" or raw[8:12] != b"WAVE":
        raise ValueError()
    position, described = 12, False
    while position + 8 <= len(raw):
        name, size = raw[position:position + 4], struct.unpack_from("<I", raw, position + 4)[0]
        body = position + 8
        if size > len(raw) - body:
            raise ValueError()
        if name == b"fmt ":
            if size < 16:
                raise ValueError()
            encoding, channels, rate, _, block, bits = struct.unpack_from("<HHIIHH", raw, body)
            if (encoding, channels, rate, block, bits) != (1, 1, AUDIO_RATE, 2, 16):
                raise ValueError()
            described = True
        elif name == b"data":
            if not described or size < 2 or size % 2:
                raise ValueError()
            return size
        position = body + size + (size & 1)
    raise ValueError()


def revealed_pages(story, chapter_id):
    """The chapters read to reach ``chapter_id``: nothing after it, and nothing from a side not followed.

    Mirrors PersonalStoryRoutes: a chapter with choices leads to a chosen chapter, a chapter a choice leads to ends
    the telling, and any other chapter continues to the next. Returns None when the chapter cannot be reached.
    """
    chapters = story["chapters"]
    index = {chapter["id"]: i for i, chapter in enumerate(chapters)}
    targets = {choice["next"] for chapter in chapters for choice in chapter.get("choices", ())}

    def walk(i, path):
        chapter, path = chapters[i], path + (chapters[i],)
        if chapter["id"] == chapter_id:
            return path
        if chapter.get("choices"):
            following = [index[choice["next"]] for choice in chapter["choices"]]
        elif chapter["id"] in targets or i + 1 >= len(chapters):
            following = []
        else:
            following = [i + 1]
        for nearer in following:
            found = walk(nearer, path) if nearer > i else None
            if found:
                return found
        return None

    return walk(0, ()) if chapters else None


def passages(story):
    """The approved passages a reply about this telling may use: those of the sources the telling itself declares."""
    return {source["id"]: HISTORICAL[source["id"]] for source in story["sources"]}


def answer_schema():
    return {"type": "object", "properties": {
        "answer": {"type": "string"},
        "truth": {"type": "string", "enum": list(TRUTHS)},
        "sources": {"type": "array", "maxItems": 3, "items": {"type": "string", "enum": sorted(SOURCES)}},
    }, "required": ["answer", "truth", "sources"], "additionalProperties": False}


_STOPWORDS = frozenset("""a about am an and any are as at be been by can could did do does for from had has have he her hers him his how i if in
into is it its me my no not of on or our she so some tell than that the their them then there these they this those to us was we were
what when where which who whom whose why will with would you your""".split())


def _words(value):
    """Lower-case content words, without question words or a plural s. Used only to compare, never returned."""
    value = re.sub(r"['’]s\b", "", value.casefold())
    words = set()
    for word in re.findall(r"[a-z]+", value):
        if word in _STOPWORDS:
            continue
        if len(word) > 3 and word.endswith("s") and not word.endswith("ss"):
            word = word[:-1]
        words.add(word)
    return words


def authored_match(chapter, question):
    """The current page's authored question that shares the most words with what was asked, if one clearly does.

    A word in the authored question counts twice a word in its answer. No shared word, or a tie, is no match.
    """
    asked = _words(question)
    scores = []
    for item in chapter["questions"]:
        answer = item["answer"][len(DISCLOSURE):] if item["answer"].startswith(DISCLOSURE) else item["answer"]
        scores.append(2 * len(asked & _words(item["label"])) + len(asked & _words(answer)))
    best = max(scores, default=0)
    return chapter["questions"][scores.index(best)] if best > 0 and scores.count(best) == 1 else None


def bundled_reply(inquiry):
    """The complete offline form: one of this page's authored answers, or its authored questions to choose from."""
    item = authored_match(inquiry.chapter, inquiry.question) if inquiry.question else None
    if item:
        return {"schema": ANSWER_SCHEMA, "mode": "bundled", "kind": "answer", "question_id": item["id"], "answer": item["answer"], "truth": item["truth"], "sources": list(item["sources"])}
    return {"schema": ANSWER_SCHEMA, "mode": "bundled", "kind": "questions", "questions": [{"id": item["id"], "label": item["label"]} for item in inquiry.chapter["questions"]]}


def accept_answer(proposal, inquiry):
    """Turn a provider's proposal into a reply, or raise ProposalError.

    ``historical`` is given only to text that is exactly one approved passage of this telling, with that passage's
    source. Any other accepted text is ``reconstructed`` with no sources, whatever the provider claimed. A proposal
    marked ``unknown`` is replaced by the fixed reply. This bounds the form and the label; it does not establish
    that imagined wording is accurate or culturally sound.
    """
    try:
        if not isinstance(proposal, dict) or set(proposal) != {"answer", "truth", "sources"} or proposal["truth"] not in TRUTHS:
            raise ValueError()
        cited = proposal["sources"]
        if not isinstance(cited, list) or len(cited) > 3 or any(not isinstance(item, str) or item not in SOURCES for item in cited):
            raise ValueError()
        if proposal["truth"] == "unknown":
            return {"schema": ANSWER_SCHEMA, "mode": "generated", "kind": "answer", "answer": UNKNOWN_STORY, "truth": "unknown", "sources": []}
        answer = text(proposal["answer"], MAX_ANSWER_UNITS)
        if len(answer.split()) > MAX_ANSWER_WORDS:
            raise ValueError()
        for claim, passage in passages(inquiry.story).items():
            if answer == passage:
                return {"schema": ANSWER_SCHEMA, "mode": "generated", "kind": "answer", "answer": answer, "truth": "historical", "sources": [claim]}
        folded = answer.casefold()
        # New wording is imagined. Refuse what would read as a record: an approved passage of another telling,
        # figures and dates, addresses, and statements about the visitor's own family.
        if answer in HISTORICAL.values() or re.search(r"\d|www\.", folded) or any(phrase in folded for phrase in IDENTITY_ASSERTIONS):
            raise ValueError()
        return {"schema": ANSWER_SCHEMA, "mode": "generated", "kind": "answer", "answer": answer, "truth": "reconstructed", "sources": []}
    except (ValueError, KeyError, TypeError):
        raise ProposalError("Provider answer failed validation") from None
