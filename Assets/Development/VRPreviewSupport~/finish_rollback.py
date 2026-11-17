"""Finish rollback after Unity's package and script reloads (stdlib only)."""
import hashlib,json,pathlib,subprocess,sys,time,shutil,os
ROOT=pathlib.Path(__file__).resolve().parents[3];os.chdir(ROOT)
W=pathlib.Path('Assets/M/VRPreviewSupport~')
MARKER=W/'rollback-finalize.json'
sha=lambda p:hashlib.sha256(pathlib.Path(p).read_bytes()).hexdigest()

def main():
    state=json.loads(MARKER.read_text(encoding='utf8'))
    ready,baseline=state['ready'],state['baseline']
    unity=ready.get('unityCli') or shutil.which('unity')
    assert unity,'Unity CLI not found'
    def evaluate(code):
        r=subprocess.run([unity,'command','eval','--code',code,'--json'],capture_output=True,text=True,timeout=20)
        d=json.loads(r.stdout)
        if r.returncode or not d.get('success'):raise RuntimeError(r.stdout[-600:])
        result=d['data']['result']
        if not result.get('success'):raise RuntimeError(str(result))
        return result['result']
    code='return new {mock=UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages().Any(p=>p.name=="com.unity.xr.mock-hmd"),busy=UnityEditor.EditorApplication.isCompiling||UnityEditor.EditorApplication.isUpdating,previewType=System.AppDomain.CurrentDomain.GetAssemblies().Any(a=>a.GetType("ProjectWait.M.VRPreview.VRPreviewTest",false)!=null)};'
    deadline=time.monotonic()+180;settled=None;last=''
    while time.monotonic()<deadline:
        try:
            s=evaluate(code)
            if not s['mock'] and not s['busy'] and not s['previewType']:
                if settled is None:settled=time.monotonic()
                if time.monotonic()-settled>=3:break
            else:settled=None
        except Exception as ex:last=str(ex);settled=None
        time.sleep(1)
    else:raise RuntimeError('Editor/package/script reload did not settle: '+last)
    assert not pathlib.Path('Assets/M/VRPreview').exists(),'Preview source folder must be removed first'
    for p,expected in ready['guarded'].items():
        assert p in ('Packages/manifest.json','Packages/packages-lock.json') or p.startswith('Assets/XR/'),p
        assert '..' not in p,p
        info=baseline['files'][p]
        assert sha(info['backup'])==info['sha256'],'Damaged backup: '+p
        assert sha(p) in (expected,info['sha256']),'Later user setting changes: '+p
    for p in ready['guarded']:
        if p.startswith('Assets/XR/'):
            pathlib.Path(p).write_bytes(pathlib.Path(baseline['files'][p]['backup']).read_bytes())
            evaluate('UnityEditor.AssetDatabase.ImportAsset('+json.dumps(p)+',UnityEditor.ImportAssetOptions.ForceSynchronousImport);return true;')
    time.sleep(2)
    mismatches=[p for p in ready['guarded'] if sha(p)!=baseline['files'][p]['sha256']]
    assert not mismatches,mismatches
    result={'phase':'complete','rootRemoved':True,'previewFolderRemoved':True,'exactSceneRestore':state['exactSceneRestore'],'packageRemoved':True,'settingsVerified':list(ready['guarded']),'finishedAfterScriptReload':True}
    (W/'full-rollback.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    MARKER.unlink()
    return result

if __name__=='__main__':
    try:main()
    except Exception as ex:
        (W/'rollback-blocked.txt').write_text(str(ex),encoding='utf8')
        (W/'full-rollback.json').write_text(json.dumps({'phase':'blocked','error':str(ex)},ensure_ascii=False,indent=2),encoding='utf8')
        raise
