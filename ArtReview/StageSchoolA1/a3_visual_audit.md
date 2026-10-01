# A3 final visual audit

All 36 final Unity views were inspected using the game's URP environment. Plan/overview shots hide review assemblies; interior close views retain roofs/ceilings. This is a visual and source/geometric audit, not a guarantee of defect-free surfaces. One corrective art pass followed initial inspection; remaining defects are documented without another tuning pass.

| Area | Capture numbers | Final inspection |
|---|---|---|
| Entrance / Reception | 09, 15–17 | Both door/counter sides and staff station improved. Rail anchor and drainage supports remain defective. |
| Applicant Waiting | 10, 28–31 | Interior window hardware, completed chair fronts/backs and opening-aware panels. Pendant mount remains incorrect. |
| Common / Portfolio | 10, 36 | Central noticeboard and four-portrait gallery clear openings. |
| Contextual Hall | 08, 33, 36 | Route retained; soffit/reveal terminations and ceiling roses incomplete. |
| Talent Preparation / Lounge | 32 | Seating and restroom approach inspected. |
| Audition / Screen Test | 11, 18, 26, 27 | Wider backdrop, foreground curtains and equipment sequence inspected. Pendant mount remains defective. |
| Director Interview | 12 | Clock clear of trim; bookcase clear of window; filing faces room. |
| Flexible Evaluation | 13 | Seating, filing, circulation and full-height partition inspected. |
| Staff Office | 14, 19 | Functional desk/accessory orientation; service-door swing clears desks. |
| Records / Archive | 14, 20 | Rack orientation, grounded boxes and aisle inspected from inside after camera adjustment. |
| Equipment Store | 14, 21 | Cupboard/rack face central aisle; final camera avoids open door. |
| Service Passage | 14, 22 | Door directions and ceiling closures inspected; narrow nominal Staff-door clearance noted. |
| Restrooms | 23–25 | Bowls, cisterns, cubicles, sinks, taps, mirrors/plumbing inspected. Mirror finish, cubicle hardware and cistern/seat remain below standard. |
| Rear/service entrance | 04, 06, 34 | Canopy, approach and utilities inspected. Drainage incomplete. |
| Front elevation | 01, 05, 15 | Cast lettering and facade identity retained; entrance defects recorded. |
| Left elevation | 02, 05 | Window/pipe spacing inspected; projecting gutter supports visible. |
| Right elevation | 03, 06 | Audition massing and window/pipe spacing inspected. |
| Rear elevation | 04, 06, 34 | Wings, service access, utilities and pipes inspected. |
| Complete roof | 07, 35 | Nine masses retained; cap overlaps and collection discontinuity remain. |
| Repeated doors | 16, 26, 27 and rooms | Source covers 15 leaves; representative reverse faces inspected. Cubicle hardware not passed. |
| Repeated windows | 28, 29 and elevations/rooms | Source covers 39 assemblies; representative inside/outside inspected. Clerestory caveat remains. |
| Armchair | 30, 31 and rooms | Upholstery, frame, legs and rear construction inspected. |

Gallery links all evidence files (`a3_NN_name.png`). Validation records: `a3_unity_result.json`, `a3_unity_sanity.json`, `a3_capture_result.json`, `a3_support_capture_result.json`, `a3_source_qa.json`. Reproducible source checks: `ArtSource/StageSchoolA3/verify_source.py`.

A private-method reflection capture attempt was rejected by Unity's namespace restriction; the supported public audit command successfully produced final captures. Final inspection was not blocked.

**Known production-art defects remain; LOD0 should not yet be frozen.**
