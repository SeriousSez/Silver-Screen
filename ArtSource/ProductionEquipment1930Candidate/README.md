# Production equipment — c.1930 candidates

Independent, unbranded art candidates. No building dependency, filming behaviour,
canonical-kit promotion, or replacement of existing Stage 1 equipment.

Run `generate_equipment.py` with Blender 5 in background/factory-startup mode.
It writes the editable `ProductionEquipment1930Candidate.blend`, deterministic
`ArtExports/ProductionEquipment1930Candidate/equipment_meshes.json.gz`, and
`generation_report.json`. Import through the independent Unity editor menu
**SilverScreen → Art → Production Equipment Candidates → Import**.

Two root collections/prefabs: `StudioCamera1930` and `StudioLamp1930`.
The camera separates Tripod, Head, Body, Magazines, Lens and Controls. The lamp
separates Stand, Yoke, Housing, Reflector, Shutters and Cable. The root datum is
floor centre, metres, unit scale; the optical axis is Blender +Y / Unity +Z.
Parts are semantic assemblies, not an animation rig. There are no invisible
film transport internals, cloth simulations, colliders or runtime scripts.

Stage School A1 links these collections in Blender and nests the two Unity
prefabs. Its two lamps share every mesh/material reference. Relocation and yaw
belong to the building instance; equipment generation does not import any
Stage School module. Materials reference the existing PeriodEnvironment1930 kit.

The limited period check informed the design language, not an exact replica:

- The [Science Museum Group's 2709 record](https://collection.sciencemuseumgroup.org.uk/objects/co8084602/bell-and-howell-2709-model-b-35mm-cine-camera-cine-camera)
  documents external magazines, hand operation, turret lenses and a footage
  counter, including a specimen sold in 1929. This candidate uses an original
  three-lens arrangement and generic mechanical detailing without branding.
- [GE and Mole-Richardson's historical press release](https://www.newmediawire.com/news/the-big-picture%3A-ge-and-mole-richardson-celebrate-80-years-of-lighting-the-entertainment-industry-3081456)
  places studio incandescent lighting in the late 1920s. The candidate is an
  open-face incandescent reflector with mechanical shutters and a steel stand;
  it introduces no LED panel or later electronic controls.

These sources establish broad plausibility, not museum-grade reconstruction.
No external photographs or branded graphics were copied into the assets.
