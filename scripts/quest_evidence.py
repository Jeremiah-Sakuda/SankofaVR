#!/usr/bin/env python3
"""Read-only Quest evidence collection. Does not install, launch, play sound or use a simulator.
Default lists devices; --serial selects a consenting connected device and collects build/display/log evidence.
Frame timings in logs are observations, not proof of sustained performance or stereo comfort.
"""
import argparse,hashlib,json,subprocess,time,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parent.parent
ADB='/Applications/Unity/Hub/Editor/6000.3.25f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb'
def run(args):
    try:return subprocess.run([ADB]+args,check=True,text=True,capture_output=True,timeout=30).stdout
    except (subprocess.CalledProcessError,subprocess.TimeoutExpired,OSError) as error:
        print('ADB read failed. Check the SDK path, local ADB socket permission and USB authorisation.',file=sys.stderr)
        raise SystemExit(1) from error

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--serial');p.add_argument('--out',type=Path,default=ROOT/'artifacts/quest-evidence');a=p.parse_args()
    devices=run(['devices']);print(devices.strip())
    if not a.serial:return
    ready=[line.split()[0] for line in devices.splitlines()[1:] if len(line.split())==2 and line.split()[1]=='device']
    if a.serial not in ready:raise SystemExit('Selected device is not connected and authorised.')
    a.out.mkdir(parents=True,exist_ok=True)
    def shell(*args):return run(['-s',a.serial,'shell',*args])
    apk=ROOT/'artifacts/Sankofa-LivingGallery.apk'
    result={'captured_unix':int(time.time()),'device_model':shell('getprop','ro.product.model').strip(),'os':shell('getprop','ro.build.version.release').strip(),'host_apk_sha256':hashlib.sha256(apk.read_bytes()).hexdigest() if apk.is_file() else None,'installed_package':shell('dumpsys','package','com.sankofa.quest'),'display':shell('dumpsys','display'),'thermal':shell('dumpsys','thermalservice'),'status':'Raw observations; human protocol and sustained run not performed by this script'}
    # Keep identifiers and device logs in ignored local artifacts, never in the public repository.
    (a.out/'device-observations.json').write_text(json.dumps(result,indent=2))
    (a.out/'sankofa-log.txt').write_text(run(['-s',a.serial,'logcat','-d','-t','5000','-s','Unity']))
    print(f'Read-only evidence saved to {a.out}. No application launched.')
if __name__=='__main__':main()
