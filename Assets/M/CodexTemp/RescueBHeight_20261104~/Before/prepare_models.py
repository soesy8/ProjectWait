import bpy, json, re
from pathlib import Path
WORK=Path(__file__).resolve().parent
ROOT=WORK.parents[1]
OUT=ROOT/'Arts'/'Models'/'Rescuers'/'Sources'
OUT.mkdir(parents=True,exist_ok=True)
SOURCE=Path('E:/ProjectWait/Blender/구조요원')
report=[]
for which,filename in [('A','Wait_Rescuer_A_Firefighter.blend'),('B','Wait_Rescuer_B_LiftSpecialist.blend')]:
    bpy.ops.wm.open_mainfile(filepath=str(SOURCE/filename))
    outline=bpy.data.collections['00_INK • 갈색 실루엣 대상']
    rig=next(o for o in bpy.data.objects if o.type=='ARMATURE')
    rig.location=(0,0,0)
    parts=[o for o in bpy.data.objects if o.type=='MESH' and o.get('Part')]
    materials={}; records=[]
    for i,ob in enumerate(parts):
        part=ob['Part']; exported=which+'_%03d_'%i+re.sub('[^A-Za-z0-9_]+','_',part).strip('_')[:50]
        records.append({'name':exported,'part':part,'outlined':ob.name in outline.objects})
        ob.name=exported
        # Preserve face silhouettes; simplify the dense garment surfaces only.
        if part.startswith(('Jacket • continuous','Yoke • fitted','Trousers • continuous')):
            arm=next((m for m in ob.modifiers if m.type=='ARMATURE'),None)
            if arm: ob.modifiers.remove(arm)
            dec=ob.modifiers.new('Runtime garment reduction','DECIMATE'); dec.ratio=.48; dec.use_collapse_triangulate=True
            bpy.context.view_layer.objects.active=ob
            bpy.ops.object.modifier_apply(modifier=dec.name)
            if arm is not None:
                mod=ob.modifiers.new('FK skin','ARMATURE'); mod.object=rig
        for slot in ob.material_slots:
            old=slot.material
            if old is None: continue
            if old.name not in materials:
                name='Rescue_'+which+'_Mat_%02d'%len(materials)
                flat=not any(n.type=='VALTORGB' for n in old.node_tree.nodes)
                pbr=bpy.data.materials.new(name); pbr.diffuse_color=old.diffuse_color; pbr.use_nodes=True
                shader=pbr.node_tree.nodes.get('Principled BSDF'); shader.inputs['Base Color'].default_value=old.diffuse_color
                shader.inputs['Roughness'].default_value=.95
                materials[old.name]={'material':pbr,'record':{'name':name,'hex':old.get('BaseColor_sRGB','#FFFFFF'),'flat':flat,'source':old.name}}
            slot.material=materials[old.name]['material']
    bpy.ops.object.select_all(action='DESELECT')
    rig.select_set(True)
    for ob in parts: ob.select_set(True)
    bpy.context.view_layer.objects.active=rig
    path=OUT/('Rescuer_'+which+'.fbx')
    bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH','ARMATURE'},use_mesh_modifiers=True,
        add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,use_custom_props=True)
    record={'id':which,'file':'Assets/M/Arts/Models/Rescuers/Sources/'+path.name,'objects':records,'materials':[v['record'] for v in materials.values()]}
    (OUT/('Rescuer_'+which+'_Manifest.json')).write_text(json.dumps(record,ensure_ascii=False,indent=2),encoding='utf-8')
    report.append({'id':which,'objects':len(parts),'materials':len(materials),'source':filename,'file':str(path)})
(WORK/'ModelPreparation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print('PREPARED '+json.dumps(report,ensure_ascii=True),flush=True)
