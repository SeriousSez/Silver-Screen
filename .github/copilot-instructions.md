SilverScreen — Copilot Instructions

Project

SilverScreen is a 1930s Hollywood movie-studio management and life-simulation game built in Unity.

The game combines studio/tycoon management with persistent simulated people whose careers, personalities, relationships, needs and lives continue beyond individual movie productions.

Core philosophy:

The core activity gives people a reason to exist; their lives make the core activity matter.

People are persistent simulated individuals, not disposable workforce resources.

General Engineering Rules

Before implementing a task:

Inspect the existing architecture and relevant systems.
Reuse existing generic systems where possible.
Do not create parallel implementations when an existing system can be extended.
Preserve existing domain boundaries.
Prefer domain/simulation state over transient presentation-only state.
Keep gameplay logic independent from specific scenes and visual assets where practical.
Prefer configurable/data-driven values over scattered constants.
Use the authoritative SilverScreen simulation clock for simulation behavior.
Do not introduce additional Time.timeScale owners.
Do not weaken tests merely to make them pass.
Investigate failures and distinguish implementation defects from obsolete test assumptions.
Do not run the entire project test suite unless the task requires it. Prefer focused tests.
Do not commit or push unless explicitly requested.

Never silently redesign a system while completing a bounded milestone.

If existing uncommitted work is present, inspect and understand it before modifying or replacing it.

Scope Discipline

Implement only the requested milestone.

Do not opportunistically implement future systems merely because supporting architecture exists.

If future functionality needs preparation, create clean extension points/metadata rather than implementing the future feature.

At completion:

report what changed;
report tests run and exact results;
report known limitations;
provide exact manual checks;
stop.

People

Persistent Identity

A Person is a persistent individual.

Do not treat applicants, employees, actors, directors, crew, writers or other professions as disposable independent character records when the existing Person architecture can represent the same individual.

Changing employment/profession must preserve the person's identity.

A person's long-term identity may eventually include:

appearance;
personality;
relationships;
family;
career history;
skills/proficiencies;
fame/reputation;
preferences;
needs/wellbeing;
addictions;
life history.

Systems should remain compatible with that direction.

Profession Freedom

Do not restrict people to predefined eligible professions unless a specific gameplay rule explicitly requires it.

When hiring through contextual profession regions, the PLAYER chooses the profession.

For example, a Stage School talent applicant may be hired as:

Actor
Director
Extra

regardless of what profession they might currently be best at.

Their underlying proficiencies affect how good that decision is; they do not normally prohibit it.

Existing employees may eventually be reassigned similarly.

Do not rewrite unrelated proficiencies when profession changes.

Employee Counts

There are NO artificial employee/profession caps.

Do not implement limits such as:

maximum Actors;
maximum Directors;
maximum Extras;
maximum Writers;
maximum Crew;
maximum Builders.

The player may over-hire.

Consequences will emerge from simulation, including:

wages;
financial pressure;
insufficient work;
boredom;
mood;
career ambitions;
dissatisfaction;
stress/overwork;
retention/resignation.

Recruitment capacity refers to applicant availability/waiting capacity, NOT maximum employment.

Recruitment

Applicants are real persistent people.

Recruitment should generally be physical:

world arrival
→ navigate to recruitment building
→ reserve exterior waiting position
→ wait visibly
→ player interacts physically
→ hire/reject/etc.

Avoid turning recruitment into a purely menu-based system.

Recruitment buildings use authored exterior applicant positions.

Do not stack applicants at the same position.

Starter recruitment is category-level/studio-level, not building-instance-level.

Current intended starter intakes:

Studio Services: 6
Stage School / Talent: 4
Crew Facility: 4
Writing Office / Writers: 3

Starter intake should not be repeatedly granted by rebuilding, re-enabling or constructing another facility of the same recruitment category.

Person Controls

Normal left click:
normal person selection/interaction.

Left-click and HOLD:
after the established pickup threshold, pick the person up.

After pickup:
the mouse button may be released;
the person remains carried/follows the cursor.

Later separate left click:
drop/place the carried person.

Do not replace this with continuous click-and-drag.

When carrying a person, relevant contextual building regions/spots may become visible.

Contextual Interactions

Distinguish:

Large contextual regions

Used for broad workforce/player decisions, such as:

ACTOR
DIRECTOR
EXTRA
profession assignment
future dismissal/create/import actions.

Precise activity spots

Used for activities performed at specific physical locations, such as:

SCREEN TEST
INTERVIEW
workstations
equipment interactions.

Do not permanently paint contextual labels into building art unless explicitly required.

They should normally appear only when contextually relevant.

Simulation Philosophy

Avoid arbitrary restrictions when simulation consequences can produce the desired behavior.

Examples:

Do not prevent over-hiring.
Let finances, boredom, career satisfaction and retention make over-hiring costly.

Do not make idle people randomly wander merely to appear alive.
Idle behavior should eventually be purposeful.

People should make autonomous choices based on combinations of:

personality;
needs;
skills;
relationships;
schedule;
career goals;
available facilities;
opportunities;
current obligations.

Wellbeing

Keep these concepts distinct rather than collapsing them into one happiness value:

stress;
boredom;
fatigue/energy;
mood;
career satisfaction.

Possible states must include combinations such as:

relaxed but bored;
busy but stressed;
exhausted but happy;
energetic but unhappy.

Work can reduce boredom while simultaneously increasing stress/fatigue.

Too little meaningful work may produce boredom and career frustration.

Healthy workload may improve career satisfaction and skills.

Excessive sustained workload may produce stress and fatigue.

Exact behavior should vary by person rather than using identical thresholds for everyone.

Stress and Coping

At sufficient stress, people may prioritize stress-relief activities over obligations.

Potential behavior can eventually include:

rest;
exercise;
eating;
socializing;
leaving the studio;
therapy;
rehabilitation;
leave.

Personality, preferences, needs, relationships, professionalism, addictions and obligation urgency may influence these choices.

Do not implement binary/punitive mental-health mechanics.

Careers

Career is part of a person's life, not their entire identity.

Career satisfaction should remain conceptually separate from general mood.

People may eventually have goals such as:

get first role;
receive more work;
become a lead;
become a Director;
work in preferred genres;
improve a skill;
become famous;
earn more;
reduce workload.

Persistent dissatisfaction may eventually affect employee retention.

Do not implement immediate resignation simply because one value crosses a threshold unless explicitly required by the milestone.

Relationships and Family

People may form relationships with other people.

Romantic partners do NOT need to be studio employees.

Employees/actors may eventually have children with employees or non-employees.

The world/person model must not assume every meaningful relationship exists inside the studio workforce.

Child performers are planned.

Career architecture should remain compatible with age-appropriate child performers before adulthood.

Movement

Movement should be purposeful.

Characters may walk or run.

Urgent production travel generally favors running when vehicles are not used.

Normal/leisure movement generally favors walking.

The player may manually move/carry a person to a destination in advance.

Tall characters should eventually adapt posture to low clearances rather than requiring every doorway to fit the tallest possible character.

Buildings

Design gameplay purpose before architectural complexity.

Every major gameplay room should answer:

What does the player or simulated person actually do here?

Do not add rooms solely because a real building would contain them if they have no gameplay/readability value.

Keep circulation readable.

Avoid unnecessary corridors and service spaces.

Simple architecture is preferred when complexity provides no gameplay benefit.

Building Art / Assets

Do not permanently delete reusable modular production assets merely because a particular building no longer uses them.

Unused by one building does not mean obsolete.

Preserve reusable:

doors;
windows;
furniture;
equipment;
props;
architectural modules;
source geometry;
Unity prefabs/assets

unless explicitly instructed otherwise.

Do not destructively overwrite approved/frozen art sources while creating new candidates.

Construction

Building placement creates a construction site rather than instantly creating the finished building when using the production construction pipeline.

Construction uses the existing shared systems for:

work/progress;
worker proficiency;
diminishing team output;
reservations;
phases;
dressing;
completion;
asynchronous navigation update.

Do not create building-specific construction systems unless genuinely required.

Building Reveal / Cutaway

Use the generic building reveal/cutaway system.

Do not create building-specific reveal systems unnecessarily.

Cutaway behavior must respect:

camera-relative/building-local direction;
arbitrary building yaw;
semantic wall/roof ownership;
attached fixtures disappearing with their supporting architecture;
independent furniture remaining independent.

Doors and Navigation

Reuse generic automatic door traversal.

Do not create profession/building-specific door navigation unless required.

Validate important routes in both directions.

Avoid using high-detail render geometry as navigation/collision geometry.

Prefer simplified collision proxies.

UI and Time

Major/modal interfaces should pause strategic simulation when appropriate.

Examples include:

pause menu;
Star Maker;
Movie Maker;
awards;
history/archive;
similar full-screen/modal interfaces.

Person UI should generally remain non-pausing.

Use the centralized pause policy rather than directly manipulating Time.timeScale from individual screens.

Character / Wardrobe Direction

The future Star Maker and runtime character system must use the SAME persistent Person and visual character representation.

Do not build a disconnected character-creator-only character model.

Garments are body-aware.

Planned garment approach:

authored garment ease/offset;
body morph fitting;
garment expansion/conforming;
corrective fit targets;
runtime interpolation;
hybrid cloth where appropriate.

Tightly fitted areas may be skinned.

Loose elements such as:

skirts;
coat tails;
ties

may use controlled secondary cloth.

Wardrobe Presentation

Changing clothes should feel like a person physically trying clothing on, not like swapping a texture/model instantly.

Planned presentation includes:

removing garments;
tossing/removing items toward off-screen wardrobe space;
reaching off-screen for replacements;
temporary dressing proxies for complex garments;
putting items on;
adjusting them;
inspecting the result;
personality/preference-driven reactions.

Difficult full-body changes may deliberately use theatrical cheats such as:

crouching/jumping out of the character viewport;
walking out and returning changed;
controlled camera framing.

Do not require physically exact cloth dressing simulation.

Sell the illusion using animation, rigged dressing proxies, blend shapes and hidden swaps.

Garment States

Where supported, clothing may expose states such as:

jacket open/buttoned;
shirt collar open/buttoned;
sleeves down/rolled;
shirt tucked/untucked.

Characters may physically animate transitions such as buttoning/unbuttoning.

Garment state should be data, not merely a one-off animation.

Visible Reactions

People should visibly react to player decisions when appropriate.

Examples:

clothing preference;
role assignment;
promotion;
disappointing assignment;
time off;
overwork;
dismissal.

Do not rely exclusively on numeric UI to communicate person state.

Personality/preferences/context should drive reactions rather than choosing them randomly.

Paths / Studio Infrastructure

Future player-built paths should NOT use rectangular road/path tiles.

The intended system is spline/network based and should support:

straight paths;
curves;
editable control points;
intersections/junctions;
path width;
entrance snapping;
surface types;
navigation integration.

Buildings should expose sensible future path-connection anchors rather than embedding unnecessary studio-wide sidewalks.

Future foot traffic may create visible desire paths.

NPCs should not be absolutely forced onto paths; route choice may consider distance versus movement advantage.

Current Vertical Slice

The first complete playable movie loop is:

build studio
→ recruit employees
→ write screenplay
→ assemble production
→ cast/assign crew
→ rehearse/prepare
→ film
→ release
→ box office
→ repeat.

Core buildings currently planned for this loop:

Studio Services
Stage School
Crew Facility
Writing Office
Casting Office
Sound Stage

Avoid expanding the vertical slice with unrelated buildings/features unless explicitly requested.

Current Stage School Rules

Stage School recruits profession-neutral Talent applicants.

Applicants wait outside.

Current physical applicant capacity: 6.

Starter intake: 4.

Direct hiring choices:

Actor
Director
Extra

Screen Test and Interview are future optional evaluation activities, NOT prerequisites for hiring.

Create/Import Talent and Star Maker are future systems.

The player may hire directly without evaluating an applicant.

Repository Safety

Never:

delete approved production assets without explicit instruction;
overwrite frozen masters casually;
discard unrelated working-tree changes;
commit;
push

unless explicitly requested.

When a task starts with existing uncommitted work:

inspect it;
understand ownership/context;
preserve unrelated work;
make only necessary changes.

When uncertain whether a change is destructive or outside scope, stop and report rather than guessing.
