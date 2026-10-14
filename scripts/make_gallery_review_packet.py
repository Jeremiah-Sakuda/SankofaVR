#!/usr/bin/env python3
"""Make offline cultural and blind-comprehension packets from current gallery content.
No provider requests, playback, uploads, fabricated responses or personal data collection.
Audio contains existing clips in resolved order, without invitation waits/transitions;
it is a script read-through, not a recording of a live visit.
"""
import hashlib, html, json, shutil, subprocess, zipfile
from pathlib import Path
ROOT=Path(__file__).resolve().parent.parent
ASSETS=ROOT/'unity/Assets/Sankofa'
PACKAGE=ASSETS/'Content/living-gallery.json'
OUT=ROOT/'artifacts/gallery-review'
STYLE='''body{overflow-wrap:anywhere;font:18px/1.6 system-ui,sans-serif;background:#faf7f0;color:#231e19;margin:0}main{max-width:860px;margin:auto;padding:32px 24px}h1{font-size:32px}h2{margin-top:36px}a{color:#174d78}a:focus-visible,button:focus-visible,textarea:focus-visible{outline:3px solid #b85b12;outline-offset:4px}img,video,audio{max-width:100%;width:100%}code{overflow-wrap:anywhere}img{height:auto}a,button{cursor:pointer}details{margin:24px 0}summary{cursor:pointer;padding:12px 0}figure{margin:24px 0}label{display:block;font-weight:600;margin-top:20px}textarea{display:block;width:100%;min-height:100px;box-sizing:border-box;font:inherit}button{font:inherit;padding:12px 18px;min-height:44px;margin-top:24px}small{display:block}.line{padding:10px 0;border-bottom:1px solid #d2c7b9}code{font-size:14px}'''
WORDS=['Enkare Nairobi','Wambui','Naserian','Maa','Maasai','Kikuyu','Akan','Sankofa','oloirien']
def e(s):return html.escape(str(s))
def page(title,body):return f'<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>{e(title)}</title><style>{STYLE}</style></head><body><main><h1>{e(title)}</h1>{body}</main></body></html>'
def questions(items):
    fields=''.join(f'<label for="q{i}">{e(q)}</label><textarea id="q{i}" name="q{i}"></textarea>' for i,q in enumerate(items))
    return '<p>Optional notes stay on this page until you explicitly download them. No network requests or automatic storage. Leave out names and identifying details. Reloading discards unsaved answers.</p><form id="review">'+fields+'''<button type="button" id="download">Download my notes as JSON</button></form><script>document.getElementById('download').onclick=()=>{const responses=Object.fromEntries(new FormData(document.getElementById('review')));const b=new Blob([JSON.stringify({packet:document.title,responses},null,2)],{type:'application/json'});const u=URL.createObjectURL(b);const a=document.createElement('a');a.href=u;a.download='review-notes.json';a.click();setTimeout(()=>URL.revokeObjectURL(u),1000);};</script>'''
def resolved(p,route,mode):
    beats={b['id']:b for b in p['beats']};outcomes={}
    for bid in route['beats']:
        b=beats[bid]
        for line in b['lines']:
            when=line.get('when')
            if not when or outcomes.get(when.split(':')[0])==when.split(':')[1]:yield line
        inv=b.get('invitation')
        if inv:
            outcomes[inv['id']]=mode
            yield from inv[mode]
def build_audio(folder,name,lines):
    clips=[];timeline=[];at=0
    for line in lines:
        clip=ASSETS/'Resources/Narration'/f"{line['audio']}.mp3"
        if not clip.is_file():raise ValueError(f'Missing clip {clip}')
        seconds=float(subprocess.check_output(['ffprobe','-v','error','-show_entries','format=duration','-of','csv=p=0',str(clip)]))
        timeline.append({'seconds':round(at,3),'id':line['id'],'text':line['text'],'claims':line.get('claims',[]),'audio':line['audio']});at+=seconds
        clips.append(clip)
    listing=folder/f'{name}.ffconcat'
    listing.write_text('ffconcat version 1.0\n'+''.join("file '"+str(c).replace("'","'\\''")+"'\n" for c in clips))
    subprocess.run(['ffmpeg','-hide_banner','-loglevel','error','-y','-f','concat','-safe','0','-i',str(listing),'-c:a','libmp3lame','-b:a','128k',str(folder/f'{name}.mp3')],check=True)
    listing.unlink();return timeline

def main():
    OUT.mkdir(parents=True,exist_ok=True);p=json.loads(PACKAGE.read_text());cultural=OUT/'cultural';novice=OUT/'first-time'
    for folder in [cultural,novice]:folder.mkdir(parents=True,exist_ok=True)
    scripts=[];word_rows=[]
    for route in p['routes'][:2]:
        for mode in ['default','accepted']:
            name=route['id'].replace('.','-')+'-'+mode;lines=list(resolved(p,route,mode));timeline=build_audio(cultural,name,lines)
            (cultural/f'{name}.json').write_text(json.dumps(timeline,indent=2))
            scripts.append(f'<h2>{e(route["label"])} — {e(mode)}</h2><audio controls preload="none" src="{name}.mp3"></audio><p>Sequential recorded lines; waiting and transitions omitted. Accepted path assumes every offered act occurred. Passive path assumes none occurred. Both are scripted review paths, not human sessions.</p>'+''.join(f'<div class="line"><code>{row["seconds"]:.1f}s · {e(row["id"])}</code><br>{e(row["text"])}<small>Claims: {e(", ".join(row["claims"])) or "Authored connective dialogue; see reconstruction claims"}</small></div>' for row in timeline))
            for word in WORDS:
                first=next((r for r in timeline if word.lower() in r['text'].lower()),None)
                if first:word_rows.append(f'<li>{e(word)} — {e(name)}, {first["seconds"]:.1f}s, {e(first["id"])}</li>')
    passive='era-1850s-default.mp3';shutil.copyfile(cultural/passive,novice/'story.mp3')
    pictures=[]
    for name,label in [('01-gallery','Before entry'),('02-what-she-brings','First memory'),('03-what-she-remembers','Other memory'),('07-shortfall','Encounter'),('05-modern-river','Modern continuation'),('08-caption-clearance','Synthetic caption obstruction check')]:
        src=ROOT/'artifacts/living-gallery'/f'{name}.png'
        if not src.is_file():raise ValueError(f'Missing current capture {src}')
        shutil.copyfile(src,cultural/src.name);pictures.append(f'<figure><img loading="lazy" src="{src.name}" alt="{e(label)}"><figcaption>{e(label)}. Muted desktop capture; not Quest or participant evidence.</figcaption></figure>')
    art=ASSETS/'Art/IllustratedTheatre'
    for name in ['cast-atlas.png','cast-expressions-v1.png','scenery-atlas.png','modern-river-v1.png']:
        shutil.copyfile(art/name,cultural/name);pictures.append(f'<figure><img loading="lazy" src="{name}" alt="Draft {e(name)}"><figcaption>Generated draft {e(name)}; fictional costume and setting, not an authenticated reconstruction.</figcaption></figure>')
    claims='<h2>Context and reconstruction register</h2>'+''.join(f'<div class="line"><strong>{e(c["id"])} · {e(c["state"])}</strong><p>{e(c["text"])}</p><ul>'+''.join(f'<li>{e(s)}</li>' for s in c.get('sources',[]))+'</ul></div>' for c in p['claims'])
    cultural_body='<p>Draft cultural, art and pronunciation review. Describe your own area of familiarity; no individual is being asked to endorse an entire community. No recording plays automatically. The narrator direction inherits a West African griot inspiration; suitability for this Kenyan encounter is an open question.</p>' + questions(['What is your area of familiarity, and what is outside it? (No name needed.)','Which specific clothing, gestures, props or relationships seem mistaken or flattened?','Does the framing distinguish Akan title, Kikuyu/Maasai context, documented history and invented encounter clearly?','Which words or names need pronunciation corrections? Give the clip/time and suggested wording or speaker reference.','Does the voice feel appropriate for this story? Which deliveries or emotional beats feel off?','Which source claims need correction or direct verification?','What must change before public release? Which comments may be credited publicly, if any?'])+'<h2>Draft art</h2>'+''.join(pictures)+'<h2>Pronunciation locations</h2><ul>'+''.join(word_rows)+'</ul>'+claims+''.join(scripts)
    (cultural/'index.html').write_text(page('Sankofa — cultural review draft',cultural_body))
    (novice/'index.html').write_text(page('Sankofa — first-time story review','<p>Listen or read only when you choose to. This is an audio read-through of one draft story, without waiting periods or interaction. It cannot establish VR comfort or how the hand interactions feel. No sound starts automatically.</p><audio controls preload="none" src="story.mp3"></audio><details><summary>Read the caption transcript instead</summary>'+''.join('<p>'+e(l['text'])+'</p>' for l in resolved(p,p['routes'][0],'default'))+'</details>'+questions(['Tell the story back in your own words.','Who wanted what? What made the meeting difficult?','What changed during the meeting?','Which decisions belonged to the characters? What did you understand the visitor could do?','Which parts did you understand as real history, and which as imagined?','Was anything confusing, uncomfortable or slow? Give a moment if you can.','What would you want to learn or experience next?'])))
    manifest={'package_version':p['version'],'package_sha256':hashlib.sha256(PACKAGE.read_bytes()).hexdigest(),'source_commit':subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip(),'working_tree_status':subprocess.check_output(['git','status','--porcelain'],cwd=ROOT,text=True).splitlines(),'status':'Prepared; no reviewers contacted or responses collected','audio':'Existing clips reordered locally; no synthesis, exports or playback','pictures':'Current batch desktop captures; not headset evidence'}
    (OUT/'manifest.json').write_text(json.dumps(manifest,indent=2))
    for folder in [cultural,novice]:
        (folder/'manifest.json').write_text(json.dumps({k:v for k,v in manifest.items() if k!='working_tree_status'},indent=2))
    for folder in [cultural,novice]:
        with zipfile.ZipFile(OUT/f'{folder.name}.zip','w',zipfile.ZIP_DEFLATED) as z:
            for f in sorted(folder.rglob('*')):
                if f.is_file():z.write(f,f.relative_to(OUT))
    print(f'Prepared {OUT}; no playback or messages sent.')
if __name__=='__main__':main()
