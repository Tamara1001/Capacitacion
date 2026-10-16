"""Helpers para generar modelos low/mid-poly del Simulador LOTO por script (Blender 5.x).

Convenciones (ver .agents/ASSETS.md):
- 1 unidad = 1 metro, Z arriba en Blender. El "frente" del objeto mira a -Y.
- Los materiales se llaman exactamente como los de Unity (M_GB_<Nombre>) para que
  el importador los remapee por nombre a los assets existentes.
- El origen de cada asset es su punto lógico (pivot de palanca, centro de válvula, piso…).
"""
import math
import os

import bpy

COLORS = {
    "Floor": "#B8BDC4", "Wall": "#D9DCE0", "Graphite": "#2E3238", "PumpBlue": "#2F5D8C",
    "PipeGreen": "#3E8E5A", "Orange": "#F28C1B", "Red": "#D3272E", "Yellow": "#FFC629",
    "Green": "#3BB273", "White": "#F2F2F2", "Steel": "#8A9099", "Dark": "#1C1F24",
    "Blue": "#2D7FF9", "Worker": "#4A6B8A", "Pilot": "#3BB273", "Skin": "#C9A27E",
    "Glass": "#9FC3D9", "Light": "#FFF6E0",
}


def _hex(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4))


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def mat(name):
    full = f"M_GB_{name}"
    m = bpy.data.materials.get(full)
    if m is None:
        m = bpy.data.materials.new(full)
        r, g, b = _hex(COLORS[name])
        m.diffuse_color = (r, g, b, 1.0)
    return m


def _finish(obj, name, material, parent, bevel, segments=2, smooth_angle=35.0):
    obj.name = name
    obj.data.name = name
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel > 0:
        mod = obj.modifiers.new("Bevel", "BEVEL")
        mod.width = bevel
        mod.segments = segments
        mod.limit_method = "ANGLE"
        bpy.ops.object.modifier_apply(modifier=mod.name)
    obj.data.materials.clear()
    obj.data.materials.append(mat(material))
    bpy.ops.object.shade_smooth_by_angle(angle=math.radians(smooth_angle))
    if parent is not None:
        set_parent(obj, parent)
    return obj


def _active():
    return bpy.context.view_layer.objects.active


def box(name, size, loc, material, bevel=0.004, parent=None, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc, rotation=[math.radians(a) for a in rot])
    obj = _active()
    obj.scale = size
    return _finish(obj, name, material, parent, bevel)


def cyl(name, radius, depth, loc, material, rot=(0, 0, 0), verts=24, bevel=0.003, parent=None):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=radius, depth=depth, location=loc,
                                        rotation=[math.radians(a) for a in rot])
    return _finish(_active(), name, material, parent, bevel)


def cone(name, r1, r2, depth, loc, material, rot=(0, 0, 0), verts=24, parent=None):
    bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=r1, radius2=r2, depth=depth, location=loc,
                                    rotation=[math.radians(a) for a in rot])
    return _finish(_active(), name, material, parent, 0)


def sphere(name, radius, loc, material, scale=(1, 1, 1), segments=24, rings=12, parent=None):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, radius=radius, location=loc)
    obj = _active()
    obj.scale = scale
    return _finish(obj, name, material, parent, 0, smooth_angle=80)


def torus(name, major, minor, loc, material, rot=(0, 0, 0), major_seg=32, minor_seg=12, parent=None):
    bpy.ops.mesh.primitive_torus_add(major_radius=major, minor_radius=minor, major_segments=major_seg,
                                     minor_segments=minor_seg, location=loc,
                                     rotation=[math.radians(a) for a in rot])
    return _finish(_active(), name, material, parent, 0, smooth_angle=80)


def half_sphere(name, radius, loc, material, scale=(1, 1, 1), parent=None):
    """Media esfera (cúpula) con la base en z=loc.z."""
    obj = sphere(name, radius, loc, material, scale=(1, 1, 1))
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="DESELECT")
    bpy.ops.object.mode_set(mode="OBJECT")
    for v in obj.data.vertices:
        v.select = v.co.z < -1e-4
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.delete(type="VERT")
    bpy.ops.object.mode_set(mode="OBJECT")
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if parent is not None:
        set_parent(obj, parent)
    return obj


def empty(name, loc=(0, 0, 0), parent=None, rot=(0, 0, 0)):
    obj = bpy.data.objects.new(name, None)
    obj.empty_display_size = 0.05
    obj.location = loc
    obj.rotation_euler = [math.radians(a) for a in rot]
    bpy.context.scene.collection.objects.link(obj)
    if parent is not None:
        set_parent(obj, parent)
    return obj


def set_parent(child, parent):
    bpy.context.view_layer.update()
    world = child.matrix_world.copy()
    child.parent = parent
    child.matrix_world = world


def join(name, objects, parent=None):
    """Une mallas en una sola (menos draw calls). Conserva los materiales de cada parte."""
    bpy.ops.object.select_all(action="DESELECT")
    for o in objects:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.join()
    obj = _active()
    obj.name = name
    obj.data.name = name
    if parent is not None:
        set_parent(obj, parent)
    return obj


def ring_of(name_prefix, count, radius, z, size, material, center=(0, 0), axis="Z", parent=None, offset=0.0):
    """Pernos u otros elementos repetidos en círculo. Devuelve la lista de objetos."""
    parts = []
    for i in range(count):
        a = 2 * math.pi * i / count + offset
        if axis == "Z":
            loc = (center[0] + math.cos(a) * radius, center[1] + math.sin(a) * radius, z)
        elif axis == "X":
            loc = (z, center[0] + math.cos(a) * radius, center[1] + math.sin(a) * radius)
        else:  # Y
            loc = (center[0] + math.cos(a) * radius, z, center[1] + math.sin(a) * radius)
        parts.append(box(f"{name_prefix}{i}", size, loc, material, bevel=0.001))
    return parts


def export(out_dir, asset_name):
    path = os.path.join(out_dir, f"SM_{asset_name}.fbx")
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=False,
        object_types={"EMPTY", "MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=True,
        mesh_smooth_type="OFF",
        use_mesh_modifiers=True,
        add_leaf_bones=False,
        bake_anim=False,
    )
    tris = sum(len(o.data.polygons) for o in bpy.data.objects if o.type == "MESH")
    print(f"[export] {os.path.basename(path)}  faces={tris}")
    return path
