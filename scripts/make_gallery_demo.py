#!/usr/bin/env python3
"""Assemble a silent labelled desktop preview from explicit muted batch runtime captures.
No playback or publication. This draft is not qualifying Quest/emulator submission footage.
"""
import hashlib,json,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parent.parent;OUT=ROOT/'artifacts/gallery-demo'
SEGMENTS=[('01-frame','Sankofa: What We Carry'),('02-gourd-received','A painted palm offers a tangible gourd'),('03-memory','A memory changes the meaning of the encounter'),('04-flour-placement','A guest can help, or remain and watch'),('05-agreement','The women make their own agreement'),('06-same-frame','Return to the same painting'),('07-returning-memory','Keep a visit; discover the other memory')]
def main():
    OUT.mkdir(parents=True,exist_ok=True);clips=[];metadata=[]
    for name,label in SEGMENTS:
        (OUT/f'{name}-label.txt').write_text(label+'\nSilent desktop preview · synthetic input · not device evidence')
    subprocess.run(['node',str(ROOT/'scripts/render_demo_labels.cjs'),str(OUT)],check=True)
    for name,label in SEGMENTS:
        frames=sorted((OUT/name).glob('*.jpg'))
        if not frames or [f.stem for f in frames]!=[f'{i:04d}' for i in range(len(frames))]:raise SystemExit(f'Missing/nonconsecutive frames: {name}')
        clip=OUT/f'{name}.mp4'
        expected=30 if name=='04-flour-placement' else 40
        if len(frames)!=expected:raise SystemExit(f'Incomplete segment {name}: {len(frames)}/{expected}')
        subprocess.run(['ffmpeg','-hide_banner','-loglevel','error','-y','-framerate','10','-i',str(OUT/name/'%04d.jpg'),'-i',str(OUT/f'{name}-label.png'),'-filter_complex','[0:v][1:v]overlay=x=0:y=H-h','-an','-c:v','libx264','-crf','19','-pix_fmt','yuv420p',str(clip)],check=True)
        clips.append(clip);metadata.append({'segment':name,'description':label,'frames':len(frames),'capture_sample_hz':10,'first_frame_sha256':hashlib.sha256(frames[0].read_bytes()).hexdigest()})
    concat=OUT/'preview.ffconcat';concat.write_text('ffconcat version 1.0\n'+''.join("file '"+str(p).replace("'","'\\''")+"'\n" for p in clips))
    video=OUT/'Sankofa-software-preview.mp4'
    subprocess.run(['ffmpeg','-hide_banner','-loglevel','error','-y','-f','concat','-safe','0','-i',str(concat),'-an','-c','copy','-movflags','+faststart',str(video)],check=True)
    probe=json.loads(subprocess.check_output(['ffprobe','-v','error','-show_streams','-show_format','-of','json',str(video)]))
    if any(s['codec_type']=='audio' for s in probe['streams']) or float(probe['format']['duration'])>=180:raise SystemExit('Invalid silent preview')
    metadata={'source_commit':subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip(),'working_tree':subprocess.check_output(['git','status','--porcelain'],cwd=ROOT,text=True).splitlines(),'segments':metadata,'duration_seconds':float(probe['format']['duration']),'sha256':hashlib.sha256(video.read_bytes()).hexdigest(),'mode':'Unity muted batch runtime, desktop camera; synthetic PalmReceiver input for gourd, direct flow accepts for other invitations; edited excerpts with narration waits omitted','evidence_limits':'Not real hands, Quest/stereo/reach/performance/participant evidence or official qualifying footage. No audio playback or publication.'}
    (OUT/'manifest.json').write_text(json.dumps(metadata,indent=2));print(f'{video}: {metadata["duration_seconds"]:.1f}s, no audio stream')
if __name__=='__main__':main()
