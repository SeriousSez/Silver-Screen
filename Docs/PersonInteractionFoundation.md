# Person Interaction + Direct Manipulation Foundation

Implemented 2026-09-28 against Unity 6000.6.3f1 / URP 17.6.0. Prototype presentation; no final UI art. No scene, prefab, material, input-action asset, package or project-setting changes are required. The repository already contained extensive uncommitted work.

## Architecture audit

- `PersonProfile` is the existing stable identity with birth date, talent, genre experience, traits, appearance and voice IDs. `Candidate` and `Employee` reference it. No second person model was introduced.
- `RecruitmentCoordinator` owns hiring; `CandidateWorldRouter.ConvertToEmployee` keeps the same GameObject and PersonProfile, swaps the presentation component, and registers one employee. Payroll and tutorial hiring events already consume `StudioEmployeeManager.OnEmployeeAdded`.
- `FacilityApplicantPool` attracts applicants from operational facilities. Studio Services attracts construction workers/groundskeepers; Casting Office attracts actors/directors/extras. Origin was previously conflated with the role used by `Hire`. `JobSought` now records original intent, independently of the profession chosen at a hiring spot.
- `EmployeeAgent` owns local navigation, idle wandering, explicit arrival callbacks and performance presentation. `CandidateAgent` owns applicant arrival/departure routing. Neither previously supported literal pickup. The existing selection controller used a short left click; camera rotation used right dragging.
- Stage production and writing use existing employee roles, intents and router callbacks. Production information includes the existing intent text and facility identifier, without inventing movie assignments. Acting/directing ability affects performance; role assignment has no proficiency threshold. Writer availability checks role/activity, not skill.
- Construction dispatch uses employee arrival and shared work/resource services. Groundskeeper employment exists, but a cleaning task system does not. This milestone does not invent cleaning assignments.
- Strategic work, exclusive reservations, genre proficiency, morale and traits already exist. Dedicated stress, boredom, needs, relationships and career-history systems do not; no placeholder values or parallel models were added.
- The existing inspector/recruitment panels and short-click selection remain available. The new replaceable projected cards are separate from domain providers and activities. No automatic pause or `Time.timeScale` mutation occurs.

## Ownership and behavior

`PersonInformationService` queries extensible providers. Applicable cards carry category, urgency, relevance, summary and expanded content, sorted by urgency/relevance. Identity and activity are always compactly anchored to the person. Hover reveals one additional card every 0.9 seconds; right click pins all applicable cards immediately. Escape, empty-world right click, selection of another person, pickup or Build Mode dismisses the pin. Existing morale and work intent determine contextual priority.

`StudioSelectionController` owns the gesture before the camera reads right dragging. A quick left click retains selection without pickup. Holding for the configurable 0.18-second threshold begins a carry session. Releasing that original press does not drop: the person continues following the pointer with held presentation and compatible nearby spots active. A later, separate left mouse-down attempts placement. Invalid attempts (including UI/non-ground) keep carrying; move elsewhere and click again. Escape explicitly cancels and returns to the pickup position. Right click no longer cancels a carry. Keyboard/edge/middle pan and zoom remain available. Existing loss-of-focus, component-disable and Build Mode safety cancellation is retained.

`PersonDragSession` temporarily stops navigation, detaches navigation position/rotation updates and disables person colliders. Drop calls `Warp` and clears the old path immediately. It never routes to the cursor. Placement requires the existing agent type/area mask, studio lot rights, NavMesh within **0.15 metres**, and full capsule clearance. Invalid placement attempts leave the active carry session unchanged. Only explicit cancellation or existing lifecycle safety cleanup returns to the pickup position. This small correction is the only nearest-point search in the drop path.

Employee task anchors and callbacks survive the gesture. On a later update an employee can route back to an outstanding obligation from the new location. Idle wandering instead recenters on the drop position. Shared work accounting marks displaced employees unavailable, retaining assignments and earned work until they return. Applicant routing callbacks are deferred while held. Hiring disables the candidate component before converting it, preventing an old arrival/departure callback from running against the new employee.

`PersonInteractionSpot` provides an instance-specific semantic ID, facility ID, authored transform/facing, activity, optional genre, label, reveal distance and optional animation anchor. Each authored anchor has capacity one; multiple slots can be authored separately. The registry contains no visible geometry. Labels appear only for a compatible held person within 12 metres. Hiring anchors are authored relative to each facility's entrance during its existing applicant registration; future architecture variants can author their own transforms.

Hiring checks the **target** operational facility's available professions and the candidate's waiting state. Job sought, source facility and proficiency never gate entry. Salary remains the existing candidate expectation. The one-argument Hire API keeps the existing panel behavior. No career history UI or general employed-person career-change workflow is added.

`PersonPracticeService.TryStart` is a normal domain action, callable by future autonomy. It acquires the existing person and spot reservations, creates a 30-strategic-minute work package, exposes progress, awards one point of existing comedy genre experience on completion, and releases reservations. Actors and extras with no conflicting activity may practice. Picking up a practicing person or assigning them another obligation cancels that session without awarding unearned proficiency. One clearly named developer anchor appears near Studio Services, at ServicePosition + (7, 0, -5); it is not a final set.

## Focused developer proof

Use the existing Studio scene in Play Mode. Subjective presentation/interaction approval belongs to the user.

1. Hover an employee or waiting applicant: name, profession/employment and activity appear immediately; contextual cards reveal progressively. Low morale prioritizes wellbeing; a work obligation prioritizes work. Move away to collapse hover cards.
2. Right click that person: all applicable cards appear immediately and remain when the pointer leaves. Escape dismisses them. Right dragging begun on the person does not rotate the camera; right dragging empty ground still does.
3. Hold left mouse for 0.18 seconds and move: the person lifts and follows. Release the original button and move with no button pressed: they stay carried. Click clear valid ground with a new press to place them immediately. Click solid geometry/outside the lot: they remain carried with held visuals and nearby spots available. Move elsewhere and retry, or press Escape to return to the pickup position. Camera pan/zoom and strategic time continue.
4. For the cross-facility proof, use `StudioBootstrap`'s existing **Start New Studio** option before Play. Hire the starter construction workforce and complete Casting Office through the existing construction flow. Wait for another Studio Services applicant to finish arriving. Inspect their sought profession, traits and abilities.
5. Hold that service applicant, pan to Casting Office and approach its entrance. Actor, Director and Extra labels appear regardless of proficiency. Release the pickup press, then separately click **Hire as Actor**. The same person/body is registered once as Actor; their name, identity, traits and talent remain. Their expected salary enters the existing payroll. Reverse this with a Casting Office applicant at a Services Groundskeeper/Construction Worker spot.
6. Hold an available actor/extra and move near the developer Practice Comedy anchor. Release the pickup press, then separately click its label: immediate authored placement/facing, Practicing activity and progress. Complete 30 strategic minutes to gain one comedy proficiency point. Pickup interrupts practice and releases its slot.
7. Pick up an employee who is reporting to Stage 1 or already has a work anchor. Drop them elsewhere on the lot. Confirm immediate relocation followed by autonomous travel back to their obligation; assignments and identity remain intact. Their assigned strategic work cannot earn while displaced. No construction/groundskeeper assignment spots are fabricated where a separate direct-assignment action does not yet exist.

The legacy prototype retains its Stage School applicants and also receives casting hiring labels at existing Casting Office entrances. New-studio facility registration is the authoritative service-to-casting proof path.

## Validation

Run **SilverScreen > Person Interaction > Run focused validation** only after saving the current scene. It selects the new person tests, existing RecruitmentTests, two affected strategic work tests and two affected building/applicant tests. The one navigation test temporarily uses an empty Play Mode scene and restores the previous saved scene; it does not save over it. It refuses an unsaved active scene.

Initial Unity compile passed with `EditorUtility.scriptCompilationFailed == false`; Console baseline had no errors. Initial focused run: 24 passed; the navigation test failed at EditMode NavMeshAgent setup before any drop behavior. That test was moved to isolated Play Mode and passed on its targeted rerun (1/1), including immediate placement, no drop path, obstacle rejection, and obligation resumption. Studio was restored in Edit Mode, clean. Passing tests were not rerun. The additional practice/obligation conflict test passed (1/1). **26 checks passed in total** across the focused runs. Final compilation reported no script errors, and the Console reported no errors. See `PersonInteractionValidationResults.txt` for the exact test list.

No complete project-wide test suite, Computer Use, commit, push, final visual approval or player build was performed.

## Carry input lifecycle correction — 2026-09-28

`Normal -> PendingPickup -> Carrying` uses the existing threshold. The original mouse-up only arms subsequent placement clicks. Carry, held sway and compatible nearby spot visibility persist without a pressed button. A fresh left mouse-down attempts ground/context placement; only success ends the session. Invalid attempts preserve it. Escape explicitly cancels to the original pickup transform. Drop presses are consumed by the person controller so they cannot also select buildings or begin another pickup.

Focused correction validation: **2/2 tests passed on the first run**, zero failures. Compiled once with no script errors; no unrelated tests or repeat runs. The new synthetic-input scenario drives the real controller through quick click, threshold pickup, initial release, button-free carry, invalid retry, ground click, contextual Practice Comedy click/activity, and Escape. The existing manipulation test covers placement/obligation and presentation invariants. No held-pose implementation was redesigned.

Exact focused tests: `PersonManipulationRuntimeTests.CarryRequiresSeparateClickAndInvalidAttemptsKeepPresentation` and `PersonManipulationRuntimeTests.GroundDropWarpsImmediatelyRejectsWallsAndRetainsWorkObligation`. The synthetic-input fixture restores input devices/settings and the saved Studio scene after its isolated Play Mode proof.
