import bpy,json
from pathlib import Path
WORK=Path(__file__).resolve().parent
S=bpy.context.scene
data={'source':bpy.data.filepath,'scene':dict(S.items()),'defaultFrame':S.frame_current,'camera':S.camera.name,'texts':[{'name':o.name,'text':o.data.body,'loc':list(o.location),'rotation':list(o.rotation_euler)} for o in S.objects if o.type=='FONT'],'keyObjects':[{'name':o.name,'type':o.type,'loc':list(o.location),'size':list(o.dimensions)} for o in S.objects if o.name.startswith(('Cabin · 측면','Cabin · 전면','Cabin · 안전 안내','로비 월','층 안내','계단실 층','계단 하부','Lift 1 · 층','Lift 2 · 층'))]}
(WORK/'Latest_Inspection.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(data,ensure_ascii=False),flush=True)
