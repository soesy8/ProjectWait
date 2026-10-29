"""Reopen verification of the final deliverable; no changes to the blend file."""
import bpy
import json
from pathlib import Path
from mathutils import Vector

WORK=Path(__file__).resolve().parent
S=bpy.context.scene
loaded_default_frame=S.frame_current
assert bpy.data.filepath.endswith('엘베_맵_실버.blend'),bpy.data.filepath
checks={}
checks['metricUnits']=S.unit_settings.system=='METRIC' and S.unit_settings.scale_length==1
checks['librariesSelfContained']=len(bpy.data.libraries)==0
checks['fontsPacked']=all(f.packed_file is not None for f in bpy.data.fonts if f.filepath and f.filepath!='<builtin>')
checks['materialsPresent']=all(o.data.materials and all(o.data.materials) for o in S.objects if o.type=='MESH')
checks['camerasPresent']=sum(o.type=='CAMERA' for o in S.objects)>=4
checks['collectionStructure']=len(bpy.data.collections)>=15
stairs=[o for o in S.objects if o.name.startswith('계단 ') and '디딤판' in o.name]
checks['nineteenStairTreads']=len(stairs)==19
checks['stairRise342cm']=abs(max(o.location.z+.05 for o in stairs)-3.42)<.001
checks['windowsOnLeft']=all(o.location.x<-6 for o in S.objects if o.name.startswith('왼쪽 창 유리'))
checks['stairsOnRight']=all(o.location.x>4 for o in stairs)
checks['rearDoorPosition']=all(o.location.y<-5 for o in S.objects if o.name.startswith('자동문 이동 유리'))
leafs=[bpy.data.objects.get('Lift 1 · 왼쪽 문 [슬라이드]'),bpy.data.objects.get('Lift 1 · 오른쪽 문 [슬라이드]')]
assert all(leafs),'missing elevator leaves'
S.frame_set(1);closed=[o.location.x for o in leafs]
bpy.context.view_layer.update()
origin=Vector((-1.8,5.8,1.6))
closed_rays=[]
for x in [-3.28,-3.0,-2.7,-2.4,-2.1,-1.82,-1.78,-1.5,-1.2,-.9,-.6,-.32]:
    target=Vector((x,2.8,1.6));v=target-origin
    hit,point,normal,index,obj,matrix=S.ray_cast(bpy.context.evaluated_depsgraph_get(),origin,v.normalized(),distance=v.length)
    if not(hit and point.y>3.0):print('CLOSED_FRONT_RAY',x,hit,tuple(point),obj.name if obj else None,flush=True)
    closed_rays.append(hit and point.y>3.0)
checks['closedCabinFrontBlocksPassage']=all(closed_rays)
S.frame_set(72);opened=[o.location.x for o in leafs]
bpy.context.view_layer.update()
target=Vector((-1.8,2.8,1.6));v=target-origin
hit,*_=S.ray_cast(bpy.context.evaluated_depsgraph_get(),origin,v.normalized(),distance=v.length)
checks['openCabinEntranceIsClear']=not hit
checks['elevatorDoorMotion']=all(abs(abs(a-b)-.9)<.00001 for a,b in zip(closed,opened))
clear_width=(opened[1]-leafs[1].dimensions.x/2)-(opened[0]+leafs[0].dimensions.x/2)
checks['openElevatorClearWidth']=1.79<clear_width<1.82
front_emergency=bpy.data.objects.get('Cabin · 전면 비상 버튼 [높이 2.15m]')
side_emergency=bpy.data.objects.get('Cabin · 측면 비상 버튼 [높이 1.28m]')
checks['frontEmergencyHigh']=front_emergency is not None and abs(front_emergency.location.z-2.15)<.001
checks['sideEmergencyLower']=side_emergency is not None and abs(side_emergency.location.z-1.28)<.001
front_panel=bpy.data.objects.get('Cabin · 전면 패널 실버 테두리')
checks['frontConsoleWide']=front_panel is not None and front_panel.dimensions.x>.5
side_panel=bpy.data.objects.get('Cabin · 측면 전체 패널 실버 테두리')
checks['sidePanelAboveRail']=side_panel is not None and side_panel.location.z-side_panel.dimensions.z/2>1.02+.031
checks['sideTwelveFloorButtons']=len([o for o in S.objects if o.name.startswith('Cabin · 측면 ') and '층 면' in o.name])==12
checks['sideOpenCloseButtons']=len([o for o in S.objects if o.name.startswith('Cabin · 측면 문 개폐 면')])==2
parents=[o for o in S.objects if o.name.startswith('자동문 ') and '[애니메이션]' in o.name]
checks['automaticDoorLeaves']=len(parents)==2
checks['automaticDoorMotion']=all(abs(abs(o.location.x)-1.3)<.00001 for o in parents)
checks['editableKoreanText']=sum(o.type=='FONT' and any('\uac00'<=c<='\ud7a3' for c in o.data.body) for o in S.objects)>=10
checks['defaultClosedInteriorView']=loaded_default_frame==1 and S.camera.name=='02 · 엘리베이터 내부'
checks['allMeshCoordinatesFinite']=all(all(abs(v)<1e6 for v in o.location) for o in S.objects)
report={'file':bpy.data.filepath,'blender':bpy.app.version_string,'checks':checks,'allPassed':all(checks.values()),'objects':len(S.objects),'meshes':sum(o.type=='MESH' for o in S.objects),'polygons':sum(len(o.data.polygons) for o in S.objects if o.type=='MESH'),'materials':len(bpy.data.materials),'collections':len(bpy.data.collections),'cameras':[o.name for o in S.objects if o.type=='CAMERA'],'animatedObjects':sum(o.animation_data is not None for o in S.objects),'closedDoorCenters':closed,'openDoorCenters':opened}
(WORK/'Reopen_Validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print('REOPEN_VERIFICATION '+json.dumps(report,ensure_ascii=False),flush=True)
assert report['allPassed'],[k for k,v in checks.items() if not v]
