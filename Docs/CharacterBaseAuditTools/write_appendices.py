"""Turn local audit metadata into reviewable Markdown; does not copy purchased geometry."""
import json
from collections import defaultdict
from pathlib import Path

root = Path('TestResults/CharacterBaseAudit')
inventory = json.loads((root / 'inventory.json').read_text())
audit = json.loads((root / 'source-audit.json').read_text())
lines = ['# Purchased human base: source evidence', '',
         'Read-only Blender 5.0.1 audit, 2026-10-02. Counts exclude control widgets unless explicitly listed. '
         'FBX-imported `use_deform` flags are not reliable control/deform classifications; source .blend flags and skin membership are authoritative.', '',
         'Source root: `C:/Users/Sez/Documents/Assets/rigged-stylized-human-body-base-mesh-rigged-3d-models`.', '',
         '## Complete file inventory', '', '| Relative path (spelling preserved) | Bytes | SHA-256 |', '| --- | ---: | --- |']
for file in inventory:
    lines.append(f"| `{file['path']}` | {file['bytes']} | `{file['sha256']}` |")
lines += ['', '## Byte-identical variants', '']
groups = defaultdict(list)
for file in inventory:
    groups[file['sha256']].append(file['path'])
for group in groups.values():
    if len(group) > 1:
        lines.append('- ' + ' = '.join('`' + path + '`' for path in group))
for entry in audit:
    lines += ['', '## ' + entry['path'], '']
    if 'error' in entry:
        lines.append('ERROR: ' + entry['error'])
        continue
    lines += ['| Armature | Bones | Source deform flag | Source non-deform flag | Roots |', '| --- | ---: | ---: | ---: | --- |']
    for arm in entry['armatures']:
        roots = ', '.join(b['name'] for b in arm['bones'] if b['parent'] is None)
        lines.append(f"| `{arm['name']}` | {arm['bone_count']} | {arm['deform_count']} | {arm['bone_count']-arm['deform_count']} | {roots} |")
    lines += ['', '| Mesh | Vertices | Triangles | Shapes excluding Basis | Material slots | UV maps |', '| --- | ---: | ---: | ---: | --- | --- |']
    for mesh in entry['meshes']:
        if not mesh['armature_modifiers'] and not mesh['shape_keys']:
            continue
        lines.append(f"| `{mesh['name']}` | {mesh['vertices']} | {mesh['triangles']} | {max(0,len(mesh['shape_keys'])-1)} | {', '.join(str(m) for m in mesh['materials']) or '(none)'} | {', '.join(mesh['uv_maps'])} |")
    for mesh in entry['meshes']:
        if mesh['shape_keys']:
            lines += ['', f"Exact shape order on `{mesh['name']}`:", '',
                      '`' + '`, `'.join(s['name'] for s in mesh['shape_keys']) + '`', '']
    if entry['path'].endswith('.blend') and 'FacialRig.blend' in entry['path']:
        for arm in entry['armatures']:
            lines += ['', f"### `{arm['name']}` source bone hierarchy", '', '| Bone | Parent | Deforms | Constraints |', '| --- | --- | --- | --- |']
            for b in arm['bones']:
                lines.append(f"| `{b['name']}` | `{b['parent'] or '(root)'}` | {b['deform']} | {', '.join(b['constraints'])} |")
    lines += ['', 'Source action datablocks: ' + (', '.join(entry['actions']) or '(none)') + '.']
Path('Docs/CharacterBaseSourceEvidence.md').write_text('\n'.join(lines) + '\n', encoding='utf-8')
