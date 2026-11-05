"""Offline runtime-mesh preparation; the editable FBX and Blender sources are preserved.
Run Blender in background with --python this_file -- <M work directory>.
"""
import bpy
import bmesh
import json
import math
import os
import sys
from mathutils import Vector
from mathutils.bvhtree import BVHTree

work = os.path.abspath(sys.argv[sys.argv.index('--') + 1])
if '/Assets/M/' not in work.replace('\\', '/'):
    raise RuntimeError('All outputs must stay inside Assets/M')
out_dir = os.path.join(work, 'Output')
os.makedirs(out_dir, exist_ok=True)
reports = []

def build(data):
    mesh = bpy.data.meshes.new(data['name'])
    mesh.from_pydata(data['vertices'], [], [data['triangles'][i:i+3] for i in range(0, len(data['triangles']), 3)])
    mesh.update()
    if not data.get('bones'):
        # Render exports split vertices at normal/material seams; collision needs positions only.
        bm = bmesh.new()
        bm.from_mesh(mesh)
        bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=0.000001)
        bm.to_mesh(mesh)
        bm.free()
        mesh.update()
    obj = bpy.data.objects.new(data['name'], mesh)
    bpy.context.collection.objects.link(obj)
    if data.get('bones'):
        for i in range(max(max(b) for b in data['bones']) + 1):
            obj.vertex_groups.new(name=str(i))
        for i, (bones, weights) in enumerate(zip(data['bones'], data['weights'])):
            for bone, weight in zip(bones, weights):
                if weight > 0:
                    obj.vertex_groups[bone].add([i], weight, 'REPLACE')
        for name in ['colors', 'style', 'shade', 'ink', 'bands']:
            attribute = mesh.attributes.new(name=name, type='FLOAT_COLOR', domain='POINT')
            attribute.data.foreach_set('color', [v for row in data[name] for v in row])
        attribute = mesh.attributes.new(name='authored_normals', type='FLOAT_VECTOR', domain='POINT')
        attribute.data.foreach_set('vector', [v for row in data['normals'] for v in row])
    return obj

def evaluated(obj, ratio=None):
    mod = obj.modifiers.new('Runtime simplification', 'DECIMATE')
    if ratio is None:
        mod.decimate_type = 'DISSOLVE'
        mod.angle_limit = math.radians(.01)
        mod.use_dissolve_boundaries = False
    else:
        mod.decimate_type = 'COLLAPSE'
        mod.ratio = ratio
        mod.use_collapse_triangulate = True
    deps = bpy.context.evaluated_depsgraph_get()
    deps.update()
    result = obj.evaluated_get(deps)
    mesh = bpy.data.meshes.new_from_object(result, preserve_all_data_layers=True, depsgraph=deps)
    obj.modifiers.remove(mod)
    mesh.calc_loop_triangles()
    return mesh

def deviation(original, candidate):
    source_tree = BVHTree.FromPolygons(original['vertices'], [original['triangles'][i:i+3] for i in range(0,len(original['triangles']),3)], all_triangles=True)
    target_tree = BVHTree.FromPolygons([v.co for v in candidate.vertices], [list(t.vertices) for t in candidate.loop_triangles], all_triangles=True)
    worst = 0
    # Check both directions, including all surviving vertices and a dense source sample.
    for vertex in candidate.vertices:
        nearest = source_tree.find_nearest(vertex.co)
        if nearest[0] is not None:
            worst = max(worst, nearest[3])
    step = max(1, len(original['vertices']) // 24000)
    for point in original['vertices'][::step]:
        nearest = target_tree.find_nearest(Vector(point))
        if nearest[0] is not None:
            worst = max(worst, nearest[3])
    return worst

bpy.ops.wm.read_factory_settings(use_empty=True)
for filename in sorted(os.listdir(os.path.join(work, 'Input'))):
    if not filename.endswith('.json'):
        continue
    with open(os.path.join(work, 'Input', filename), encoding='utf-8-sig') as stream:
        data = json.load(stream)
    obj = build(data)
    skin = bool(data.get('bones'))
    ratio = 1
    if skin:
        dimensions = obj.dimensions
        diagonal = Vector(dimensions).length
        limit = max(.001, diagonal * .002)
        for ratio in [.25, .4, .5, .65, .8, .95, 1.0]:
            mesh = evaluated(obj, ratio)
            error = deviation(data, mesh)
            if error <= limit:
                break
            bpy.data.meshes.remove(mesh)
        if error > limit:
            raise RuntimeError('Surface error exceeded limit: ' + data['name'])
    else:
        # Keep collision surfaces within 3 mm; thin barriers and floor openings must survive.
        for ratio in [.15, .35, .6, .8, 1.0]:
            mesh = evaluated(obj, ratio)
            error = deviation(data, mesh)
            if error <= .003:
                break
            bpy.data.meshes.remove(mesh)
        if error > .003:
            raise RuntimeError('Collision surface error exceeded limit: ' + data['name'])
    before = len(data['triangles']) // 3
    after = len(mesh.loop_triangles)
    if not skin and after >= before * .9:
        bpy.data.meshes.remove(mesh)
        bpy.data.objects.remove(obj, do_unlink=True)
        continue
    result = dict(name=data['name'], path=data['path'], ratio=ratio, error=error,
                  vertices=[list(v.co) for v in mesh.vertices],
                  triangles=[i for t in mesh.loop_triangles for i in t.vertices])
    if skin:
        result['normals'] = [list(v.vector) for v in mesh.attributes['authored_normals'].data]
        for name in ['colors', 'style', 'shade', 'ink', 'bands']:
            result[name] = [list(v.color) for v in mesh.attributes[name].data]
        result['bones'], result['weights'] = [], []
        for vertex in mesh.vertices:
            influences = sorted([(g.group, g.weight) for g in vertex.groups if g.weight > 0], key=lambda p: -p[1])[:4]
            total = sum(p[1] for p in influences)
            if not influences or total <= 0:
                raise RuntimeError('Unweighted vertex')
            bones = [p[0] for p in influences]
            weights = [p[1] / total for p in influences]
            result['bones'].append(bones + [0] * (4-len(bones)))
            result['weights'].append(weights + [0] * (4-len(weights)))
    with open(os.path.join(out_dir, filename), 'w', encoding='utf-8') as stream:
        json.dump(result, stream, separators=(',', ':'))
    reports.append(dict(name=data['name'], before=before, after=after, ratio=ratio, error=error))
    print(data['name'], before, '->', after, 'surface error', error, flush=True)
    bpy.data.meshes.remove(mesh)
    bpy.data.objects.remove(obj, do_unlink=True)
with open(os.path.join(work, 'GeometrySummary.json'), 'w', encoding='utf-8') as stream:
    json.dump(reports, stream, indent=2)
print('DONE', len(reports), flush=True)
