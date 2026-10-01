# Building placement and construction foundation

## Audit and integration decisions

The current project is Unity 6000.6.3f1 with URP and AI Navigation. Existing `StudioBuildingView`, `SoundStageIdentity`, authored local entrance/station transforms, `StudioWorldRouter`, and `StudioFilmingCapabilities` provide building presentation and production routing. They are reused. The former `StarterEstablishmentService` was an instant in-place tutorial prototype; it remains a compatibility domain fixture, but the new-studio runtime and developer window now use real construction.

`SimulationTimeDriver` already owns a strategic clock, scheduler, `WorkService`, and shared `ResourceReservationBook`. Construction uses those services. `StudioFinances` owns spending and `StudioAccountingService` owns salary/upkeep. `RecruitmentCoordinator`, `Candidate`, `CandidateWorldRouter`, and `EmployeeAgent` already supply physical applicants and employees. These are extended rather than duplicated. There is no complete game save system to integrate.

The saved Studio scene remains the populated production sandbox. Choose **SilverScreen > Tutorial and Studio PA > New Studio** before Play to exercise this opening. Turn Tutorial OFF for unrestricted starter construction. This preserves the existing sandbox and saved building assets.

## Definition and instance

`BuildingDefinition` is immutable architecture/content data. Stable definition/content IDs identify Headquarters, Casting Office, and the approved Stage 1 architecture. Definitions contain local structural footprint, construction clearance, entrance clearance, decorative overhang, primary entrance, exterior approach, optional service entrance, interaction/navigation policy IDs, price, required work, phases, capacity, capabilities, professions, era, and upgrade-family boundary. They contain no lot/world position.

`PlacedBuilding` owns a unique instance ID, definition reference, `BuildingPose` (X/Z/yaw), lifecycle, phase/progress, and stage number. Multiple instances share the same architecture. Placed stage clones receive unique facility IDs and stable stage numbers before activation; routing uses those IDs. Existing scene content is currently the source for the content IDs. An asset catalog can replace that adapter later without changing domain placement.

`StudioBuildLot` authors a conservative 76 x 64 m buildable rectangle inside the existing 80 x 68 m fenced campus. It reserves the east vehicle-gate approach and the starter service facility. It identifies the already-owned main property parcel; multiple properties, ownership transfers, and lot expansion are deferred. The large background ground collider does not confer build rights.

## Build Mode and placement validation

**Construction V2A supersedes the prototype controls described in the next paragraph.** See [ConstructionV2A.md](ConstructionV2A.md) for the current circular Build tools, bottom palette, mesh ghost, 90-degree rotation, input arbitration, world validation and manual checklist. The construction/domain foundation below remains in use.

Press **B**, or use **Build / Workforce**, to open the prototype palette. Select a building, move its green/red footprint preview with the mouse on the lot, and click outside the palette to confirm. **R / Shift+R** rotates. The rotation slider permits continuous yaw. Position snapping is optional (1 m default when enabled), as is the 15-degree keyboard rotation step. The public snap settings accept other increments. **Esc**, B, or Cancel preview cancels without charging. The sphere marks the exterior approach. Object selection is suspended while the palette is open.

Placement uses authored oriented rectangles and separating-axis overlap checks, not renderer bounds. All hard, construction, and entrance clearance corners must remain within the owned rectangle. Those reservations cannot overlap another active building/site, the service area, or forbidden zones. Decorative overhang is separate metadata and is not a hard exclusion. Entrance reservations include worker/applicant waiting and approach space. Non-finite transforms, unavailable era/tutorial definitions, disconnected exterior approaches, and insufficient funds reject confirmation. Confirmation revalidates; duplicate instance IDs are rejected before spending.

## Navigation and Stage 1

The adapter transforms existing local entrances, interior stations, door geometry, and interaction children through the instance root. It reuses the lot's existing `NavMeshSurface`, physics-collider collection, agent settings. The initial/site-only bake keeps the existing 0.05 m voxel size; an operational Stage refines the runtime raster to 0.025 m (or retains an already finer setting). A synchronous runtime bake occurs on new-studio setup, site placement, completion, and cancellation. There is no per-frame bake. This foundation favors correctness for a small lot; bounded asynchronous updates are a later optimization.

AI Navigation supports runtime builds through `NavMeshSurface.BuildNavMesh`; the installed package implementation and [Unity's official API](https://docs.unity.cn/Packages/com.unity.ai.navigation%402.0/api/Unity.AI.Navigation.NavMeshSurface.html) were checked. Runtime data is not saved back to the baked project asset. Superseded runtime NavMeshData is destroyed; the original authored data is preserved.

A site has a solid temporary footprint and an exterior work approach. Finished content and its operational colliders remain inactive until completion. Completion replaces the temporary blocker with the finished colliders and rebuilds the continuous surface. No threshold links are created. Stage 1's approved 55 mm door, 8 cm apron, local arrival-order avoidance controller, closed-door carving, employee dimensions, and separation rules are preserved in the reused asset/code. See `Stage1ThresholdStabilization.md` for the prior threshold baseline; the old crowd defect was resolved there.

## Construction work and visual foundation

Lifecycle is **Planned -> UnderConstruction -> Operational**, with explicit cancellation. Phases are SitePreparation, Foundation, Structure, Exterior, Finishing, Complete. Placement pays for materials and creates a zero-capacity work item. No employees and workers still travelling earn zero work. A real hired ConstructionWorker claims the shared person resource, receives an EmployeeAgent destination, travels to an exterior work point, then contributes only after arrival. The existing strategic work scheduler accounts for work and phase completion; no wall-clock completion timer is used.

The default capacity is three workers. Hourly rates are 1, then +2/3, then +1/2 work units, giving diminishing returns. The placeholder costs/work are HQ $18,000/18 units, Casting $12,000/12, Stage $50,000/36. Worker skill balancing is deferred. The driver dispatches available builders, retries unavailable paths at a modest interval, and releases assignments on cancellation/completion or unavailable employees. `MaintenanceTaskKind` distinguishes Construction, Repair, Renovation; only Construction is implemented.

`ConstructionSiteView` supplies shared foundation/scaffold/material-crate placeholders, a site blocker, and phase-dependent scaffold height. `ConstructionPhaseVisuals` defines optional renderer groups for foundation, structure, walls, roof, openings, fixtures, details; `ConstructionDressingKind` reserves scaffold, planks, ladder, fencing, materials, crates, canvas, debris categories. Existing art is not repartitioned. Default finished content remains hidden until complete; modular authoring can bind/render finished-model groups in a future presentation pass. No second bespoke model is required.

## Zero staff, service facility, and applicants

New Studio suppresses configured sandbox employees and hides the prototype HQ, Casting, Stage, ScriptOffice, and StageSchool. It starts with zero employees and a minimal cube service facility with local arrival/wait/exit anchors. Nobody is auto-hired. The service facility advertises ConstructionWorker and Groundskeeper. Groundskeeper is a real profession and hireable employee; cleaning/litter behavior is deferred.

`FacilityApplicantPool` contains only operational facilities, each with a stable instance context and allowed professions. Round-robin selection visits facilities and professions fairly, respecting per-facility capacity. Existing recruitment cadence, waiting expiry, rejection, and conversion to employee remain in use. An applicant carries its facility ID, physically routes to that facility, becomes hireable only while waiting, and eventually leaves if unselected. The Build / Workforce palette groups applicants under their facility; the legacy sandbox recruitment path is retained when no construction session exists.

Completed Casting attracts Actor, Director, Extra. Completed HQ supplies temporary outdoor writing stations and a Writer applicant context to preserve the current screenplay/tutorial loop until a dedicated writers facility exists. The service facility remains available throughout. Research/crew facilities and additional professions are not implemented.

## Completion, costs, and tutorial

Completion activates the final model, disables dressing, rebuilds navigation, refreshes production routing, registers stage capabilities under the placed instance ID, registers applicant facilities, releases workers, raises Operational/Established domain events, and requests a BuildingCompleted PA announcement. No operational stage capability or Casting applicant category is granted on placement.

Placement commits its full cost once through `TryRecordExpense` with the building instance as reference. Preview cancellation costs nothing. Cancelling a committed site refunds nothing (materials are committed), removes the reservation/blocker, and releases workers. There is no demolition of operational facilities in this foundation. New-studio upkeep enumerates operational instances only; normal employee salaries still apply.

Tutorial adds a first-builder objective after Welcome. The normal construction service observes tutorial availability, and `TutorialGameEvents` observes its completed-building events. Continue dismisses a pause message so applicants and construction can progress. Tutorial OFF removes only tutorial gates, retaining cost, ownership, geometry, and navigation validation. No tutorial-only building or construction shortcut exists in the live flow.

## Persistence boundary

Versioned `PlacedBuildingSnapshot` stores instance/definition IDs, pose, lifecycle, phase, completed work in fixed micro-units, and stage number. `MaintenanceAssignmentSnapshot` stores employee/building/task/arrival state. `ApplicantSnapshot` stores person/facility/profession/status/wait duration; person/talent data belongs to the existing person persistence boundary. These contracts contain no Unity object references.

Full restoration is deliberately deferred. A save adapter must persist people, finance ledger, tutorial progress, work/scheduler state, stage number allocation and recruitment cadence/random selection state together; rehydrate buildings without charging; rebuild navigation and facilities; and re-route applicants/workers rather than trusting saved physical arrival. These DTOs are a boundary, not a claim of implemented save/load.

## Validation and limitations

Validation results are recorded below after compilation and the focused proof. No full suite, final art pass, or subjective visual approval is intended. New tests cover instance/local-transform separation, rotated overlap/bounds/forbidden/funds/duplicate charging, no work before arrival, completion-only applicant activation, diminishing capacity/cancellation, facility scoping, and real tutorial completion.

Remaining scope: final UI and art, full modular group binding, sophisticated dispatch/worker skill, save restoration, async/localized nav updates, dedicated writer building, litter from real activities and interrupted disposal intentions, cleaning tasks, deterioration, repairs, renovations, roads/utilities, landscaping, and lot expansion. No random litter system was added.


## Recorded validation — 2026-09-28

- Initial batched project compilation: successful, current Console ground truth 0 errors / 0 warnings. The corrective compilation also finished with 0 current errors / 0 warnings. One corrective project compilation was needed after the physical proof exposed rotated-geometry rasterization. This is an explicit exception to the requested one-compile target, not a repeated test loop.
- `BuildingPlacementAndConstructionTests`: 6/6 passed, once.
- `TutorialAndAnnouncementTests`: 6/6 passed, once.
- `RecruitmentTests`: 14/14 passed, once.
- No complete project suite or repeated passing suite was run.
- Live fresh studio: zero employees; service cube exists; only ConstructionWorker/Groundskeeper pool; no operational stage capabilities. A ConstructionWorker physically arrived/waited, was hired through the real coordinator, and became the same world employee.
- Build Mode API exercised opening/selecting/moving/rotating a preview, rejecting an out-of-lot pose, confirming Casting at X=-12, Z=-8, yaw=27 degrees, and rejecting overlap. This is a functional developer API proof; subjective mouse/UI/visual approval is left to the user.
- Casting created a Planned site with zero work. Builder travelled, entered Working, and only then earned work. Strategic acceleration was applied only after observed arrival. Foundation/Structure/Exterior/Finishing/Complete were recorded; completion enabled Actor/Director/Extra facility professions.
- Stage placed at X=10, Z=12, yaw=35 degrees. The same builder travelled to its exterior work point, earned work, completed all phases, and activated finished content. Stage capabilities were absent while it was under construction.
- The first five-agent Stage traversal at the original 0.05 m voxel size failed at 0.008926 m wall contact (limit 0.005 m), with separation/speed still passing. The failure record is retained. A single-variable runtime experiment at 0.025 m voxel size passed: 5/5 agents crossed the personnel opening and arrived at transformed interior stations; minimum separation 0.7018146 m (limit 0.69 m), maximum wall/door penetration 0, maximum horizontal speed 3.5000868 m/s (limit 3.55). No off-mesh traversal; closed-door carving blocked the route and reopening restored it. This refinement is now the operational-Stage runtime bake policy. Original .35 m agent/build radius, 2 m height, geometry, door thickness, priority controller and acceptance thresholds were unchanged.
- This is one focused arbitrary-transform proof, not an exhaustive sweep of all yaw/position combinations or a rerun of the prior 20-run threshold stress set.
- Saved Studio scene, Stage1_Live prefab and Studio_Revision04 navigation asset SHA-256 hashes match their before-state. No art/material asset edits. Temporary Play settings were discarded by reloading the originally clean saved scene; no scene save.
- Retained proof inputs/results and per-fixture results are under `Logs/BuildingPlacementAndConstruction`; working audit backups are in `Temp/BuildingMilestone`. The tests remain in `Assets/Tests/Editor/BuildingPlacementAndConstructionTests.cs`. No Computer Use, commit, or push.

The finer full-world bake took about 12 seconds in this Editor versus about 2.7 seconds at the original resolution. It runs only at lifecycle changes but can visibly stall. The recommended next milestone is a bounded/local asynchronous navigation update and a small placement authoring review (including more yaw/position cases and the modular renderer bindings), followed by the existing save-system boundary integration.

## Changed code map

New domain: `Buildings/BuildingDefinition.cs`, `Buildings/BuildingConstructionService.cs`, `Recruitment/FacilityApplicantPool.cs`.
New presentation: `Buildings/StudioBuildLot.cs`, `StudioBuildMode.cs`, `StudioConstructionDriver.cs`, `ConstructionSiteView.cs`, `ConstructionPhaseVisuals.cs`.
Extended: employee/professional roles and mapping; construction expense category; Candidate and RecruitmentCoordinator facility context; RecruitmentPresentation routing/composition; StudioBootstrap; SoundStageIdentity clone initialization; StudioEconomyDriver upkeep provider; StudioSelectionController input ownership; tutorial sequence/events/announcements/guidance/window and the compatibility establishment interface. One new six-test fixture, the affected tutorial fixture, and this/earlier tutorial documentation changed. Unity-generated script/folder `.meta` files accompany new sources. Existing asset GUIDs and serialized content were preserved.

