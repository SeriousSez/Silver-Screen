"""New SilverScreen adult authoring skeleton. Contains no human mesh generator.

Run in Blender 5 with --background --factory-startup --python. The script
imports no art, opens no rejected prototype and exports no runtime character.
"""
from pathlib import Path
import json
import math

import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parent
OUTPUT = ROOT / "SS_Adult_Authoring.blend"
VERSION = "silverscreen-adult-authoring-v1"
HEIGHT = 1.78


def main():
    # Never destroy subsequent mesh authoring in this workspace.
    if OUTPUT.exists():
        with bpy.data.libraries.load(str(OUTPUT), link=False) as (source, target):
            if source.meshes:
                raise RuntimeError("Authoring workspace contains meshes; edit the source blend directly.")
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.unit_settings.system = 'METRIC'
    scene.unit_settings.scale_length = 1.0
    scene.render.fps = 30
    scene['status'] = 'SKELETON SCAFFOLD ONLY - NO CHARACTER OR VALIDATION'
    scene['concept_authority'] = 'Reference/ApprovedConcept.png'
    scene['anatomical_height_metres'] = HEIGHT
    scene['source_lineage'] = 'Fresh explicit landmarks; no imported art or rejected prototype data'

    armature = bpy.data.armatures.new('SS_Adult_SemanticSkeleton')
    rig = bpy.data.objects.new('SS_Adult_Root', armature)
    scene.collection.objects.link(rig)
    bpy.context.view_layer.objects.active = rig
    rig.select_set(True)
    rig.show_in_front = True
    rig['contract'] = VERSION
    rig['ground_origin'] = 'Metres; Z=0; anatomical feet midpoint'
    rig['forward'] = '-Y'
    rig['bind_state'] = 'T pose; anatomical landmarks provisional until art/deformation review'
    rig['geometry_status'] = 'No meshes or skin weights yet'
    bones = []
    mapping = {}
    bpy.ops.object.mode_set(mode='EDIT')

    def bone(name, head, tail, parent=None, human=None, role='deform'):
        b = armature.edit_bones.new(name)
        b.head, b.tail = head, tail
        if parent:
            b.parent = armature.edit_bones[parent]
        b.use_deform = role == 'deform'
        # Stable limb bending axes will be refined with the finished body.
        if name.startswith(('Left', 'Right')) and any(part in name for part in ('Arm', 'Hand')):
            b.align_roll(Vector((0, -1, 0)))
        bones.append({'name': name, 'parent': parent, 'head': list(head), 'tail': list(tail), 'role': role})
        if human:
            mapping[human] = name
        return b

    bone('Root', (0, 0, 0), (0, 0, .12), role='root')
    bone('Hips', (0, 0, .98), (0, 0, 1.075), 'Root', 'Hips')
    bone('Spine', (0, 0, 1.075), (0, 0, 1.23), 'Hips', 'Spine')
    bone('Spine1', (0, 0, 1.23), (0, 0, 1.37), 'Spine', 'Chest')
    bone('Spine2', (0, 0, 1.37), (0, 0, 1.47), 'Spine1', 'UpperChest')
    bone('Neck', (0, 0, 1.47), (0, -.004, 1.555), 'Spine2', 'Neck')
    bone('Head', (0, -.004, 1.555), (0, 0, 1.745), 'Neck', 'Head')
    bone('Jaw', (0, -.012, 1.625), (0, -.087, 1.574), 'Head', 'Jaw')
    for side, sign in [('Left', 1), ('Right', -1)]:
        def point(x, y, z):
            return (x * sign, y, z)
        bone(side + 'Eye', point(.032, -.071, 1.665), point(.032, -.096, 1.665), 'Head', side + 'Eye')
        bone(side + 'Shoulder', point(.035, 0, 1.465), point(.185, 0, 1.46), 'Spine2', side + 'Shoulder')
        bone(side + 'Arm', point(.185, 0, 1.46), point(.475, 0, 1.46), side + 'Shoulder', side + 'UpperArm')
        bone(side + 'ForeArm', point(.475, 0, 1.46), point(.725, 0, 1.46), side + 'Arm', side + 'LowerArm')
        bone(side + 'Hand', point(.725, 0, 1.46), point(.803, 0, 1.46), side + 'ForeArm', side + 'Hand')
        fingers = [('Index', -.027, .075), ('Middle', -.009, .084), ('Ring', .009, .078), ('Pinky', .026, .060)]
        for finger, y, length in fingers:
            x = .796 - (.008 if finger == 'Pinky' else 0)
            parent = side + 'Hand'
            for segment, fraction, human_segment in [(1, .45, 'Proximal'), (2, .32, 'Intermediate'), (3, .23, 'Distal')]:
                end = x + length * fraction
                name = side + 'Hand' + finger + str(segment)
                bone(name, point(x, y, 1.46), point(end, y, 1.46), parent,
                     side + ('Little' if finger == 'Pinky' else finger) + human_segment)
                x, parent = end, name
        thumb = [(.745, -.020, 1.449), (.769, -.043, 1.445), (.794, -.053, 1.445), (.816, -.062, 1.445)]
        parent = side + 'Hand'
        for i, human_segment in enumerate(('Proximal', 'Intermediate', 'Distal')):
            name = side + 'HandThumb' + str(i + 1)
            bone(name, point(*thumb[i]), point(*thumb[i + 1]), parent, side + 'Thumb' + human_segment)
            parent = name

        bone(side + 'UpLeg', point(.096, 0, .98), point(.103, -.018, .505), 'Hips', side + 'UpperLeg')
        bone(side + 'Leg', point(.103, -.018, .505), point(.102, 0, .087), side + 'UpLeg', side + 'LowerLeg')
        bone(side + 'Foot', point(.102, 0, .087), point(.102, -.137, .042), side + 'Leg', side + 'Foot')
        bone(side + 'ToeBase', point(.102, -.137, .042), point(.102, -.233, .027), side + 'Foot', side + 'Toes')

        # Helpers are explicit deformation slots, not proof of implemented skinning.
        for source in ('Arm', 'ForeArm', 'UpLeg', 'Leg'):
            original = armature.edit_bones[side + source]
            a, z = original.head.copy(), original.tail.copy()
            for number, t in enumerate((.33, .66), 1):
                start = a.lerp(z, t)
                end = a.lerp(z, min(.99, t + .2))
                bone(side + source + 'Twist' + str(number), start, end, side + source)
        bone(side + 'Scapula', point(.07, .045, 1.407), point(.16, .045, 1.45), 'Spine2')

        for kind, a, z in (
            ('HandIK', point(.725, 0, 1.46), point(.725, -.06, 1.46)),
            ('FootIK', point(.102, 0, .087), point(.102, -.09, .087)),
            ('ElbowPole', point(.475, .35, 1.46), point(.475, .35, 1.52)),
            ('KneePole', point(.103, -.4, .505), point(.103, -.4, .565)),
        ):
            bone(side + kind, a, z, 'Root', role='authoring_anchor')
        bone(side + 'Grip', point(.77, -.012, 1.448), point(.77, -.065, 1.448), side + 'Hand', role='interaction_anchor')
        bone(side + 'SoleContact', point(.102, -.08, 0), point(.102, -.14, 0), side + 'Foot', role='contact_anchor')
        bone(side + 'EyeAim', point(.032, -.75, 1.665), point(.032, -.81, 1.665), 'Root', role='authoring_anchor')

    bone('HeadwearSocket', (0, 0, 1.742), (0, 0, 1.792), 'Head', role='attachment_anchor')
    bone('LookOrigin', (0, -.071, 1.665), (0, -.141, 1.665), 'Head', role='interaction_anchor')
    bone('SeatContact', (0, .035, .88), (0, .035, .94), 'Hips', role='contact_anchor')
    bpy.ops.object.mode_set(mode='OBJECT')
    roles = {b['name']: b['role'] for b in bones}
    for role in sorted(set(roles.values())):
        collection = armature.collections.new(role)
        for b in armature.bones:
            if roles[b.name] == role:
                collection.assign(b)
                b['semantic_role'] = role
                b['validated_skinning'] = False
    for name in ('Anatomy_Body', 'Anatomy_Head', 'Eye_Left', 'Eye_Right',
                 'Brows', 'Hair', 'FacialHair_Optional', 'Garment_Shirt',
                 'Garment_Waistcoat', 'Garment_Jacket', 'Garment_Trousers',
                 'Shoe_Left', 'Shoe_Right', 'Hat_Fedora', 'Accessory_Tie',
                 'Accessory_Belt'):
        collection = bpy.data.collections.new(name)
        scene.collection.children.link(collection)
        collection['binding_contract'] = VERSION
        collection['status'] = 'EMPTY - MESH NOT AUTHORED'

    armature.display_type = 'OCTAHEDRAL'
    assert tuple(rig.location) == (0, 0, 0)
    assert tuple(rig.scale) == (1, 1, 1)
    assert len(set(b['name'] for b in bones)) == len(bones)
    assert len(mapping) == 55, (len(mapping), mapping)
    for b in bones:
        assert (Vector(b['tail']) - Vector(b['head'])).length > .001
        assert all(math.isfinite(v) for v in b['head'] + b['tail'])
        if b['parent']:
            assert b['parent'] in armature.bones
    assert len(bpy.data.meshes) == 0
    scene.camera = None
    bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT))
    contract = {'version': VERSION, 'status': 'provisional authoring scaffold',
                'unit': 'metre', 'up': '+Z', 'forward': '-Y', 'rootOrigin': 'feet-ground',
                'anatomicalHeightMetres': HEIGHT, 'bones': bones, 'unityHumanoidMapping': mapping,
                'note': 'Unity Avatar validity, skinning and deformation have not been tested.'}
    (ROOT / 'rig-contract.json').write_text(json.dumps(contract, indent=2) + '\n', encoding='utf-8')
    check = {'status': 'scaffold structure checks passed', 'boneCount': len(bones),
             'humanoidMappedBoneCount': len(mapping), 'meshCount': 0, 'animationClipCount': 0,
             'rootPosition': list(rig.location), 'rootScale': list(rig.scale),
             'uniqueBoneNames': True, 'finiteNonzeroBones': True, 'parentReferencesValid': True,
             'importedArtSources': [], 'skinningValidated': False, 'deformationValidated': False,
             'humanoidAvatarValidated': False, 'visualGatePassed': False,
             'runtimeValidated': False}
    (ROOT / 'authoring-check.json').write_text(json.dumps(check, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(check))


if __name__ == '__main__':
    main()
