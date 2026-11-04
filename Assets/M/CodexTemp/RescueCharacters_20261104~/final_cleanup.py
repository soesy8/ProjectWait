import bpy
from pathlib import Path
WORK=Path(__file__).resolve().parent
OUT=WORK/'Delivery'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'Wait_Rescue_AB.blend'))
bpy.context.preferences.filepaths.save_version=0
outline=bpy.data.collections['00_INK • 갈색 실루엣 대상']
S=.32
for ob in bpy.data.objects:
    if ob.type!='MESH' or not ob.get('Part'): continue
    part=ob['Part']
    if ob.get('BindBone','').startswith('hand.'):
        # The requested extra reach belongs to the arm, so retain the original palm length.
        for v in ob.data.vertices: v.co.z=2.07025*S+(v.co.z-2.07025*S)/1.175
    if part=='Trousers • pelvis' or part.startswith('Boot ankle shaft.'):
        if ob.name in outline.objects: outline.objects.unlink(ob)
    ob.data.update()
for rig in [o for o in bpy.data.objects if o.type=='ARMATURE']:
    bpy.context.view_layer.objects.active=rig; bpy.ops.object.mode_set(mode='EDIT')
    for side in ('L','R'): rig.data.edit_bones['hand.'+side].tail.z=1.67025*S
    bpy.ops.object.mode_set(mode='OBJECT')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Wait_Rescue_AB.blend'))
print('CLEANUP_DONE',flush=True)
