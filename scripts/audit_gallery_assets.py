#!/usr/bin/env python3
"""Verify declared input hashes and current narration fingerprints; never claims legal clearance."""
import hashlib,json,subprocess
from pathlib import Path
from generate_narration import DIRECTION,fingerprint,lines
ROOT=Path(__file__).resolve().parent.parent;ASSETS=ROOT/'unity/Assets/Sankofa'
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def main():
    entries=[];problems=[]
    def check(path,expected,kind,licence,source):
        actual=sha(path) if path.is_file() else None
        ok=actual==expected if expected else actual is not None
        entries.append({'path':str(path.relative_to(ROOT)),'kind':kind,'sha256':actual,'declared_sha256':expected,'hash_matches':ok,'rights_basis':licence,'source':source})
        if not ok:problems.append(str(path.relative_to(ROOT)))
    art=ASSETS/'Art/IllustratedTheatre';data=json.loads((art/'sources.json').read_text())
    for item in data['assets']:check(art/item['file'],item['sha256'],'generated illustration','OpenAI output/account terms unresolved','Art/IllustratedTheatre/sources.json')
    item=json.loads((art/'modern-river-v1.sources.json').read_text());check(art/item['asset'],item['sha256'],'generated illustration','OpenAI output/account terms unresolved','Art/IllustratedTheatre/modern-river-v1.sources.json')
    data=json.loads((ASSETS/'Art/Recorded/sources.json').read_text())
    for item in data['recordings']:check(ASSETS/'Art/Recorded'/item['file'],item['sha256'],'field recording',item['licence'],item['page'])
    data=json.loads((ASSETS/'Art/Scanned/sources.json').read_text())
    for item in data['assets']:
        for filename,info in item['files'].items():check(ASSETS/'Art/Scanned'/item['id']/filename,info['sha256'],'scanned texture',item['license'],item['page'])
    package=json.loads((ASSETS/'Content/living-gallery.json').read_text());manifests={};speech=[]
    for line in lines(package):
        namespace,key=line['audio'].split('/',1)
        if namespace not in manifests:manifests[namespace]=json.loads((ASSETS/'Content'/f'{namespace}.narration.json').read_text())
        m=manifests[namespace].get(key);clip=ASSETS/'Resources/Narration'/f"{line['audio']}.mp3"
        ok=m is not None and m['fingerprint']==fingerprint(line['text'],m['model'],m['voice'],m.get('direction',DIRECTION)) and clip.is_file()
        speech.append({'line':line['id'],'audio':line['audio'],'text_sha256':hashlib.sha256(line['text'].encode()).hexdigest(),'sha256':sha(clip) if clip.is_file() else None,'fingerprint_matches':ok,'model':m.get('model') if m else None,'voice':m.get('voice') if m else None})
        if not ok:problems.append(line['id'])
    report={'package_version':package['version'],'package_sha256':sha(ASSETS/'Content/living-gallery.json'),'source_commit':subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip(),'integrity_problems':problems,'inputs':entries,'current_narration':speech,'limits':['Integrity and provenance audit, not legal or cultural acceptance.','All declared recordings are checked, including legacy assets not used by this candidate.','Unity Resources contains legacy narration for saved visits; this list describes current package speech only.','Unity engine, Meta SDK and font distribution licences remain separate from generated-output rights.','Applicable user/provider account agreements and original project contributor permissions remain unconfirmed.']}
    out=ROOT/'artifacts/release/gallery-asset-integrity.json';out.parent.mkdir(parents=True,exist_ok=True);out.write_text(json.dumps(report,indent=2))
    print(f'{len(entries)} declared art/recording/texture inputs; {len(speech)} current speech lines; {len(problems)} integrity problems. Report: {out}')
    if problems:raise SystemExit(1)
if __name__=='__main__':main()
