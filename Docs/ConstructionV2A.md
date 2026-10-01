# Construction V2A: Build Mode & Building Placement

Implemented 2026-09-29. This report supersedes the prototype Build UI instructions in BuildingPlacementAndConstruction.md. No V2B work, commits or pushes.

## Focused performance and rotation refinement — 2026-09-29

This pass addressed only the reported placement/completion stalls and free rotation.

1. **Placement hitch root cause.** A valid placement synchronously called `NavMeshSurface.BuildNavMesh` after creating the site. The isolated full bake cost about 10.38 seconds in the current Studio scene. End-to-end Casting Office placement cost about 11.55 seconds. For comparison, Stage 1 template instantiation cost about 4.02 ms, pure construction-plan generation averaged 2.86 ms, and the Casting site generated 66 dressing transforms. Navigation was the material stall; finance, domain instance/work creation, template cloning and procedural dressing were not.

2. **Completion hitch root cause.** Completion activated the retained finished hierarchy and then synchronously rebuilt the same full navigation surface. The final transition cost about 12.80 seconds; an isolated completed-state bake cost about 13.06 seconds. Renderer restore, collider/behaviour activation, registration, worker release and routing refresh were below the timing noise beside that bake. The finished facility was already retained inside the site, so there was no rebuilding of final art to remove.

3. **Performance change.** Placement, completion and cancellation now request a coalesced asynchronous update of the existing runtime `NavMeshData`. Related requests in one frame settle before source collection, and a request arriving during an update causes one subsequent consolidated update. The only synchronous full bake remains initial New Studio construction composition, before these player transitions. Worker dispatch and production retry wait for navigation readiness. Unity-object work remains on the main thread; only Unity's supported `UpdateNavMeshDataAsync` path performs the bake asynchronously.

4. **Navigation safety.** A construction site's existing public-exclusion object now also owns a stationary carving `NavMeshObstacle`. It protects the active NavMesh immediately while the authoritative surface update is pending. At completion, the previously baked construction hole remains in the active data until the final-building update completes, so the asynchronous transition does not temporarily open a route through the structure. Applicant, employee, Construction Worker, Services and entrance routing continue to use the same surface/settings. The 0.025 m detail rule for Services and operational Stage 1 remains unchanged.

5. **After evidence.** In the one post-change runtime sanity pass, valid Casting Office placement returned in about 62.50 ms with the async update pending and carving active, versus 11.55 seconds before. Final completion returned in about 11.34 ms with the async update pending, versus 12.80 seconds before. Both background updates completed, and the final state was `Operational / Complete / 37°`. The navigation job itself still takes roughly 10–15 seconds in this Editor scene; it no longer blocks a single gameplay frame. Source collection, site presentation and template activation remain synchronous and are the costs to watch as lot/content complexity grows.

6. **Construction presentation and completion lifecycle.** Procedural scaffolding, fence, foundation, material-pile generation and appearance were unchanged. The same finished hierarchy created at placement stays attached to `ConstructionSiteView`; completion restores and activates it in place. `Planned -> UnderConstruction -> Complete/Operational`, identity, finance, phase work and simulation-speed ownership are unchanged.

7. **Free rotation controls.** A quick R press/release rotates exactly +90°. Shift held when R begins rotates exactly -90°. Holding R for at least 0.18 seconds and moving horizontally at least 5 pixels enters free rotation at 0.35 degrees per pixel. The building position is frozen for the entire R gesture, so horizontal pointer movement changes yaw around the authoritative placement pivot without moving the intended placement point. Releasing R commits the displayed arbitrary yaw and immediately returns pointer/camera control.

8. **Angle assistance and feedback.** Free rotation remains continuous. Angles within 2° of a 15° multiple magnetize to that useful angle; values outside the range remain arbitrary. A compact rounded-degree readout replaces the ordinary feedback line only while free rotation is active and disappears on release. Tap rotation is independent of the magnet and remains exactly 90°.

9. **Input and fidelity.** While the R gesture is active, right-drag camera rotation is released and placement confirmation is disarmed. Keyboard camera yaw is still independent; all camera controls resume immediately on R release. Existing oriented structural, construction-clearance and entrance rectangles update from the same `BuildingPose`. Ghost meshes/outlines/entrance arrow, validation, `PlacedBuilding`, construction root, dressing, entrance and retained final building all receive the exact yaw. The sanity pass preserved 37° through completion with no 90° snap-back.

10. **Files in the refinement.** `StudioConstructionDriver.cs`, `ConstructionSiteView.cs`, `StudioBuildMode.cs`, `BuildingRotationGesture.cs` plus its Unity-generated `.meta`, `BuildModeUI.cs`, `StudioCameraController.cs`, `ConstructionV2APlacementTests.cs`, and this document. No scene, prefab, material, input-action or Stage 1 presentation asset was changed.

11. **Validation.** Unity 6000.6.3f1 compiled the runtime and Editor assemblies. Seven focused EditMode tests passed: the existing placement/finance/transform coverage, the three V2A confirmation/footprint tests, and two rotation gesture/magnet tests. One short runtime session validated immediate placement/completion return, carving presence, async navigation completion, lifecycle completion and 37° transform fidelity. No full suite, screenshot, camera sweep, visual validation or broad profile was run. The unrelated pre-existing `NewStudioSceneAuthoring` `ExtensionOfNativeClass` Console error remains.

12. **Manual tests for this refinement.**

    1. Place Administration, Casting Office and Stage 1 and judge whether the confirmation frame remains responsive while panning or zooming immediately afterward. Wait for workers to receive routes once navigation finishes.
    2. Complete each type and continue moving the camera during the transition. Verify employees/applicants route correctly once the background update finishes and no one routes through the site/building during it.
    3. Tap R four times and verify exact 90° steps; repeat with Shift+R for exact reverse steps. Quick taps with small mouse movement must not enter free rotation.
    4. Hold R, wait briefly, then move horizontally. Verify the pivot/position stays fixed, yaw moves continuously, right-drag does not rotate the camera during the gesture, and camera rotation resumes on release.
    5. Approach 15°, 30°, 45°, 60° and 90° slowly and feel the light magnet. Stop at an arbitrary angle such as 27° or 37° and verify it remains unsnapped. Check that the temporary angle readout appears only during the free gesture.
    6. At arbitrary yaw, test lot edges, Services/gate reservations, existing buildings/sites, entrance clearance, ground support and blockers. Confirm red/green feedback follows the rotated outlines.
    7. Confirm a valid arbitrary angle and compare ghost, footprint, fence/scaffolding, entrance, site root and completed building. None should snap back. Repeat Escape/back and camera-gesture cancellation checks.
    8. Verify Pause stops construction and 1x/2x/3x retain their existing progression rates. Place or complete two sites near together to check coalesced navigation updates and later worker routing.

Construction worker distribution, construction-art improvements, Stage 1 presentation, Casting Office rebuilding, Construction Presentation V2C, HUD V2 and all other milestones remain deferred.

1. **Build-mode architecture.** `StudioBuildMode` owns open/category/placement state and facility placement input. It composes the existing `StudioConstructionDriver` and domain `BuildingConstructionService`. `BuildModeUI` owns the world-tool presentation. `BuildingPlacementPreview` owns disposable render-only preview objects. No parallel construction or finance service was introduced.

2. **UI components.** A runtime uGUI canvas adds a circular Build button beside the bottom-left world view, three circular category controls, a compact bottom palette and a feedback strip. Existing management-navigation buttons are unchanged. During placement, category controls and the full palette collapse, leaving Build and the feedback strip. Existing UI factory, colors and TMP text are reused; the simple circular button sprite is generated once and released with the UI. No thumbnail pipeline or new building art.

3. **Categories.** `BuildCategory` represents Facilities, Sets, and LandscapingAndDecorations. `BuildPaletteItem` contains category, identity, display name, cost, availability callback and selection action. This UI contract does not require a facility definition. Only the facility placement backend is implemented in this milestone; future content providers can supply sibling-category entries/actions. Sets and Landscaping & Decorations show an honest empty state, with no invented content. Existing specialized production-set construction remains separate.

4. **Facilities.** Administration ($18,000), Casting Office ($12,000), and Stage 1 ($50,000) appear because they already have construction definitions and scene-template support. Administration is the palette label for the existing Studio Headquarters definition; IDs and economics are unchanged. Missing templates, tutorial/era locks and insufficient money disable entries. Script Office was investigated: it has legacy station/recruitment support but no independently placeable definition, authored placement footprint, construction cost or work requirement. It is omitted. Administration completion still creates the existing starter writing stations and writer recruitment area.

5. **Palette population.** The controller adapts `Service.Definitions` into catalog entries. The UI iterates entries and filters by the selected category. It refreshes availability at 0.2-second intervals; selection and confirmation check authority again. There is no separate button branch for each facility. The two name aliases do not alter content or routing.

6. **Placement preview.** Copies the selected template's mesh hierarchy and local transforms into new render-only objects. Shares source meshes, uses two owned preview materials, excludes lower LOD duplicates and respects inactive children. It never instantiates source gameplay components. There are no colliders, lights, navigation components, source-material mutations or per-frame material creation. A transparent green/red mesh, structural outline, construction clearance, entrance clearance and approach-to-door arrow communicate the placement. Preview transforms follow the ground projection in LateUpdate, after camera Update. No OS cursor movement.

7. **Footprints.** Existing per-definition `PlacementRect` metadata remains authoritative: structural footprint, construction clearance and entrance clearance. Off-center rectangles rotate about the instance origin. The same `BuildingPose` drives the ghost, domain validation and final site. Geometry does not derive from arbitrary renderer bounds or assume all architectures of a gameplay type have equal dimensions. More complex footprint shapes remain a future extension.

8. **Validity.** Existing oriented-rectangle/SAT validation rejects lot-bound violations, reserved gate/Services areas, completed buildings and other construction sites, including entrance reservations. The new optional world-validation callback runs inside service validation and confirmation. `StudioBuildLot` verifies supported flat ground at the rectangle edges and interior samples no more than 2 m apart, then performs oriented physics overlap checks for blocking geometry. Mobile NavMesh agents and recognized ground slabs are excluded from solid-building blockers. Full overlap buffers reject conservatively. Existing entrance-to-Services NavMesh connectivity, definition availability, finite poses and affordability checks remain. Invalid clicks retain placement and show a reason without charging.

   The current flat lot uses Y=0. Optional serialized build-surface colliders support explicit authoring; the existing-scene adapter recognizes broad, shallow ground slabs at lot elevation. The backdrop does not grant build rights: the owned-lot rectangle is checked first. This is deliberately not terrain simulation. Preview validity refreshes at 10 Hz; confirmation always revalidates immediately.

9. **Rotation.** R rotates +90 degrees; Shift+R rotates -90 degrees by default. `RotationSnap` remains configurable for future content. `PositionSnap` remains configurable and defaults off for precise unsnapped placement. Rotation updates mesh, outlines, arrow, validation and committed site transform together.

10. **Input and camera.** Build handles its own keyboard controls before existing selection input. While open, it owns world selection/placement clicks, without disabling the camera, simulation or Input System. A fresh left press and release within 6 pixels confirms; UI clicks, typing, right/middle gestures, scrolling, keyboard camera movement and focus loss invalidate the pending click. Placement follows the camera after its Update. A closing-frame guard prevents click-through to person/building selection. Opening Build explicitly cancels a carry using its existing restoration path and reports the return to the prior position. A pending dismissal confirmation must be resolved first. Camera source code and bindings were not changed.

11. **Finance.** Uses the existing `IStudioFinanceService` through construction. Palette affordability is live, and `Place` revalidates immediately before `TryRecordExpense`. Only successful confirmation records the existing BuildingConstruction expense once. Preview/cancellation makes no transaction or reservation. No cost rebalance or new economy model.

12. **Successful placement.** Creates a unique `PlacedBuilding` in Planned/SitePreparation state with the exact X/Z/yaw, plus the existing strategic work item. `StudioConstructionDriver` creates its `ConstructionSiteView` root at that pose, instantiates the existing finished-content template under it, gates operational behavior/collision, generates the existing site presentation and rebuilds navigation. Stage instance identity continues through the established system. The palette returns after placement. The facility is not instantly operational.

13. **What happens next.** Existing code immediately dispatches available Construction Workers and retries dispatch later. Travel contributes zero work. With no arrived workers, the project remains waiting at zero progress. Arrival allows existing strategic construction work, phase changes and eventual completion. Existing dressing/scaffolding and staged visuals are preserved, not expanded. Only completion activates operational gameplay, production routing and recruitment. Pausing strategic time continues to prevent construction progress. These existing behaviors were not replaced with a V2B implementation.

14. **Cancel/back.** Escape during placement disposes the ghost/materials and returns to the selected category. Escape from the category palette or category picker closes Build. B or the Build button toggles the complete tool. Disabling the controller also closes it and releases ownership. Cancellation does not create a site, work item, reservation or financial transaction. This cancel path concerns uncommitted previews; the existing service's committed-site cancellation/refund rules are unchanged.

15. **New Studio.** Existing construction-driver composition adds the UI automatically; no Inspector wiring is required. Startup composition, Services-only lot, zero employees, $250,000 starting capital and opening applicant wave were not changed. No progression facility was reintroduced at startup. Tutorial availability remains authoritative; use tutorial OFF/Skip when manually checking every facility without progression gates.

16. **Files changed/added.**

    | File | Change |
    | --- | --- |
    | Assets/Scripts/Presentation/Buildings/StudioBuildMode.cs | Replace prototype management-style Build panel with controller/input flow |
    | Assets/Scripts/Presentation/UI/BuildModeUI.cs | New category model, catalog-entry contract, circular controls and bottom palette |
    | Assets/Scripts/Presentation/Buildings/BuildingPlacementPreview.cs | New render-only ghost and footprint/entrance presentation |
    | Assets/Scripts/Presentation/Buildings/StudioBuildLot.cs | Resolve valid ground and validate world blockers |
    | Assets/Scripts/Presentation/Buildings/StudioConstructionDriver.cs | Expose template lookup and connect world validation |
    | Assets/Scripts/Domain/Buildings/BuildingConstructionService.cs | Availability reason and optional confirmation-time world validator |
    | Assets/Scripts/Presentation/Interaction/PersonInteractionController.cs | Narrow public carry-cancel and dismissal-state integration |
    | Assets/Scripts/Presentation/Selection/StudioSelectionController.cs | Respect Build ownership before person/world clicks |
    | Assets/Tests/Editor/ConstructionV2APlacementTests.cs | Three focused EditMode tests |
    | Docs/ConstructionV2A.md | This completion report and manual checklist |
    | Docs/BuildingPlacementAndConstruction.md | Mark old Build UI instructions as superseded |

    Unity generated `.meta` files for the three new scripts. Existing asset GUIDs, scene/prefab serialization, materials and source art were not edited by this task. The working tree already contained extensive unrelated modifications; no cleanup or repository-wide preservation audit was performed.

17. **Validation.** Unity 6000.6.3f1 compiled and loaded the runtime and Editor assemblies. Five focused EditMode tests passed, zero failures: two existing instance/rotation and invalid-placement/finance tests, plus three new world-revalidation, changing-affordability and offset-footprint tests. Result: `Temp/ConstructionV2A/results.xml`.

    Exactly one brief Play Mode sanity run instantiated the Build UI/catalog, selected Casting Office, and checked the ghost (6 mesh renderers, 0 colliders, 0 MonoBehaviours). Back left the palette open with no active ghost. Confirming at X=0, Z=0, yaw=90 created a Planned site at exactly that transform and charged $12,000. Build then closed and Play Mode was exited without saving runtime state. Tutorial was skipped only in that transient sanity session.

    No feature exception appeared in the final Console read. The Console did report `'SilverScreen.Editor.NewStudioSceneAuthoring' is missing the class attribute 'ExtensionOfNativeClass'!` during import and Play entry. That untouched authoring component is outside this change; the sanity run still completed. No screenshots, visual validation, input-device automation, repeated Play runs, full-suite run or corrective pass. Subjective layout, ghost appearance, pointer behavior and camera UX remain for manual evaluation.

18. **Exact manual checks.**

    1. Start New Studio. Verify Services only, zero employees, $250,000 and opening applicants. Turn tutorial OFF/Skip to inspect all three facility choices, or follow its gates.
    2. Click bottom-left Build (also test B). Check category-circle placement and labels at your usual resolution. Open each category; Sets and Landscaping should show empty content. Return to Facilities and inspect names/costs/states.
    3. Select Casting Office. Check that the palette collapses, recognizable ghost follows the cursor, and structural/entrance outlines align. Move over UI and back to the lot; no site should be created by UI clicks.
    4. Test R and Shift+R through four rotations, especially Stage 1's off-center entrance. Check the preview and final site orientation against the pointer position. Check all three facility footprints.
    5. While placing, use WASD/arrows, Q/E, middle drag, right drag, edge pan and wheel zoom. Combine left clicks with active camera gestures; they must not place a site. A normal clean left click after the gesture should work.
    6. Move outside the lot, across the gate/Services reservation, across a solid world prop, and over an existing site/building. Check red feedback and reason text. Invalid clicks must retain the ghost, preserve cash and add no site.
    7. Place one facility on valid ground without workers. Check exact position/yaw, one cost deduction, site-only state and return to Facilities. Then hire a Construction Worker normally and confirm the existing travel/progression behavior resumes; placement itself must not unlock the facility.
    8. Cancel a preview with Escape: category palette returns, no site or charge. Escape again closes Build. Repeat open/select/cancel and B toggling. Start while carrying a person: the person should return safely to the prior position, with no duplicate drop/site action. Check normal carrying, Services hover/pin/cutaway and employee actions after closing Build.
    9. When cash is below a facility cost, verify its disabled state. If cash changes while a ghost is active, confirmation must refuse an unaffordable purchase. Also test Pause/1x/2x/3x: placement UI and camera should work without granting construction progress while paused.

19. **Deferred.** No construction worker AI, productivity balancing, new phases, site art, partial models, scaffolding expansion, Script Office construction definition, new sets/decorations, HUD V2, Star Maker or Casting Office art rebuild. Existing construction progression remains intact. No V2B work will follow this handoff automatically.
