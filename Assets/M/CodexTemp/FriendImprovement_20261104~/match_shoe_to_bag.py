"""Change only the existing shoe palette swatch, preserving the authored model."""
import bpy, math, os, json
from mathutils import Vector
ROOT=r'D:\project\ProjectWait\Assets\M'
QA=ROOT+r'\CodexTemp\FriendImprovement_20261104~'
atlas=next(i for i in bpy.data.images if i.packed_file)
width,height=atlas.size;cols=width//16
pixels=list(atlas.pixels[:]);bag=8;shoe=16
for y in range(16):
 for x in range(16):
  src=((bag//cols*16+y)*width+bag%cols*16+x)*4
  dst=((shoe//cols*16+y)*width+shoe%cols*16+x)*4
  pixels[dst:dst+4]=pixels[src:src+4]
atlas.pixels.foreach_set(pixels);atlas.update()
atlas.unpack(method='REMOVE')
atlas.filepath_raw=ROOT+r'\Arts\Textures\FriendImprovedPalette.png'
atlas.file_format='PNG';atlas.save();atlas.pack()
arm=next(o for o in bpy.data.objects if o.type=='ARMATURE')
mesh=next(o for o in bpy.data.objects if o.type=='MESH')
scene=bpy.context.scene;cam=scene.camera;target=Vector((0,0,.76))
def reset():
 for b in arm.pose.bones:b.rotation_mode='XYZ';b.rotation_euler=(0,0,0)
 for key in mesh.data.shape_keys.key_blocks:key.value=0
def relaxed():
 reset()
 for side,sign in [('L',1),('R',-1)]:
  arm.pose.bones['upper_arm.'+side].rotation_euler.x=math.radians(-62)
  arm.pose.bones['forearm.'+side].rotation_euler.z=math.radians(9*sign)
def render(name,location):
 cam.location=location;cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
 scene.render.filepath=os.path.join(QA,name+'.png');bpy.context.view_layer.update();bpy.ops.render.render(write_still=True)
relaxed();render('improved_front',(0,-4,1.06));render('improved_threequarter',(2.5,-4,1.85));render('improved_back',(2.2,4,1.65))
reset();arm.pose.bones['upper_arm.L'].rotation_euler.x=math.radians(-55);arm.pose.bones['forearm.L'].rotation_euler.z=math.radians(-65);arm.pose.bones['thigh.R'].rotation_euler.x=math.radians(-22);arm.pose.bones['shin.R'].rotation_euler.x=math.radians(45);render('rig_bend_test',(2.5,-4,1.85))
for key,name in [('Surprised','expression_surprised'),('Worried','expression_worried'),('Blink','expression_blink')]:
 relaxed();mesh.data.shape_keys.key_blocks[key].value=1;render(name,(0,-4,1.06))
reset();bpy.ops.object.select_all(action='DESELECT');arm.select_set(True);mesh.select_set(True);bpy.context.view_layer.objects.active=arm
bpy.ops.export_scene.fbx(filepath=ROOT+r'\Arts\Models\FriendImproved\Wait_Friend_Improved.fbx',use_selection=True,object_types={'ARMATURE','MESH'},use_mesh_modifiers=False,add_leaf_bones=False,use_armature_deform_only=True,bake_anim=False,axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',use_custom_props=True)
relaxed();cam.location=(2.5,-4,1.85);cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
current=list(atlas.pixels[:]);bag_index=((bag//cols*16+8)*width+bag%cols*16+8)*4;shoe_index=((shoe//cols*16+8)*width+shoe%cols*16+8)*4
report={'bag_rgba':current[bag_index:bag_index+4],'shoe_rgba':current[shoe_index:shoe_index+4],'matches':current[bag_index:bag_index+4]==current[shoe_index:shoe_index+4],'material_count':len(mesh.data.materials)}
with open(os.path.join(QA,'shoe_bag_color_validation.json'),'w') as f:json.dump(report,f,indent=2)
assert report['matches'] and report['material_count']==1
print(json.dumps(report))
