# Procedural construction dressing and scaffolding

## Scope and audit

This extends the existing freeform placement/construction milestone in Unity 6000.6.3f1 (URP). `BuildingConstructionService`, its authoritative work accounting, costs, phases, completion events, and persistence contracts are unchanged. The existing `ConstructionSiteView`, `ConstructionPhaseVisuals`, and `StudioConstructionDriver` are extended rather than replaced by another construction system.

Previously, a site displayed a slab, one scaffold bar, and crates; changing phase increased the bar height. Finished content remained inactive until Operational, so the optional reveal bindings could not progressively show the model. Workers used three entrance-approach positions. The public site blocker reserved the hard footprint; navigation rebaked at placement/completion/cancellation. All of those ownership boundaries remain, with a separate render gate now allowing real final-model renderers to appear before their gameplay/colliders activate.

The actual current exports were inspected: Casting has seven renderers (body/roof/doors/windows/trim/sign), HQ has the equivalent seven, and Stage 1 has 32 renderers including interior floor, primary structure, exterior walls, roof and fixtures. Authored height metadata is 4.92 m, 8.82 m, and 11.43 m respectively. Saved scene/prefab/material/mesh assets are not edited.

## Kit and replacement contract

`ConstructionVisualKit` is a ScriptableObject mapping semantic module types to optional prefabs:

- ScaffoldVertical, ScaffoldHorizontal, ScaffoldBrace, ScaffoldPlatform, ScaffoldRail
- Ladder, TemporaryFence, SiteMarker
- TimberPile, MaterialPile, Crate, DebrisPile, CanvasOrTarp, WheelbarrowPlaceholder

Missing entries resolve to generated timber-coloured placeholder geometry. The generator produces module requests, never primitive-specific instructions. Add a kit through **Create > SilverScreen > Construction Visual Kit**, assign prefabs, and assign it on `StudioConstructionDriver` before initialization. A driver can be added to the bootstrap object before Play for that serialized authoring; its existing Initialize method still owns session setup. Leaving the kit unassigned uses the default placeholders.

Prefab contract: a centred unit-box module, X=length, Y=height, Z=depth, with its geometry inside that envelope. Module transforms scale that envelope to each request. Kit assets must be passive visual content; colliders and behaviours are disabled on instantiated modules, and the dressing root is excluded from NavMesh collection. Preserve pivots/envelopes when replacing art so tested exclusions remain valid. A later era resolver can select a different kit without changing layout or work accounting. Only one placeholder kit is implemented now.

## Metadata and scaffold generation

`BuildingDefinition.ConstructionVisuals` holds immutable local `ConstructionVisualMetadata`: building height, explicitly scaffoldable `ScaffoldSegment` centreline start/end/height, exclusion rectangles, and material zones. The generator is not keyed by building type. The starter definitions author different side selections; arbitrary line segments are supported, not only four perimeter sides. A helper authors selected rectangle edges for these existing rectangular footprints. A missing profile safely generates no scaffolds rather than guessing an entire cage.

`ConstructionDressingGenerator` produces a deterministic local-space plan tied to the placed instance. `ConstructionSiteView` parents it under the same position/rotation root as the building. No world coordinates or runtime random sequence are used. Interval clipping removes parts of scaffold runs around protected areas before dividing surviving lengths into bays of at most 2.2 m. Bay length is adjusted evenly for the remainder; very short remnants are omitted. Heights determine repeated lifts, with posts, horizontal beams, diagonal braces, platforms, rails and ground-only ladder placeholders. Finishing retains only every third authored segment at one lift, so scaffold reduction precedes completion.

Every emitted module's conservative local ground bounds must fit inside the existing construction clearance and avoid the structural footprint, primary entrance clearance, optional service entrance, authored exclusions, and human-sized activity pads. Material zones also remove scaffold sections. An open front ground aisle joins the entrance to both scaffold ends, keeping worker access clear of fences and braces. Existing placement validation reserves the entire construction clearance against neighbours and lot edges; keeping all dressing inside that reservation makes it respect those constraints without a second placement validator.

Material and equipment positions come from authored zones beside the footprint. Their bounds go through the same exclusion tests. Invalid or unavailable zones are skipped; there is no random litter or material delivery/inventory simulation.

## Phase presentation and reveal

| Authoritative phase | Dressing and final-model presentation |
| --- | --- |
| SitePreparation | Marked footprint, stakes, partial fences, material piles and placeholder equipment; no major scaffold. |
| Foundation | Marked footprint, one-lift scaffold, materials/crates; explicitly authored foundation renderers visible where present. |
| Structure | Repeated scaffold lifts, rails/braces/platforms/ladders, covered materials; real structural renderers visible. |
| Exterior | Major scaffold retained; actual wall and roof groups revealed; footprint marker hidden. |
| Finishing | Selected one-lift runs retained, fencing removed, fewer material zones, remaining detail/inspection work; openings, fixtures, trim/signage visible. |
| Complete / Operational | No generated dressing; temporary footprint/blocker destroyed; original render and component states restored and normal completion integration runs. |

The existing `ConstructionPhaseVisuals` bindings support Foundation, Structure, Walls, Roof, Openings, Fixtures, Details and added Signage. Inspector renderer bindings take precedence. Content-specific adapters bind the names in the three inspected exports without editing imported meshes or prefab assets; they are keyed by content ID, not by gameplay building type. In particular, Casting/HQ's body is one intact mesh: Structure reveals that complete body, not a fabricated frame or a sliced mesh.

For unknown/unbound content, local renderer height provides a conservative fallback: low pieces at Foundation, shorter pieces at Structure, remaining intact renderers at Exterior. This is a readable coarse fallback, not semantic recognition of future architecture. Explicit bindings are recommended for authored art. Rendering uses `forceRenderingOff` and restores the original flag; material assignments, mesh references, hierarchy and geometry are retained. No second under-construction model is created.

Finished content is active for rendering, but its behaviours and physical colliders are gated until the real domain state is Operational. `StudioWorldRouter` skips disabled building views, preventing partially revealed content from entering production routing when another site completes. Native navigation components are restored before MonoBehaviours, allowing the existing Stage door pose guard to establish its final blocker state before the existing completion bake.

## Workers and navigation

The plan exposes instance-associated deterministic GroundWork, FoundationWork, ScaffoldWork, WallWork, MaterialPickup, MaterialDrop, Inspection and Cleanup points. The current construction phases select ground, foundation, scaffold-end, wall-end and inspection work. Pickup/drop/cleanup are semantic positions for later task composition, not implemented hauling or cleaning.

Workers use reachable **ground-level** points in the protected approach aisle, including either scaffold end. The driver checks a nearby NavMesh sample and a complete path and falls back to another suitable point for that slot if necessary. It uses the existing real EmployeeAgent task, turns the employee toward its work target on arrival, and calls the existing `WorkerArrived` boundary. At phase transitions the old assignment is released and the employee re-routes; no construction capacity is earned during that travel. Developer visual overrides never change this authoritative activity selection.

Navigation distinctions:

- Scaffold, fence, material, ladder and other generated kit pieces are visual-only, collider-free/disabled, with NavMesh collection ignored.
- The separate public-exclusion blocker retains the previous footprint, size and centre, remaining active throughout real construction.
- Worker access/entrance/activity clearances reserve free ground space; there is no scaffold or ladder climbing.
- Completion restores the original finished building colliders and runs the existing navigation/capability activation.

The navigation bake implementation and settings are unchanged. The existing operational-Stage 0.025 m synchronous bake can take about **12 seconds**. This remains an existing limitation; no baking optimization, threshold link, door-geometry change, employee-size change or threshold-controller change is part of this milestone.

## Ownership and developer inspection

The site owns its generated dressing root. On a different displayed phase, the old root is immediately deactivated and destroyed, then the new plan is instantiated. Repeated refreshes in the same phase reuse it; there is no per-frame regeneration and no pool. Cancellation removes dressing and the owning site; completion destroys temporary geometry; removing the site destroys all owned children. Placeholder renderers share the existing site material and use property blocks for colour, avoiding per-piece material instances.

In Play Mode, use **SilverScreen > Construction Dressing**, select a placed site, choose a phase and press **Preview selected phase**. Complete previews the Operational appearance only. **Follow real construction** clears the override; closing the window also clears it. The panel reports the authoritative state separately, module counts and real activity points, and can select/frame the site. These controls do not modify work, lifecycle, finance, collision or capabilities. Pause the strategic clock while inspecting if desired. Final mouse/UI and subjective visual approval remain with the user.

## Validation plan and remaining limits

Five focused tests cover transformed module instances and stable instance-associated activities; all three definitions' reservation/access safety; deterministic phase reduction; actual-model reveal and debug authority isolation; completion/cancellation/owner cleanup. The existing six construction/placement tests are the only existing fixture selected. Runtime proof will inspect Casting and Stage at different rotations, use a real hired builder through phase transitions, and check one ordinary operational Stage route. No full suite or large navigation stress campaign is needed.

Proof results are recorded below after validation. Placeholder braces, fences, materials, tarps and wheelbarrow silhouettes are not final historically detailed assets. Unknown content uses coarse whole-renderer reveal. Custom prefab envelopes and explicit reveal bindings need author review. Climbing, mocap, inventory, litter, groundskeeper AI, deterioration, advanced repairs, roads/lot expansion, final UI and navigation optimization remain deferred.

## Changed files

- `Domain/Buildings/ConstructionVisualMetadata.cs`: authored segment/zone/height and activity vocabulary.
- `Domain/Buildings/BuildingDefinition.cs`: optional metadata and three starter profiles.
- `Presentation/Buildings/ConstructionDressingGenerator.cs`: deterministic semantic layout and activity plan.
- `ConstructionVisualKit.cs`: prefab mapping and primitive fallback factory.
- `ConstructionSiteView.cs`: ownership, rendering/gameplay gate, phase updates, reachable activity selection and debug preview.
- `ConstructionPhaseVisuals.cs`: existing-model reveal bindings/adapters/fallback.
- `StudioConstructionDriver.cs`: kit composition, real worker phase activity routing/facing.
- `StudioWorldRouter.cs`: skip gated non-operational views.
- `Editor/ConstructionDressingWindow.cs`: presentation-only inspection controls.
- `Tests/Editor/ConstructionDressingTests.cs`: five focused tests.

Recommended next milestone: author-reviewed 1930s kit prefabs and explicit reveal bindings, with a small representative placement/rotation review. Keep the separately planned navigation-bake performance work in its own milestone.

## Recorded results — 2026-09-28

- Unity project compilation: **one batched compile**, successful, no corrective compile. Ephemeral proof scripts outside Assets compile independently and do not rebuild project assemblies.
- `ConstructionDressingTests`: **5/5 passed**, once.
- `BuildingPlacementAndConstructionTests`: **6/6 passed**, once.
- No complete project suite, repeated passing tests, navigation stress campaign, Computer Use, commit or push.
- Runtime: a zero-staff New Studio generated a real ConstructionWorker applicant. After physical arrival it was hired, placed sites used the normal Build Mode API, and the same employee performed all ten construction-phase activities. Additional candidate generation was disabled only in this temporary proof after hiring to bound unrelated crowd activity.
- Casting: X=-12, Z=-8, yaw=27 degrees. Stage: X=10, Z=12, yaw=35 degrees. All recorded phases preserved entrance exclusion and kept the operational building view disabled until real completion.

| Phase | Casting modules / visible final renderers | Stage modules / visible final renderers |
| --- | ---: | ---: |
| SitePreparation | 13 / 0 | 30 / 0 |
| Foundation | 51 / 0 | 266 / 1 |
| Structure | 101 / 1 | 1,108 / 5 |
| Exterior | 101 / 2 | 1,108 / 10 |
| Finishing | 23 / 7 | 95 / 31 |
| Complete | 0 / 7 | 0 / 31 |

Casting has no separately authored foundation renderer; the shared footprint placeholder conveys that early phase. Renderer counts measure enabled rendering state, not subjective visibility/occlusion approval. The simple footprint slab persists through Structure, so low floor detail can remain covered by that temporary marker until Exterior. Explicit art bindings and a more refined ground marker remain part of the future presentation review.

Each incomplete site exposed 24 stable instance-associated ground activity points. The builder's recorded intents were GroundWork, FoundationWork, ScaffoldWork, WallWork, Inspection at both sites. Example Stage local arrival positions were (-8.50, 1.03, -13.51) for scaffold-end work and (8.49, 1.03, -13.49) for wall work. Strategic acceleration occurred **only after actual arrival**, and each phase transition required another real route. No fake worker or direct writable phase/progress shortcut was used.

At completion both generated dressing roots were absent and the normal operational views were enabled. The same builder then reached Stage's transformed Director station using ordinary NavMesh walking: **0 wall penetration**, **3.5000925 m/s maximum measured horizontal speed**, and no navigation link. The existing access guard was passable with carving disabled in its held-open pose. This was one focused route, not a repeat of the earlier crowd campaign.

Headquarters passed the same profile/generator clearance and scaffold tests; no redundant full HQ runtime visual proof was run. Gameplay reveal/collision gating, debug isolation and object destruction were covered by the new tests. The runtime proof completed in approximately 55 seconds including construction lifecycle bake stalls. Current Unity Console after proof: **0 errors / 0 warnings**.

Evidence is retained in `Logs/ConstructionDressingAndScaffolding`: per-fixture results, the phase/worker/route proof, compilation/current-console evidence and preservation checks. Working before-state copies are in `Temp/DressingMilestone`. Final inspection restores the originally clean saved scene without saving temporary options or constructions.

The Stage placeholder peaks at 1,108 semantic modules (with child render primitives). This is a deliberately simple, event-generated prototype, not a draw-call/object-count optimization claim. Prefab art consolidation or measured mesh batching can be considered during the art/performance pass; no speculative pooling was added.
