import bpy,os
mesh=next(o for o in bpy.data.objects if o.type=='MESH')
single=mesh.data.materials[0]
for material in list(bpy.data.materials):
 if material!=single:bpy.data.materials.remove(material,do_unlink=True)
for data in list(bpy.data.meshes):
 if data.users==0:bpy.data.meshes.remove(data)
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
exec(compile(open(r'D:\project\ProjectWait\Assets\M\CodexTemp\FriendImprovement_20261104~\validate_blend.py',encoding='utf8').read(),'validate_blend.py','exec'))
