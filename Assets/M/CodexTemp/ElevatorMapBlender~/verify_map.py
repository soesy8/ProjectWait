import bpy
import json
from pathlib import Path
from mathutils import Matrix, Vector

work=Path(__file__).resolve().parent
source=json.loads((work/'Elevator_Map_Source.json').read_text(encoding='utf-8'))
by_id={str(o.get('Unity_Source_ID')):o for o in bpy.data.objects if o.get('Unity_Source_ID')}
assert len(by_id)==len(source['parts'])
conversion=Matrix(((1,0,0),(0,0,1),(0,1,0)))
bpy.context.view_layer.update()
max_error=0.
for part in source['parts']:
    obj=by_id[part['id']]
    expected=conversion@Vector(part['worldPosition'])
    max_error=max(max_error,(obj.matrix_world.translation-expected).length)
    assert obj.hide_render== (not part['visible']),part['path']
    assert bool(obj.parent)==bool(part['parent']),part['path']
    if part['parent']:assert obj.parent==by_id[part['parent']]
    if part['geometry']:
        assert obj.type=='MESH'
        assert len(obj.data.vertices)==len(part['geometry']['vertices']),part['path']
        assert all(mat is not None for mat in obj.data.materials),part['path']
        assert all(poly.material_index<len(obj.data.materials) for poly in obj.data.polygons),part['path']
        if part['text']:assert obj['안내판 원문']==part['text']['content']
assert max_error<.0001,max_error
meshes=[o for o in by_id.values() if o.type=='MESH']
assert len(meshes)==source['meshes']
assert sum(len(o.data.vertices) for o in meshes)==source['vertices']
assert sum(len(o.data.polygons) for o in meshes)==source['triangles']
used_images=[img for img in bpy.data.images if img.type=='IMAGE' and img.users>0]
assert used_images and all(img.packed_file for img in used_images)
assert not bpy.data.libraries
assert bpy.data.objects.get('LeftDoor') and bpy.data.objects.get('RightDoor')
assert bpy.context.scene.camera.name=='Camera_로비_원본'
report={'reopened':True,'file':bpy.data.filepath,'sourceObjects':len(by_id),'meshes':len(meshes),'vertices':sum(len(o.data.vertices) for o in meshes),'triangles':sum(len(o.data.polygons) for o in meshes),'maxWorldPositionErrorMeters':max_error,'packedImages':len(used_images),'externalLibraries':len(bpy.data.libraries),'sourceCountsMatch':True,'hierarchyMatches':True,'visibilityMatches':True,'missingMaterialSlots':0,'textObjects':sum(1 for o in by_id.values() if o.get('안내판 원문'))}
(work/'Reopen_Validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print('REOPEN_VALIDATION '+json.dumps(report,ensure_ascii=False),flush=True)
