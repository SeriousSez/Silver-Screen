# Studio Services production and fidelity validation

The production assembly was refined after its original detailing pass. Current evidence and the full audit are in [Fidelity/validation.md](../Fidelity/validation.md) and [the visual review](../Fidelity/review.html). Visual approval remains pending.

- Unity 6000.6.3f1: **39/39 focused tests**, zero failed/skipped; final run 2026-09-28 18:22:22–18:22:49 UTC ([tests.xml](tests.xml)).
- **37/37 preflight checks** passed ([checks.json](checks.json)).
- Actual Studio Play Mode passed applicant arrival, carry/cancel, cutaway, both immediate hiring interactions, stable identity, restoration and collision-checked routes through personnel access to workshop/storage ([runtime.txt](runtime.txt)).
- 61 canonical assemblies, 28 shared material families, 175 placements; 744,595 assembled triangles before culling. Original 57 identities retained.
- Approved massing, rooms, openings and 19 anchors retained. No employee-dimension or gameplay-architecture changes in the fidelity refinement.
- Roof/sweep source regressions and export validation passed. Current details and limitations are recorded in the linked report.

`implementation-manifest.json` here describes the **earlier production/integration pass** against its 8,654-file baseline. The later refinement has a separate [manifest](../Fidelity/implementation-manifest.json) against its 9,135-file baseline; use both for milestone ownership. Earlier New Studio scenario references remain untouched by the refinement.

Dynamic doors, water simulation, final LODs, dense-lot performance qualification and standalone player build remain outside this pass. No commit or push.
