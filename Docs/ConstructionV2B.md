# Construction V2B — Workforce & Productivity

## Existing model retained

Construction already used `WorkService` work quantities scheduled against `SimulationClock`. A site created one work item per phase, travel contributed nothing, arrived workers enabled work, and completion activated the existing facility and asynchronous navigation path. V2B extends that model rather than adding a timer or second scheduler.

## Authored requirements

Each `BuildingDefinition` now owns `ConstructionRequirements`: total work, recommended workers, maximum useful workers, and an ordered set of phase work shares and useful capacities.

| Facility | Total work | Recommended | Maximum | Prep | Foundation | Structure | Exterior | Finishing |
| --- | ---: | ---: | ---: | --- | --- | --- | --- | --- |
| Casting Office | 16 | 2 | 3 | 18% / 3 | 22% / 3 | 25% / 3 | 20% / 2 | 15% / 2 |
| Administration | 30 | 3 | 5 | 12% / 4 | 20% / 4 | 30% / 5 | 20% / 3 | 18% / 4 |
| Sound Stage | 72 | 5 | 8 | 12% / 6 | 18% / 7 | 32% / 8 | 23% / 5 | 15% / 6 |

These are initial tuning values. Architecture and structural complexity are authored explicitly; footprint size is not used as an automatic duration formula.

## Productivity

Only arrived, employed Construction Workers contribute. Travel, blocked routing, unreserved positions, other professions, and zero-worker sites produce zero work.

For proficiency `s` on the existing 0–100 per-profession scale:

`proficiency multiplier = 0.65 + 0.007 × clamp(s, 0, 100)`

For workers ordered by proficiency, the team output in construction-work per simulated hour is:

`Σ proficiencyMultiplier(worker i) × (i + 1)^-0.45`

Equal workers at skill 50 therefore produce approximately 1.00, 1.73, 2.34, and 2.88 work/hour for teams of one through four. Capacity prevents unbounded useful crews.

Productive work is attributed proportionally to the active team. Every eight attributed construction-work grants one Construction proficiency point, capped at 100. Fractional experience and each profession's skill are stored separately on the employee, so changing profession and later returning preserves Construction proficiency.

`ConstructionSiteStatus` exposes total work, completed work, phase, active worker count, useful phase capacity, and current relative productivity for future UI.

## Work positions and coordination

Each active phase deterministically generates exactly its useful capacity in local-space work spots around the authoritative footprint perimeter. Points use a distributed perimeter sequence, a phase-specific offset, a ground-level position outside the building footprint, and a facing target on the nearest facade. Existing activity kinds describe ground, foundation, scaffold, wall, and inspection work for future animation integration.

Dispatch validates each point once against the worker's NavMesh agent type, a complete path, and capsule clearance. Each assignment reserves both the employee and a phase-specific `construction-spot` resource. Released slots are reusable; two workers cannot reserve one slot.

Workers retain a spot for the phase. A phase change releases presentation assignments and paths, then dispatches workers to newly generated phase positions. Failed routing, invalid agents, profession changes, dismissal, cancellation, and completion release both reservations and presentation tasks.

For multiple projects, each free Construction Worker considers sites by lowest assigned/capacity ratio, then stable site ID. This fills projects evenly instead of exhausting the first site. Workers beyond all current phase capacities remain available to normal autonomy or another site.

## Completion and performance

The existing strategic clock remains authoritative for pause and 1×/2×/3× progression. Completion still fires once through the existing work event, releases workers and spots, activates the facility, registers capabilities/applicants, and requests the existing asynchronous NavMesh update.

Work spots are generated only when a site or phase presentation is created/refreshed. Dispatch performs bounded checks per available worker and site. No per-frame mesh generation, full-scene physics scan, synchronous completion rebuild, or new time-scale owner was added.

## Validation

Unity compiled the runtime and Editor assemblies. Ten focused EditMode tests passed, covering authored requirements, phase weights/capacities, proficiency mapping, diminishing returns, distinct/reusable reservations, travel-versus-active contribution, status data, Construction experience retention, phase-aware work positions, and existing construction completion/tutorial behavior. No Play Mode sanity check or subjective balancing loop was performed.

The Unity Console still contains the pre-existing `SilverScreen.Editor.NewStudioSceneAuthoring` `ExtensionOfNativeClass` error. No new V2B compiler, test, exception, or assertion error was observed.

## Manual checks

1. Place a Casting Office with no workers and confirm progress remains at zero.
2. Hire one inexperienced Construction Worker; confirm travel contributes nothing, arrival begins work, and the worker uses a clear perimeter point.
3. Add a second and third worker; confirm distinct positions and a meaningful but diminishing speed increase.
4. Place two simultaneous sites; confirm workers distribute between them by relative occupancy and excess workers do not stack.
5. Observe a phase boundary; confirm workers leave their old positions and purposefully travel to new phase positions without rapid shuffling.
6. Compare Casting Office, Administration, and Sound Stage pacing. The Sound Stage should be substantially longer while accepting a larger useful crew.
7. Test Pause, 1×, 2×, and 3×. Pause must stop travel/work and speed changes should scale strategic progression.
8. Reassign an active worker away from Construction, then back later. Confirm their spot and contribution release immediately and their Construction proficiency remains.
9. Dismiss an active worker. Confirm the reservation and contribution disappear and another available worker may take the slot.
10. Complete and cancel sites. Confirm all workers return to autonomy, reservations clear, completion occurs once, and existing operational registration/navigation behavior remains.

The values most likely to need later tuning are total work, phase shares, per-phase capacities, the `-0.45` diminishing exponent, the 0.65–1.35 proficiency range, and eight work per proficiency point.
