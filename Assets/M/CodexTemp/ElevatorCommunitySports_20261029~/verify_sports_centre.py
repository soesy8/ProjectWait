"""Reopen the self-contained result and check this request's visible and spatial changes."""
import bpy,json
from pathlib import Path
from mathutils import Vector
WORK=Path(__file__).resolve().parent
S=bpy.context.scene
initial_frame=S.frame_current;initial_camera=S.camera.name
source=json.loads((WORK/'Latest_Inspection.json').read_text(encoding='utf-8'))
build=json.loads((WORK/'Build_Validation.json').read_text(encoding='utf-8'))
old={o['name']:o for o in source['keyObjects']}
C={}
C['mainExteriorFloor1']=bpy.data.objects['Lift 1 · 층수'].data.body=='1'
C['mainInteriorFloor1']=bpy.data.objects['Cabin · 전면 층수'].data.body=='1'
C['unusedRightLiftFloor3']=bpy.data.objects['Lift 2 · 층수'].data.body=='3'
C['lobbyFloor1']=bpy.data.objects['로비 월 그래픽 층수'].data.body=='1F'
C['upperLandingFloor2']=bpy.data.objects['계단실 층 안내'].data.body=='2F'
C['unnecessaryTextRemoved']=all(bpy.data.objects.get(o['name']) is None for o in build['removedText'])
C['fewerTextObjects']=sum(o.type=='FONT' for o in S.objects)<len(source['texts'])
C['entireSidePanelShifted45cm']=all(abs(bpy.data.objects[name].location.y-old[name]['loc'][1]-.45)<.00002 for name in build['movedSideObjects'])
panel=bpy.data.objects['Cabin · 측면 전체 패널 실버 테두리']
C['sidePanelRetainsHeight']=abs(panel.location.z-old[panel.name]['loc'][2])<.00002
C['sidePanelAboveHandrail']=panel.location.z-panel.dimensions.z/2>1.051
C['sideTwelveFloorButtons']=len([o for o in S.objects if o.name.startswith('Cabin · 측면 ') and '층 면' in o.name])==12
C['sideOpenCloseRetained']=len([o for o in S.objects if o.name.startswith('Cabin · 측면 문 개폐 면')])==2
for plane in ['전면','측면']:
    C[plane+'FirstFloorIlluminated']=bpy.data.objects[f'Cabin · {plane} 01층 링'].data.materials[0].name.startswith('19') and bpy.data.objects[f'Cabin · {plane} 02층 링'].data.materials[0].name.startswith('08')
bell=bpy.data.objects['Cabin · 전면 비상 버튼 [높이 2.15m]'];plate=bpy.data.objects['Cabin · 승강기 식별 정보판']
C['identityBelowEmergencyAboveFloors']=plate.location.z+plate.dimensions.z/2<bell.location.z-.059 and plate.location.z-plate.dimensions.z/2>1.83+.048
C['identityBuildingAndCar']=bpy.data.objects['Cabin · 식별 건물명'].data.body=='한빛 생활체육센터' and 'HB-EL-01' in bpy.data.objects['Cabin · 식별 호기와 번호'].data.body
C['identityReadableFont']=bpy.data.objects['Cabin · 식별 건물명'].data.size>=.0359 and 'Bold' in bpy.data.objects['Cabin · 식별 건물명'].data.font.name
images=[img for img in bpy.data.images if img.name=='엘리베이터_안전포스터.png']
C['posterTexturePacked']=len(images)==1 and images[0].packed_file is not None
C['posterResolution']=len(images)==1 and tuple(images[0].size)==(1024,1536)
C['posterAssigned']=bpy.data.objects['Cabin · 안전 포스터 이미지 [내장 PNG]'].data.materials[0].name.startswith('25')
C['allFontsPacked']=all(f.packed_file is not None for f in bpy.data.fonts if f.filepath and f.filepath!='<builtin>')
C['noExternalLibraries']=len(bpy.data.libraries)==0
C['allMeshMaterialsPresent']=all(o.data.materials and all(o.data.materials) for o in S.objects if o.type=='MESH')
C['metricUnits']=S.unit_settings.system=='METRIC' and S.unit_settings.scale_length==1
C['sixCameras']=sum(o.type=='CAMERA' for o in S.objects)==6
C['scenarioEmbedded']=bpy.data.texts.get('건물콘셉트_시나리오.txt') is not None
C['defaultClosedInteriorView']=initial_frame==1 and initial_camera=='02 · 엘리베이터 내부'
# Preserve the actual closed cabin and open passage through the independently animated doors.
origin=Vector((-1.8,5.8,1.6));S.frame_set(1);bpy.context.view_layer.update()
blocked=[]
for x in [-3.28,-3,-2.7,-2.4,-2.1,-1.82,-1.78,-1.5,-1.2,-.9,-.6,-.32]:
    v=Vector((x,2.8,1.6))-origin
    hit,p,*_=S.ray_cast(bpy.context.evaluated_depsgraph_get(),origin,v.normalized(),distance=v.length)
    blocked.append(hit and p.y>3)
C['closedCabinStillSealed']=all(blocked)
S.frame_set(72);bpy.context.view_layer.update();v=Vector((-1.8,2.8,1.6))-origin
hit,*_=S.ray_cast(bpy.context.evaluated_depsgraph_get(),origin,v.normalized(),distance=v.length)
C['openCabinPassageClear']=not hit
report={'file':bpy.data.filepath,'allPassed':all(C.values()),'checks':C,'objects':len(S.objects),'textObjects':sum(o.type=='FONT' for o in S.objects),'sourceTextObjects':len(source['texts']),'posterPacked':C['posterTexturePacked'],'initialFrame':initial_frame,'initialCamera':initial_camera}
(WORK/'Reopen_Validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print('SPORTS_CENTRE_REOPEN '+json.dumps(report,ensure_ascii=False),flush=True)
assert report['allPassed'],[k for k,v in C.items() if not v]
