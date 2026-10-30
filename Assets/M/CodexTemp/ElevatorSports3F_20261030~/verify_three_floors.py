import bpy,json,math
from pathlib import Path
from mathutils import Vector
WORK=Path(__file__).resolve().parent
S=bpy.context.scene
source=json.loads((WORK/'Source_Inspection.json').read_text(encoding='utf-8'))
old={o['name']:o for o in source['objects']}
initial_frame=S.frame_current;initial_camera=S.camera.name
H=3.96;Z3=7.92
C={}
def obj(n):return bpy.data.objects[n]
def third(n):return obj('3F · '+n)
def close(a,b,tol=.0001):return abs(a-b)<tol
C['currentFloor3']=S.get('현재층')==3 and third('Lift 1 · 층수').data.body=='3' and third('Cabin · 전면 층수').data.body=='3'
C['unusedLiftFloor5']=third('Lift 2 · 층수').data.body=='5' and obj('Lift 2 · 층수').data.body=='5'
C['originalGroundFloor1']=obj('Lift 1 · 층수').data.body=='1' and obj('로비 월 그래픽 층수').data.body=='1F'
C['thirdFloorActuallyElevated']=close(third('로비 구조 바닥').location.z-old['로비 구조 바닥']['loc'][2],Z3)
C['groundCabinAndFurniturePreserved']=all(all(close(obj(n).location[i],old[n]['loc'][i]) and close(obj(n).dimensions[i],old[n]['size'][i]) for i in range(3)) for n in ['로비 구조 바닥','Cabin · 뒤 벽 구조','Cabin · 석재 중앙 바닥','벤치 하부 프레임','벤치 좌방석 0','왼쪽 창 유리 00'])
protected=[]
for rec in source['objects']:
    n=rec['name']
    if rec['type']=='MESH' and any(c.startswith(('01','03','04','05','06','08','09','10')) for c in rec['collections']):
        actual=bpy.data.objects.get(n)
        if actual:protected.append(all(close(actual.location[i],rec['loc'][i]) and close(actual.dimensions[i],rec['size'][i]) for i in range(3)))
C['protectedGroundMeshesUnmoved']=all(protected)
C['independentThirdFloorMeshes']=all(third(n).data is not obj(n).data for n in ['로비 구조 바닥','Cabin · 뒤 벽 구조','벤치 하부 프레임','왼쪽 창 유리 00'])
second=next(c for c in bpy.data.collections if c.name.startswith('20 · 2층'))
C['secondFloorHasNoFurnitureOrSigns']=len(second.objects)>0 and all(o.type=='MESH' and not any(v in o.name for v in ['가구','버튼','보관함','벤치','화분']) for o in second.objects)
C['secondFloorAtCorrectHeight']=all(close(o.location.z+o.dimensions.z/2,H) for o in second.objects if '슬래브' in o.name)
ret=[o for o in S.objects if o.name.startswith('2F→3F · ') and '디딤판' in o.name]
C['returnFlight22Steps']=len(ret)==22
C['returnFlightReaches3F']=close(max(o.location.z+.05 for o in ret),Z3)
C['firstFloorStairOriginal19Retained']=len([o for o in S.objects if o.name.startswith('계단 ') and '디딤판' in o.name])==19
C['upperStairExtensionTo2F']=len([o for o in S.objects if o.name.startswith('1F→2F · 추가 디딤판')])==3
S.frame_set(1);bpy.context.view_layer.update()
for name,y,z in [('secondFloorConnection',4.9,H+1.2),('thirdFloorConnection',-2.21,Z3+1.2)]:
    hit,*_=S.ray_cast(bpy.context.evaluated_depsgraph_get(),Vector((6.25,y,z)),Vector((1,0,0)),distance=.9)
    C[name+'Clear']=not hit
directory=obj('3F · 층별 시설 안내 프레임');poster=third('Cabin · 안전 안내 프레임')
C['directoryNextToPoster']=close(directory.location.z,poster.location.z) and .75<directory.location.y-poster.location.y<1.2
C['directorySixFacilityRows']=len([o for o in S.objects if o.name.startswith('3F · 안내 시설 ')])==6
C['directoryThirdFloorFacilities']='농구장' in obj('3F · 안내 시설 3F').data.body
C['directoryHasNoCurrentLocationHighlight']=bpy.data.objects.get('3F · 현재층 안내 강조') is None and bpy.data.objects.get('3F · 현위치 표기') is None and 'Bold' not in obj('3F · 안내 층 3F').data.font.name
C['sideControlHeightsRetained']=close(third('Cabin · 측면 비상 버튼 [높이 1.28m]').matrix_world.translation.z,Z3+1.28)
C['frontEmergencyAndIdentificationRetained']=close(third('Cabin · 전면 비상 버튼 [높이 2.15m]').matrix_world.translation.z,Z3+2.15) and 'HB-EL-01' in third('Cabin · 식별 호기와 번호').data.body
C['both3FPanelCurrentLights']=all(third(f'Cabin · {p} 03층 링').data.materials[0].name.startswith('19') and third(f'Cabin · {p} 01층 링').data.materials[0].name.startswith('08') for p in ['전면','측면'])
C['basketballCourtAndLockers']=obj('3F · 농구 연습장 마루') is not None and len([o for o in S.objects if o.name.startswith('3F · 개인 보관함')])==10
C['courtAt3F']=close(obj('3F · 농구 연습장 마루').location.z,Z3+.015)
C['thirdFloorRearDoorHasFloorBeyond']=close(obj('3F · 체육관 연결 복도 바닥').location.z+.09,Z3)
background=[o for o in S.objects if o.name.startswith('배경 · ') and o.name.endswith('건물') and '맞은편' not in o.name and o.type=='MESH']
C['tallerBackgroundAtGroundLevel']=len(background)>=6 and max(o.dimensions.z for o in background)>=30 and all(close(o.location.z-o.dimensions.z/2,-.6) for o in background)
C['treesStayAtGroundLevel']=all(close(obj(n).location.z,rec['loc'][2]) for n,rec in old.items() if n.startswith(('외부 조경 나무','외부 조경 잎')))
C['allImagesPacked']=all(im.packed_file is not None for im in bpy.data.images if im.source=='FILE')
C['allFontsPacked']=all(f.packed_file is not None for f in bpy.data.fonts if f.filepath and f.filepath!='<builtin>')
C['noLinkedLibraries']=len(bpy.data.libraries)==0
C['noMissingMaterials']=all(o.data.materials and all(o.data.materials) for o in S.objects if o.type=='MESH')
C['default3FInterior']=initial_frame==1 and initial_camera=='3F · 02 · 엘리베이터 내부'
C['cameraAt3FEyeHeight']=close(S.camera.location.z,Z3+1.65)
leafs=[third('Lift 1 · 왼쪽 문 [슬라이드]'),third('Lift 1 · 오른쪽 문 [슬라이드]')]
closed=[o.location.copy() for o in leafs]
S.frame_set(72);bpy.context.view_layer.update();opened=[o.location.copy() for o in leafs]
C['elevatedDoorAnimationRetainsHeight']=all(close(a.z,b.z) and close(b.z,Z3+1.405) and close(abs(a.x-b.x),.9) for a,b in zip(closed,opened))
origin=Vector((-1.8,5.8,Z3+1.6));v=Vector((-1.8,2.8,Z3+1.6))-origin
hit,*_=S.ray_cast(bpy.context.evaluated_depsgraph_get(),origin,v.normalized(),distance=v.length)
C['3FOpenCabinPassageClear']=not hit
report={'file':bpy.data.filepath,'allPassed':all(C.values()),'checks':C,'checkedGroundMeshes':len(protected),'objects':len(S.objects),'floorHeight':H,'thirdFloorElevation':Z3,'sourceObjects':len(source['objects']),'cameras':[o.name for o in S.objects if o.type=='CAMERA']}
(WORK/'Reopen_Validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print('THREE_FLOORS_REOPEN '+json.dumps(report,ensure_ascii=False),flush=True)
assert report['allPassed'],[k for k,v in C.items() if not v]
