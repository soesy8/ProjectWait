"""Requested relative dimensions: B body X 83%; A/B body Y 112% / 108%."""
import bpy, json, math
from pathlib import Path
from mathutils import Matrix

WORK=Path(__file__).resolve().parent
OUT=WORK/'Delivery'
BASE=WORK/'BeforeWidthDepth_20261104'/'Wait_Rescue_AB.blend'
bpy.ops.wm.open_mainfile(filepath=str(BASE))
bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene

def bounds(objects):
    coords=[v.co for ob in objects for v in ob.data.vertices]
    low=[min(p[i] for p in coords) for i in range(3)]
    high=[max(p[i] for p in coords) for i in range(3)]
    return {'min':low,'max':high,'size':[high[i]-low[i] for i in range(3)]}

report={'operation':'Relative to delivered models before this request',
        'axes':'Blender local X = left/right; Y = front/back; Z = height',
        'head_hair_face_and_helmet':'Preserved', 'characters':[], 'issues':[]}

for which,fx,fy in [('A',1.0,1.12),('B',.83,1.08)]:
    prefix=which+' | '
    rig=bpy.data.objects[prefix+'RIG • Pose Mode']
    parts=[o for o in bpy.data.objects if o.type=='MESH' and o.name.startswith(prefix) and o.get('Part')]
    body=[o for o in parts if o.get('BindBone')!='head']
    head=[o for o in parts if o.get('BindBone')=='head']
    before=bounds(body); head_before=bounds(head)
    head_snapshot={ob.name:[tuple(v.co) for v in ob.data.vertices] for ob in head}
    transform=Matrix.Diagonal((fx,fy,1.0,1.0))
    for ob in body:
        if ob.data.users>1: ob.data=ob.data.copy()
        ob.data.transform(transform,shape_keys=True)
        ob.data.update()
    bpy.ops.object.select_all(action='DESELECT')
    bpy.context.view_layer.objects.active=rig; rig.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    for bone in rig.data.edit_bones:
        if bone.name=='head': continue
        bone.head=transform@bone.head
        bone.tail=transform@bone.tail
    bpy.ops.object.mode_set(mode='OBJECT')
    after=bounds(body); head_after=bounds(head)
    actual=[after['size'][i]/before['size'][i] for i in range(3)]
    expected=[fx,fy,1.0]
    if any(abs(a-b)>1e-5 for a,b in zip(actual,expected)):
        report['issues'].append(which+': dimension ratio mismatch')
    max_head_delta=max(abs(v.co[i]-head_snapshot[ob.name][j][i]) for ob in head for j,v in enumerate(ob.data.vertices) for i in range(3))
    if max_head_delta>1e-8: report['issues'].append(which+': head modified')
    if any(not all(math.isfinite(c) for c in v.co) for ob in body for v in ob.data.vertices):
        report['issues'].append(which+': invalid coordinates')
    rig['Body_dimension_revision']='X %.2f / Y %.2f / Z 1.00, head preserved'%(fx,fy)
    report['characters'].append({'id':which,'body_meshes':len(body),'head_meshes_preserved':len(head),
        'before_body_bounds_m':before,'after_body_bounds_m':after,
        'measured_axis_ratios':actual,'requested_axis_ratios':expected,
        'head_bounds_before_m':head_before,'head_bounds_after_m':head_after,
        'max_head_vertex_difference_m':max_head_delta})

report['status']='PASS' if not report['issues'] else 'FAIL'
if report['issues']: raise RuntimeError(str(report['issues']))
scene['Body_dimension_revision']='B width 83%; A depth 112%; B depth 108%; heads unchanged'
text=bpy.data.texts['읽어주세요 • README']
note='\n추가 비율 수정: 직전 납품본 대비 몸 좌우 폭은 B 83%, 앞뒤 두께는 A 112% / B 108%로 조정했습니다.\n머리·이목구비·머리카락·모자는 유지했습니다. 팔·다리·신발·장비와 뼈대는 몸 비율에 맞춰 함께 조정했습니다.\n키와 팔의 세로 길이는 유지했습니다. 세부 치수 검증은 Dimensions_구조요원.json에 있습니다.\n'
text.write(note)
(OUT/'README_구조요원.txt').write_text(text.as_string(),encoding='utf-8-sig')
(OUT/'Dimensions_구조요원.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
bpy.ops.object.select_all(action='DESELECT')
head=bpy.data.objects['A | Head • 둥근 얼굴']; head.select_set(True); bpy.context.view_layer.objects.active=head
scene.camera=bpy.data.objects['CAM_00 • 두 구조요원']; scene.render.filepath='//Preview_00_AB.png'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Wait_Rescue_AB.blend'))
print('DIMENSION_REVISION '+json.dumps(report,ensure_ascii=True),flush=True)
