#!/usr/bin/env python3
"""Verify declared input hashes and current narration fingerprints; never claims legal clearance."""
import hashlib,json,subprocess
from pathlib import Path
from generate_narration import DIRECTION,fingerprint,lines
ROOT=Path(__file__).resolve().parent.parent;ASSETS=ROOT/'unity/Assets/Sankofa'
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()

def inspect_file(path, expected, root=ROOT):
    """An observed digest is not a verified declaration when a manifest omitted its digest."""
    actual = sha(path) if path.is_file() else None
    entry = {'path': path.relative_to(root).as_posix(), 'sha256': actual,
             'declared_sha256': expected, 'exists': actual is not None,
             'hash_matches': actual == expected if expected else None}
    problem = actual is None or bool(expected and actual != expected)
    return entry, problem

def river_inventory(check, findings, problems):
    """Inputs declared by the painted-river builders, not an inspected final APK dependency list."""
    for relative in ('Art/RiverThreshold/sources.json', 'Art/RiverThreshold/objects.sources.json',
                     'Art/RiverCohesion/sources.json'):
        manifest = ASSETS / relative
        data = json.loads(manifest.read_text(encoding='utf-8'))
        assets = data.get('assets', [])
        if 'file' in data:
            assets = [data] + data.get('additional_assets', [])
        declaration = data.get('rights_status', data.get('review', data.get('review_status')))
        for item in assets:
            entry = check(manifest.parent / item['file'], item.get('sha256'), 'river generated illustration',
                          declaration or 'No release-rights declaration in manifest', relative)
            entry.update({'manifest': manifest.relative_to(ROOT).as_posix(),
                          'source_output': item.get('source'),
                          'reference_inputs': item.get('references', []),
                          'provider': data.get('provider'), 'tool': data.get('tool'),
                          'model': data.get('model'), 'terms_reference': data.get('terms_reference'),
                          'declared_rights_status': declaration,
                          'digest_baseline': data.get('digest_baseline'),
                          'scope': 'painted-river source inventory; cast-materials-v1 is the 3D comparison input'})
            if not item.get('sha256'):
                findings.append({'code': 'missing_declared_digest', 'path': entry['path'],
                                 'detail': 'Current bytes hashed, but source manifest has no digest to compare; source identity is not verified by this audit.'})

    relative = 'Art/Recorded/RiverThreshold/sources.json'
    manifest = ASSETS / relative
    for item in json.loads(manifest.read_text(encoding='utf-8'))['recordings']:
        entry = check(manifest.parent / item['file'], item.get('sha256'), 'river recorded foley', item['licence'], item['page'])
        entry.update({'manifest': manifest.relative_to(ROOT).as_posix(), 'title': item['title'],
                      'author': item['author'], 'licence_url': item.get('licence_url'), 'edit': item['edit'],
                      'source_file': item['source_file'], 'declared_source_sha256': item['source_sha256'],
                      'source_digest_verified': False,
                      'scope': 'selected river threshold cue; raw vendor download not audited'})

    relative = 'Art/Recorded/derived-sounds.json'
    manifest = ASSETS / relative
    for item in json.loads(manifest.read_text(encoding='utf-8'))['sounds']:
        entry = check(ASSETS / 'Resources/Sounds' / item['file'], item['sha256'], 'derived table/river foley', item['licence'], item['page'])
        source = ASSETS / 'Art/Recorded' / item['source']
        source_entry, source_problem = inspect_file(source, item['source_sha256'])
        entry.update({'manifest': manifest.relative_to(ROOT).as_posix(), 'title': item['title'],
                      'author': item['author'], 'edit': item['edit'], 'derived_from': source_entry,
                      'scope': 'Resources cue; stage-rise also used by cinematic opening'})
        if source_problem:
            problems.append(entry['path'] + ':derived-source')
            findings.append({'code': 'derived_source_mismatch', 'path': entry['path'], 'detail': source_entry})
    return

def personal_narration(problems, findings):
    manifest = ASSETS / 'Content/personal-table.narration.json'
    data = json.loads(manifest.read_text(encoding='utf-8'))
    result = []
    for section in ('chapters', 'lines', 'archived_chapters'):
        for item in data.get(section, []):
            scope = 'retained for exact older replay' if section == 'archived_chapters' else 'current prepared table content'
            identity = item.get('chapter_id', item.get('line_id'))
            if item.get('status') == 'pending-recording':
                result.append({'line': identity, 'scope': scope, 'status': 'pending-recording'})
                findings.append({'code': 'pending_recording', 'line': identity, 'detail': 'Caption content is not proof of a supplied recording.'})
                continue
            clip = ASSETS / 'Resources/Narration/personal-table' / item['filename']
            entry, problem = inspect_file(clip, item.get('audio_sha256'))
            text_hash = hashlib.sha256(item['text'].encode('utf-8')).hexdigest()
            declared_text = item.get('text_sha256', item.get('hash'))
            spoken_hash = hashlib.sha256(item.get('spoken', item['text']).encode('utf-8')).hexdigest()
            expected_resource = 'Narration/personal-table/' + Path(item['filename']).stem
            entry.update({'line': identity, 'scope': scope, 'section': section,
                          'manifest': manifest.relative_to(ROOT).as_posix(),
                          'resource_id': item.get('resource_id'),
                          'resource_id_matches': item.get('resource_id') == expected_resource,
                          'text_sha256': text_hash, 'declared_text_sha256': declared_text,
                          'text_hash_matches': text_hash == declared_text if declared_text else None,
                          'spoken_sha256': spoken_hash, 'declared_spoken_sha256': item.get('spoken_sha256'),
                          'spoken_hash_matches': spoken_hash == item['spoken_sha256'] if item.get('spoken_sha256') else None,
                          'provider': item.get('provider', data.get('provider')),
                          'model': item.get('model', data.get('model')), 'voice': item.get('voice', data.get('voice')),
                          'declared_rights_status': data.get('review_status'), 'date': item.get('date'),
                          'derived_from': item.get('derived_from'), 'edit': item.get('edit')})
            if problem or entry['resource_id_matches'] is False or entry['text_hash_matches'] is False or entry['spoken_hash_matches'] is False:
                problems.append(section + ':' + identity + ':' + item['filename'])
            if not item.get('audio_sha256') or not declared_text:
                findings.append({'code': 'missing_narration_digest', 'path': entry['path'], 'line': identity})
            result.append(entry)
    findings.append({'code': 'declared_combined_review_pending',
                     'manifest': manifest.relative_to(ROOT).as_posix(), 'detail': data.get('review_status'),
                     'limit': 'The declaration combines provider terms with cultural/listening acceptance; this audit does not establish a distribution prohibition.'})
    return result

def main():
    entries=[];problems=[];findings=[]
    def check(path,expected,kind,licence,source):
        entry,problem=inspect_file(path,expected)
        entry.update({'kind':kind,'rights_basis':licence,'source':source});entries.append(entry)
        if problem:problems.append(entry['path'])
        return entry
    art=ASSETS/'Art/IllustratedTheatre';data=json.loads((art/'sources.json').read_text(encoding='utf-8'))
    for item in data['assets']:check(art/item['file'],item['sha256'],'generated illustration','OpenAI output/account terms unresolved','Art/IllustratedTheatre/sources.json')
    item=json.loads((art/'modern-river-v1.sources.json').read_text(encoding='utf-8'));check(art/item['asset'],item['sha256'],'generated illustration','OpenAI output/account terms unresolved','Art/IllustratedTheatre/modern-river-v1.sources.json')
    data=json.loads((ASSETS/'Art/Recorded/sources.json').read_text(encoding='utf-8'))
    for item in data['recordings']:check(ASSETS/'Art/Recorded'/item['file'],item['sha256'],'field recording',item['licence'],item['page'])
    data=json.loads((ASSETS/'Art/Scanned/sources.json').read_text(encoding='utf-8'))
    for item in data['assets']:
        for filename,info in item['files'].items():check(ASSETS/'Art/Scanned'/item['id']/filename,info['sha256'],'scanned texture',item['license'],item['page'])
    fonts=ASSETS/'Art/Fonts/AtkinsonHyperlegible';data=json.loads((fonts/'sources.json').read_text(encoding='utf-8'))
    for filename,expected in data['files'].items():check(fonts/filename,expected,'font/notice',data['license'],data['source'])
    package=json.loads((ASSETS/'Content/living-gallery.json').read_text(encoding='utf-8'));manifests={};speech=[]
    for line in lines(package):
        namespace,key=line['audio'].split('/',1)
        if namespace not in manifests:manifests[namespace]=json.loads((ASSETS/'Content'/f'{namespace}.narration.json').read_text(encoding='utf-8'))
        m=manifests[namespace].get(key);clip=ASSETS/'Resources/Narration'/f"{line['audio']}.mp3"
        ok=m is not None and m['fingerprint']==fingerprint(line['text'],m['model'],m['voice'],m.get('direction',DIRECTION)) and clip.is_file()
        speech.append({'line':line['id'],'audio':line['audio'],'text_sha256':hashlib.sha256(line['text'].encode()).hexdigest(),'sha256':sha(clip) if clip.is_file() else None,'fingerprint_matches':ok,'model':m.get('model') if m else None,'voice':m.get('voice') if m else None})
        if not ok:problems.append(line['id'])
    river_inventory(check,findings,problems)
    table_speech=personal_narration(problems,findings)
    report={'package_version':package['version'],'package_sha256':sha(ASSETS/'Content/living-gallery.json'),'source_commit':subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip(),'integrity_problems':problems,'evidence_findings':findings,'inputs':entries,'current_narration':speech,'personal_table_narration':table_speech,
            'scope':{'legacy_package':'unity/Assets/Sankofa/Content/living-gallery.json','selected_scene':'unity/Assets/Sankofa/Scenes/RiverThresholdPainted.unity','selected_scene_sha256':sha(ASSETS/'Scenes/RiverThresholdPainted.unity'),'scene_dependency_or_apk_audit':False},
            'limits':['Integrity and declared provenance audit, not legal, cultural, listening or device acceptance.','Legacy checks remain; new river inputs include original paintings and comparison materials from shared manifests. This is not an APK inclusion or dependency report.','A current SHA-256 without a declared expected digest is observation only; hash_matches is null and missing declarations are findings.','Personal-table entries include 12 current chapters, welcome/return lines and retained older recordings; duplicate file references remain distinct text records.','Source vendor downloads are not reverified by this audit; derived runtime cue sources are checked against their recorded digest.','Unity engine, Meta SDK and font distribution notices and reachable attribution still require final-build inspection.','Applicable user/provider account agreements and original project contributor permissions remain unconfirmed.']}
    out=ROOT/'artifacts/release/gallery-asset-integrity.json';out.parent.mkdir(parents=True,exist_ok=True);out.write_text(json.dumps(report,indent=2),encoding='utf-8')
    print(f'{len(entries)} art/recording/texture/font inputs ({sum(e["hash_matches"] is True for e in entries)} declared hashes verified); {len(speech)} legacy gallery speech lines; {len(table_speech)} personal-table narration records; {len(problems)} integrity problems; {len(findings)} evidence findings. Report: {out}')
    if problems:raise SystemExit(1)
if __name__=='__main__':main()
