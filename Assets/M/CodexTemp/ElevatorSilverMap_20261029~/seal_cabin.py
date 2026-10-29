import bpy
from pathlib import Path
WORK=Path(__file__).resolve().parent
for o in bpy.context.scene.objects:
    if o.name.startswith('Cabin · 출입구 내부 측면 마감'):
        o.location.y=3.41
        o.dimensions.y=.38
bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=str(WORK/'엘베_맵_실버.blend'),compress=True)
