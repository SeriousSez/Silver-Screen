# SilverScreen Unity context

Inspected 2026-09-22 for the Stage 1 clean-candidate production geometry gate.
Repository: `C:/Repos/Unity/Silver Screen`; HEAD
`1c994a4653e157d3b9f68cea4098f1419fde9f93`. The tree already contained extensive
uncommitted work. This task adds isolated candidate/source/review files.

- Unity **6000.5.9f1**, URP **17.5.0**, Linear colour, active Windows 64-bit target.
- `Assets/Scenes/Studio.unity` is the sole enabled build scene. Main Camera is
  perspective, FOV 60, saved at `(0,18,-15)`, pitch 52 degrees.
- The active PC URP asset, native volume, procedural sky and directional light
  establish studio daylight. See `Docs/Rendering/StudioDaylightBaseline.md`.
- Input System 1.20 is used by the management camera. AI Navigation 2.0.14 and
  Unity Test Framework 1.7.0 are installed. No gameplay networking was confirmed;
  Multiplayer Center's presence alone is not evidence of multiplayer gameplay.
- First-party code uses Unity's default assemblies (no first-party asmdefs found):
  domain logic under `Assets/Scripts/Domain`, services/application logic separate
  from `Assets/Scripts/Presentation`, Editor tests under `Assets/Tests/Editor`.
  Namespaces begin `SilverScreen`; private serialized fields use `_camelCase`.
- `StudioBuildingView` provides building type and interaction anchor.
  `StudioWorldRouter` discovers active building views, registers facility IDs,
  rejects duplicate Stage numbers/IDs and prepares production presentation.
- `SoundStageIdentity` owns the Stage instance number and stable facility ID;
  `StageArchitecturalSign` assembles independent glyph meshes at runtime/Edit Mode.
- Production stations, actor marks, blocking points and slate positions are
  separate layout components. `PrototypeProductionCamera` and `LiveFilmingPlayback`
  supply Live Camera View. Their existing Stage coordinates need migration before
  integrating the clean candidate; see its review report.
- Art generators under `ArtSource` own source blends and exports. Production
  environment art is under `Assets/SilverScreen/Environment`. Follow root,
  Environment and ArtSource `AGENTS.md`. Preserve all asset GUIDs and references.
- Existing official Unity AI Assistant MCP provider: package **2.19.0-pre.2**,
  local relay `C:/Users/Sez/.unity/relay/relay_win.exe --mcp --project-path ...`.
  Unity connection, scene reads, C# execution, Console and Main Camera URP captures
  were verified. This session needed approved access outside the shell sandbox
  to connect to the local named pipe. No provider/package was installed.
- Stage candidate revision 04 preserves the approved production architecture and
  adds focused grounding, opening, service/lighting clearance, hardware, canvas,
  crown, blackout and roof-material corrections. Source GROUND=0 is now the
  lowest export surface; FLOOR=0.08. Seven personnel sweeps, fifteen wall-lamp
  checks and 290 roof-blackout coverage rays pass. See the review report.
  The candidate exports 29 semantic meshes, nine shared 1K PBR sets and about
  630K triangles. The 47-view gallery includes all seven service doors and eight
  low perimeter angles. Numbering remains instance-driven. Original Stage and
  Administration remain unchanged. No LOD/optimization or prop extraction.
  Candidate remains unregistered with the production router, with five broad
  colliders, 31 permanent worklights and a 128px interior reflection. Roof
  renderers use an exterior probe anchor. Repeated refresh/save is verified
  to retain one lighting root with 31 lights (a prefab-override duplication
  bug was fixed in the review companion). No Play Mode/build/navigation work.
- The user's Studio acquired an unsaved extra candidate placement during this
  pass. It remains open and dirty; it was neither saved nor reloaded. Revision 04
  seats that extra candidate as one Undo-recorded whole-root placement from
  Y=0.200003 to actual ground Y=0; child transforms are not offset. A snapshot
  is under the candidate's `ReviewSnapshots` folder. MCP validation and captures
  use `OpenPreviewReview()` so the working scene is preserved. Do not blindly
  run an old restore script or open a scene in Single mode over unsaved work.

Evidence: ProjectVersion, package manifest, EditorBuildSettings, camera/controller,
StudioBuildingView, StudioWorldRouter, Stage identity/sign components, production
layout/camera components, Studio daylight document, Administration generator export
conventions and MCP scene probes. This is focused art-task context, not a full
architecture or release-readiness audit.
