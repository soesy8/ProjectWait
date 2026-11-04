import bpy,json,os
from mathutils import Vector
o=bpy.data.objects['Cube'];pts=[o.matrix_world@v.co for v in o.data.vertices]
out={'vertices':[list(p) for p in pts],'polygons':[list(p.vertices) for p in o.data.polygons]}
with open(r'D:\project\ProjectWait\Assets\M\CodexTemp\FriendImprovement_20261104~\body_topology.json','w') as f:json.dump(out,f)
for ax in (0,2):
 values={}
 for p in pts:
  if ax==2 and p.z>2.05:continue
  if ax==0 and (p.z<2.8 or abs(p.x)<1):continue
  key=round(abs(p[ax]) if ax==0 else p[ax],3);values[key]=values.get(key,0)+1
 print('AXIS',ax, sorted(values.items()))
