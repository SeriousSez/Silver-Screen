# SilverScreen Repository Instructions

## Project Context

- SilverScreen is a Hollywood movie-studio management game beginning in 1930.
- Inspect the existing architecture, implementation, and nearby assets before making substantial changes.
- Keep changes scoped to the requested task. Do not redesign unrelated systems or overengineer speculative future systems.
- Preserve existing gameplay integrations unless the task explicitly requires changing them.
- Prototype geometry, placeholder assets, and temporary coordinates are not authoritative when they conflict with an approved production design.
- Prefer fixing the authoritative, reproducible source over applying one-off changes to generated or exported output.

## Implementation and Compatibility

- This is a Unity 6000.5 project using URP. Preserve Unity `.meta` files, asset GUIDs, serialized references, and existing material assignments when editing or regenerating assets.
- Follow the existing C# organization and `SilverScreen` namespaces; keep domain logic separate from presentation and Unity-facing behavior where the current architecture does so.
- Maintain compatibility with future save/load, building condition, time of day, and historical progression where relevant, but do not implement those systems prematurely.
- Do not hardcode assumptions that every building of one gameplay type has identical architecture. Future variants may differ by construction era, size, quality, and technology.

## Validation and Reporting

- Do not claim visual validation unless the actual result was inspected.
- Use available Unity MCP tooling for scene, import, and visual validation when appropriate; inspect the result under the game's real rendering setup.
- Report incomplete validation, untested behavior, and tool limitations honestly.
- Do not commit or push unless explicitly requested.
