import bpy,json,math,hashlib,struct
from pathlib import Path
WORK=Path(__file__).resolve().parent;OUT=WORK/'Delivery'
spec=json.loads((OUT/'BHeightHead_Validation.json').read_text(encoding='utf-8'))
bpy.context.preferences.filepaths.save_version=0
report={'status':'PASS','files':[],'fbx':None,'issues':[]}
def bounds(objects):
    p=[v.co for o in objects for v in o.data.vertices]
    return [max(v[i] for v in p)-min(v[i] for v in p) for i in range(3)]
for filename in ['Wait_Rescuer_B_LiftSpecialist.blend','Wait_Rescue_AB.blend']:
    bpy.ops.wm.open_mainfile(filepath=str(OUT/filename))
    rig=bpy.data.objects['B | RIG • Pose Mode']
    parts=[o for o in bpy.data.objects if o.type=='MESH' and o.name.startswith('B | ') and o.get('Part')]
    head=[o for o in parts if o['BindBone']=='head'];body=[o for o in parts if o['BindBone']!='head']
    size=bounds(parts);headsize=bounds(head);bodysize=bounds(body)
    ratios=[headsize[i]/spec['head_before']['size'][i] for i in range(3)]
    heightsize=size[2]/spec['before']['size'][2]
    weights=sum(1 for o in parts for v in o.data.vertices if not v.groups or abs(sum(g.weight for g in v.groups)-1)>.0001)
    missing=sum(1 for o in parts if not any(m.type=='ARMATURE' and m.object==rig for m in o.modifiers))
    issues=[]
    if abs(heightsize-.89)>1e-5:issues.append('height ratio')
    if any(abs(s-.96)>1e-5 for s in ratios):issues.append('head uniform scale')
    if weights or missing or len(rig.data.bones)!=18:issues.append('skin or skeleton')
    if any(abs(bodysize[i]-spec['body_before']['size'][i])>1e-6 for i in [0,1]):issues.append('body width/depth changed')
    bpy.context.scene['Model_height_m']=size[2] if filename.startswith('Wait_Rescuer_B') else 'A 1.685445 / B %.6f'%size[2]
    for text in bpy.data.texts:
        if 'README' in text.name:
            value=text.as_string().replace('B 약 1.66m','B 약 1.48m');text.clear();text.write(value)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/filename))
    source=(WORK/'Before'/filename)
    # Existing A source geometry inside the pair must remain identical.
    if filename=='Wait_Rescue_AB.blend':
        def a_hash():
            h=hashlib.sha256()
            for ob in sorted([o for o in bpy.data.objects if o.type=='MESH' and o.name.startswith('A | ') and o.get('Part')],key=lambda o:o.name):
                h.update(ob.name.encode())
                for v in ob.data.vertices:h.update(struct.pack('<fff',*v.co))
            return h.hexdigest()
        actual=a_hash();bpy.ops.wm.open_mainfile(filepath=str(source))
        if actual!=a_hash():issues.append('A changed')
    report['files'].append({'file':filename,'height_ratio':heightsize,'height_m':size[2],'head_axis_ratios':ratios,'invalid_skin_vertices':weights,'missing_armature_bindings':missing,'issues':issues})
    report['issues']+=issues
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(OUT/'Wait_Rescuer_B_LiftSpecialist.fbx'),use_anim=False)
rigs=[o for o in bpy.data.objects if o.type=='ARMATURE'];meshes=[o for o in bpy.data.objects if o.type=='MESH']
report['fbx']={'rigs':len(rigs),'bones':len(rigs[0].data.bones) if rigs else 0,'meshes':len(meshes)}
if report['fbx']['rigs']!=1 or report['fbx']['bones']!=18 or report['fbx']['meshes']<20:report['issues'].append('FBX roundtrip')
report['status']='FAIL' if report['issues'] else 'PASS'
(OUT/'Validation_구조요원.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
dimensions=json.loads((WORK/'Before'/'Dimensions_구조요원.json').read_text(encoding='utf-8'))
dimensions['latest_B_height_head_revision']=spec
(OUT/'Dimensions_구조요원.json').write_text(json.dumps(dimensions,ensure_ascii=False,indent=2),encoding='utf-8')
readme=(OUT/'README_구조요원.txt').read_text(encoding='utf-8-sig').replace('B 약 1.66m','B 약 1.48m')
(OUT/'README_구조요원.txt').write_text(readme,encoding='utf-8-sig')
print('SOURCE_VERIFIED '+json.dumps(report),flush=True)
if report['status']!='PASS':raise RuntimeError(report['issues'])
