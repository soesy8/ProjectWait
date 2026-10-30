import bpy,json
from pathlib import Path
WORK=Path(__file__).resolve().parent
S=bpy.context.scene
data={'file':bpy.data.filepath,'collections':[{'name':c.name,'count':len(c.objects)} for c in bpy.data.collections],'objects':[{'name':o.name,'type':o.type,'loc':list(o.location),'size':list(o.dimensions),'parent':o.parent.name if o.parent else None,'collections':[c.name for c in o.users_collection],'text':o.data.body if o.type=='FONT' else None} for o in S.objects]}
(WORK/'Source_Inspection.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
print('SOURCE_INSPECTED',len(S.objects),flush=True)
