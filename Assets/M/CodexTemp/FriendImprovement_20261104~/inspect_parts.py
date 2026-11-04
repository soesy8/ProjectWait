import bpy,json,os
from mathutils import Vector
out=[]
for o in bpy.data.objects:
 if o.type!='MESH':continue
 adj={v.index:set() for v in o.data.vertices}
 for e in o.data.edges:
  a,b=e.vertices;adj[a].add(b);adj[b].add(a)
 remain=set(adj);parts=[]
 while remain:
  stack=[remain.pop()];ids=[]
  while stack:
   i=stack.pop();ids.append(i)
   for j in adj[i]:
    if j in remain:remain.remove(j);stack.append(j)
  pts=[o.matrix_world@o.data.vertices[i].co for i in ids]
  parts.append({'count':len(ids),'bounds':[[round(min(p[k] for p in pts),4),round(max(p[k] for p in pts),4)] for k in range(3)]})
 out.append({'object':o.name,'parts':parts})
with open(r'D:\project\ProjectWait\Assets\M\CodexTemp\FriendImprovement_20261104~\source_parts.json','w') as f:json.dump(out,f,indent=2)
