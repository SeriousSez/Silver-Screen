# Adult female facial bone classification evidence

Generated from the purchased .blend metadata and deterministic pose probes. Source files remain external and unchanged.

The 95-bone experiment retained the body/root set and only the 42 empirically changed weighted facial joints plus two added eye transforms. It failed blink parity and is not an approved runtime representation.

## Candidate runtime bones

### Body/root (51)

```text
Arm_L
Arm_R
Foot_L
Foot_R
ForeArm_L
ForeArm_R
HandIndex1_L
HandIndex1_R
HandIndex2_L
HandIndex2_R
HandIndex3_L
HandIndex3_R
HandMiddle1_L
HandMiddle1_R
HandMiddle2_L
HandMiddle2_R
HandMiddle3_L
HandMiddle3_R
HandPinky1_L
HandPinky1_R
HandPinky2_L
HandPinky2_R
HandPinky3_L
HandPinky3_R
HandRing1_L
HandRing1_R
HandRing2_L
HandRing2_R
HandRing3_L
HandRing3_R
HandTumb1_L
HandTumb1_R
HandTumb2_L
HandTumb2_R
Hand_L
Hand_R
Head
Hips
Leg_L
Leg_R
Neck
ROOT
Shoulder_L
Shoulder_R
Spine
Spine1
Spine2
Toe_L
Toe_R
UpLeg_L
UpLeg_R
```

### Tested facial/eye set (44)

```text
DEF-brow.B.L
DEF-brow.B.L.001
DEF-brow.B.L.002
DEF-brow.B.L.003
DEF-brow.B.R
DEF-brow.B.R.001
DEF-brow.B.R.002
DEF-brow.B.R.003
DEF-brow.T.L
DEF-brow.T.L.001
DEF-brow.T.L.002
DEF-brow.T.L.003
DEF-brow.T.R
DEF-brow.T.R.001
DEF-brow.T.R.002
DEF-brow.T.R.003
DEF-cheek.B.L.001
DEF-cheek.B.R.001
DEF-cheek.T.L
DEF-cheek.T.R
DEF-forehead.L
DEF-forehead.L.001
DEF-forehead.L.002
DEF-forehead.R
DEF-forehead.R.001
DEF-forehead.R.002
DEF-lid.B.L
DEF-lid.B.L.001
DEF-lid.B.L.002
DEF-lid.B.L.003
DEF-lid.B.R
DEF-lid.B.R.001
DEF-lid.B.R.002
DEF-lid.B.R.003
DEF-lid.T.L
DEF-lid.T.L.001
DEF-lid.T.L.002
DEF-lid.T.L.003
DEF-lid.T.R
DEF-lid.T.R.001
DEF-lid.T.R.002
DEF-lid.T.R.003
SS_Eye_L
SS_Eye_R
```

## Complete 441-bone source classification

Classification is specific to this experiment; omitted facial bones may matter for other expressions. Bone parenting of rigid eyes and teeth is recorded separately.

| Source bone | Parent | Source deform flag | Weighted on audited meshes | Role in candidate |
| --- | --- | --- | --- | --- |
| `SWITCH` | `(root)` | False | False | E: source animator/control infrastructure |
| `ROOT` | `(root)` | False | False | A: body/root |
| `Hips` | `ROOT` | True | True | A: body/root |
| `Spine` | `Hips` | True | True | A: body/root |
| `Spine1` | `Spine` | True | True | A: body/root |
| `Spine2` | `Spine1` | True | True | A: body/root |
| `Neck` | `Spine2` | True | True | A: body/root |
| `Head` | `Neck` | True | True | A: body/root |
| `ORG-face` | `Head` | False | False | D: constraint/mechanism infrastructure |
| `ORG-lip.T.L` | `ORG-face` | False | False | D: constraint/mechanism infrastructure |
| `ORG-lip.B.L` | `ORG-face` | False | False | D: constraint/mechanism infrastructure |
| `ORG-ear.L` | `ORG-face` | False | False | D: constraint/mechanism infrastructure |
| `ORG-ear.L.001` | `ORG-face` | False | False | D: constraint/mechanism infrastructure |
| `ORG-ear.R` | `ORG-face` | False | False | D: constraint/mechanism infrastructure |
| `ORG-ear.R.001` | `ORG-face` | False | False | D: constraint/mechanism infrastructure |
| `ORG-lip.T.R` | `ORG-face` | False | False | D: constraint/mechanism infrastructure |
| `ORG-lip.B.R` | `ORG-face` | False | False | D: constraint/mechanism infrastructure |
| `ORG-forehead.L` | `ORG-face` | False | False | D: constraint/mechanism infrastructure |
| `ORG-forehead.L.001` | `ORG-face` | False | False | D: constraint/mechanism infrastructure |
| `ORG-forehead.L.002` | `ORG-face` | False | False | D: constraint/mechanism infrastructure |
| `ORG-temple.L` | `ORG-face` | False | False | D: constraint/mechanism infrastructure |
| `ORG-cheek.B.L` | `ORG-face` | False | False | D: constraint/mechanism infrastructure |
| `ORG-forehead.R` | `ORG-face` | False | False | D: constraint/mechanism infrastructure |
| `ORG-forehead.R.001` | `ORG-face` | False | False | D: constraint/mechanism infrastructure |
| `ORG-forehead.R.002` | `ORG-face` | False | False | D: constraint/mechanism infrastructure |
| `ORG-temple.R` | `ORG-face` | False | False | D: constraint/mechanism infrastructure |
| `ORG-cheek.B.R` | `ORG-face` | False | False | D: constraint/mechanism infrastructure |
| `ORG-cheek.T.L` | `ORG-face` | False | False | D: constraint/mechanism infrastructure |
| `ORG-cheek.T.R` | `ORG-face` | False | False | D: constraint/mechanism infrastructure |
| `ORG-teeth.T` | `ORG-face` | False | False | D: constraint/mechanism infrastructure |
| `ORG-teeth.B` | `ORG-face` | False | False | D: constraint/mechanism infrastructure |
| `DEF-forehead.L` | `ORG-face` | True | True | B: tested facial deformation |
| `DEF-forehead.R` | `ORG-face` | True | True | B: tested facial deformation |
| `DEF-forehead.L.001` | `ORG-face` | True | True | B: tested facial deformation |
| `DEF-forehead.R.001` | `ORG-face` | True | True | B: tested facial deformation |
| `DEF-forehead.L.002` | `ORG-face` | True | True | B: tested facial deformation |
| `DEF-forehead.R.002` | `ORG-face` | True | True | B: tested facial deformation |
| `DEF-temple.L` | `ORG-face` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `DEF-temple.R` | `ORG-face` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `master_eye.L` | `ORG-face` | False | False | E: source animator/control infrastructure |
| `brow.B.L` | `master_eye.L` | False | False | E: source animator/control infrastructure |
| `ORG-brow.B.L` | `brow.B.L` | False | False | D: constraint/mechanism infrastructure |
| `DEF-brow.B.L` | `brow.B.L` | True | True | B: tested facial deformation |
| `brow.B.L.001` | `master_eye.L` | False | False | E: source animator/control infrastructure |
| `ORG-brow.B.L.001` | `brow.B.L.001` | False | False | D: constraint/mechanism infrastructure |
| `DEF-brow.B.L.001` | `brow.B.L.001` | True | True | B: tested facial deformation |
| `brow.B.L.002` | `master_eye.L` | False | False | E: source animator/control infrastructure |
| `ORG-brow.B.L.002` | `brow.B.L.002` | False | False | D: constraint/mechanism infrastructure |
| `DEF-brow.B.L.002` | `brow.B.L.002` | True | True | B: tested facial deformation |
| `brow.B.L.003` | `master_eye.L` | False | False | E: source animator/control infrastructure |
| `ORG-brow.B.L.003` | `brow.B.L.003` | False | False | D: constraint/mechanism infrastructure |
| `DEF-brow.B.L.003` | `brow.B.L.003` | True | True | B: tested facial deformation |
| `brow.B.L.004` | `master_eye.L` | False | False | E: source animator/control infrastructure |
| `lid.B.L` | `master_eye.L` | False | False | E: source animator/control infrastructure |
| `ORG-lid.B.L` | `lid.B.L` | False | False | D: constraint/mechanism infrastructure |
| `lid.B.L.001` | `master_eye.L` | False | False | E: source animator/control infrastructure |
| `ORG-lid.B.L.001` | `lid.B.L.001` | False | False | D: constraint/mechanism infrastructure |
| `lid.B.L.002` | `master_eye.L` | False | False | E: source animator/control infrastructure |
| `ORG-lid.B.L.002` | `lid.B.L.002` | False | False | D: constraint/mechanism infrastructure |
| `lid.B.L.003` | `master_eye.L` | False | False | E: source animator/control infrastructure |
| `ORG-lid.B.L.003` | `lid.B.L.003` | False | False | D: constraint/mechanism infrastructure |
| `lid.T.L` | `master_eye.L` | False | False | E: source animator/control infrastructure |
| `ORG-lid.T.L` | `lid.T.L` | False | False | D: constraint/mechanism infrastructure |
| `lid.T.L.001` | `master_eye.L` | False | False | E: source animator/control infrastructure |
| `ORG-lid.T.L.001` | `lid.T.L.001` | False | False | D: constraint/mechanism infrastructure |
| `lid.T.L.002` | `master_eye.L` | False | False | E: source animator/control infrastructure |
| `ORG-lid.T.L.002` | `lid.T.L.002` | False | False | D: constraint/mechanism infrastructure |
| `lid.T.L.003` | `master_eye.L` | False | False | E: source animator/control infrastructure |
| `ORG-lid.T.L.003` | `lid.T.L.003` | False | False | D: constraint/mechanism infrastructure |
| `MCH-eye.L` | `master_eye.L` | False | False | C/D: eye orientation mechanism; replace with direct eye transform |
| `ORG-eye.L` | `MCH-eye.L` | False | False | C/D: eye orientation mechanism; replace with direct eye transform |
| `MCH-eye.L.001` | `master_eye.L` | False | False | C/D: eye orientation mechanism; replace with direct eye transform |
| `MCH-lid.B.L` | `master_eye.L` | False | False | D: constraint/mechanism infrastructure |
| `DEF-lid.B.L` | `MCH-lid.B.L` | True | True | B: tested facial deformation |
| `MCH-lid.B.L.001` | `master_eye.L` | False | False | D: constraint/mechanism infrastructure |
| `DEF-lid.B.L.001` | `MCH-lid.B.L.001` | True | True | B: tested facial deformation |
| `MCH-lid.B.L.002` | `master_eye.L` | False | False | D: constraint/mechanism infrastructure |
| `DEF-lid.B.L.002` | `MCH-lid.B.L.002` | True | True | B: tested facial deformation |
| `MCH-lid.B.L.003` | `master_eye.L` | False | False | D: constraint/mechanism infrastructure |
| `DEF-lid.B.L.003` | `MCH-lid.B.L.003` | True | True | B: tested facial deformation |
| `MCH-lid.T.L` | `master_eye.L` | False | False | D: constraint/mechanism infrastructure |
| `DEF-lid.T.L` | `MCH-lid.T.L` | True | True | B: tested facial deformation |
| `MCH-lid.T.L.001` | `master_eye.L` | False | False | D: constraint/mechanism infrastructure |
| `DEF-lid.T.L.001` | `MCH-lid.T.L.001` | True | True | B: tested facial deformation |
| `MCH-lid.T.L.002` | `master_eye.L` | False | False | D: constraint/mechanism infrastructure |
| `DEF-lid.T.L.002` | `MCH-lid.T.L.002` | True | True | B: tested facial deformation |
| `MCH-lid.T.L.003` | `master_eye.L` | False | False | D: constraint/mechanism infrastructure |
| `DEF-lid.T.L.003` | `MCH-lid.T.L.003` | True | True | B: tested facial deformation |
| `master_eye.R` | `ORG-face` | False | False | E: source animator/control infrastructure |
| `brow.B.R` | `master_eye.R` | False | False | E: source animator/control infrastructure |
| `ORG-brow.B.R` | `brow.B.R` | False | False | D: constraint/mechanism infrastructure |
| `DEF-brow.B.R` | `brow.B.R` | True | True | B: tested facial deformation |
| `brow.B.R.001` | `master_eye.R` | False | False | E: source animator/control infrastructure |
| `ORG-brow.B.R.001` | `brow.B.R.001` | False | False | D: constraint/mechanism infrastructure |
| `DEF-brow.B.R.001` | `brow.B.R.001` | True | True | B: tested facial deformation |
| `brow.B.R.002` | `master_eye.R` | False | False | E: source animator/control infrastructure |
| `ORG-brow.B.R.002` | `brow.B.R.002` | False | False | D: constraint/mechanism infrastructure |
| `DEF-brow.B.R.002` | `brow.B.R.002` | True | True | B: tested facial deformation |
| `brow.B.R.003` | `master_eye.R` | False | False | E: source animator/control infrastructure |
| `ORG-brow.B.R.003` | `brow.B.R.003` | False | False | D: constraint/mechanism infrastructure |
| `DEF-brow.B.R.003` | `brow.B.R.003` | True | True | B: tested facial deformation |
| `brow.B.R.004` | `master_eye.R` | False | False | E: source animator/control infrastructure |
| `lid.B.R` | `master_eye.R` | False | False | E: source animator/control infrastructure |
| `ORG-lid.B.R` | `lid.B.R` | False | False | D: constraint/mechanism infrastructure |
| `lid.B.R.001` | `master_eye.R` | False | False | E: source animator/control infrastructure |
| `ORG-lid.B.R.001` | `lid.B.R.001` | False | False | D: constraint/mechanism infrastructure |
| `lid.B.R.002` | `master_eye.R` | False | False | E: source animator/control infrastructure |
| `ORG-lid.B.R.002` | `lid.B.R.002` | False | False | D: constraint/mechanism infrastructure |
| `lid.B.R.003` | `master_eye.R` | False | False | E: source animator/control infrastructure |
| `ORG-lid.B.R.003` | `lid.B.R.003` | False | False | D: constraint/mechanism infrastructure |
| `lid.T.R` | `master_eye.R` | False | False | E: source animator/control infrastructure |
| `ORG-lid.T.R` | `lid.T.R` | False | False | D: constraint/mechanism infrastructure |
| `lid.T.R.001` | `master_eye.R` | False | False | E: source animator/control infrastructure |
| `ORG-lid.T.R.001` | `lid.T.R.001` | False | False | D: constraint/mechanism infrastructure |
| `lid.T.R.002` | `master_eye.R` | False | False | E: source animator/control infrastructure |
| `ORG-lid.T.R.002` | `lid.T.R.002` | False | False | D: constraint/mechanism infrastructure |
| `lid.T.R.003` | `master_eye.R` | False | False | E: source animator/control infrastructure |
| `ORG-lid.T.R.003` | `lid.T.R.003` | False | False | D: constraint/mechanism infrastructure |
| `MCH-eye.R` | `master_eye.R` | False | False | C/D: eye orientation mechanism; replace with direct eye transform |
| `ORG-eye.R` | `MCH-eye.R` | False | False | C/D: eye orientation mechanism; replace with direct eye transform |
| `MCH-eye.R.001` | `master_eye.R` | False | False | C/D: eye orientation mechanism; replace with direct eye transform |
| `MCH-lid.B.R` | `master_eye.R` | False | False | D: constraint/mechanism infrastructure |
| `DEF-lid.B.R` | `MCH-lid.B.R` | True | True | B: tested facial deformation |
| `MCH-lid.B.R.001` | `master_eye.R` | False | False | D: constraint/mechanism infrastructure |
| `DEF-lid.B.R.001` | `MCH-lid.B.R.001` | True | True | B: tested facial deformation |
| `MCH-lid.B.R.002` | `master_eye.R` | False | False | D: constraint/mechanism infrastructure |
| `DEF-lid.B.R.002` | `MCH-lid.B.R.002` | True | True | B: tested facial deformation |
| `MCH-lid.B.R.003` | `master_eye.R` | False | False | D: constraint/mechanism infrastructure |
| `DEF-lid.B.R.003` | `MCH-lid.B.R.003` | True | True | B: tested facial deformation |
| `MCH-lid.T.R` | `master_eye.R` | False | False | D: constraint/mechanism infrastructure |
| `DEF-lid.T.R` | `MCH-lid.T.R` | True | True | B: tested facial deformation |
| `MCH-lid.T.R.001` | `master_eye.R` | False | False | D: constraint/mechanism infrastructure |
| `DEF-lid.T.R.001` | `MCH-lid.T.R.001` | True | True | B: tested facial deformation |
| `MCH-lid.T.R.002` | `master_eye.R` | False | False | D: constraint/mechanism infrastructure |
| `DEF-lid.T.R.002` | `MCH-lid.T.R.002` | True | True | B: tested facial deformation |
| `MCH-lid.T.R.003` | `master_eye.R` | False | False | D: constraint/mechanism infrastructure |
| `DEF-lid.T.R.003` | `MCH-lid.T.R.003` | True | True | B: tested facial deformation |
| `ear.L` | `ORG-face` | False | False | E: source animator/control infrastructure |
| `DEF-ear.L` | `ear.L` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `DEF-ear.L.001` | `ear.L` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `ear.L.002` | `ear.L` | False | False | E: source animator/control infrastructure |
| `ORG-ear.L.002` | `ear.L.002` | False | False | D: constraint/mechanism infrastructure |
| `DEF-ear.L.002` | `ear.L.002` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `ear.L.003` | `ear.L` | False | False | E: source animator/control infrastructure |
| `ORG-ear.L.003` | `ear.L.003` | False | False | D: constraint/mechanism infrastructure |
| `DEF-ear.L.003` | `ear.L.003` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `ear.L.004` | `ear.L` | False | False | E: source animator/control infrastructure |
| `ORG-ear.L.004` | `ear.L.004` | False | False | D: constraint/mechanism infrastructure |
| `DEF-ear.L.004` | `ear.L.004` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `ear.R` | `ORG-face` | False | False | E: source animator/control infrastructure |
| `DEF-ear.R` | `ear.R` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `DEF-ear.R.001` | `ear.R` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `ear.R.002` | `ear.R` | False | False | E: source animator/control infrastructure |
| `ORG-ear.R.002` | `ear.R.002` | False | False | D: constraint/mechanism infrastructure |
| `DEF-ear.R.002` | `ear.R.002` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `ear.R.003` | `ear.R` | False | False | E: source animator/control infrastructure |
| `ORG-ear.R.003` | `ear.R.003` | False | False | D: constraint/mechanism infrastructure |
| `DEF-ear.R.003` | `ear.R.003` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `ear.R.004` | `ear.R` | False | False | E: source animator/control infrastructure |
| `ORG-ear.R.004` | `ear.R.004` | False | False | D: constraint/mechanism infrastructure |
| `DEF-ear.R.004` | `ear.R.004` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `jaw_master` | `ORG-face` | False | False | E: source animator/control infrastructure |
| `teeth.B` | `jaw_master` | False | False | E: source animator/control infrastructure |
| `tongue_master` | `jaw_master` | False | False | E: source animator/control infrastructure |
| `tongue` | `tongue_master` | False | False | E: source animator/control infrastructure |
| `ORG-tongue` | `tongue` | False | False | D: constraint/mechanism infrastructure |
| `DEF-tongue` | `tongue` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `chin` | `jaw_master` | False | False | E: source animator/control infrastructure |
| `ORG-chin` | `chin` | False | False | D: constraint/mechanism infrastructure |
| `DEF-chin` | `chin` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `chin.001` | `chin` | False | False | E: source animator/control infrastructure |
| `ORG-chin.001` | `chin.001` | False | False | D: constraint/mechanism infrastructure |
| `DEF-chin.001` | `chin.001` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `chin.L` | `jaw_master` | False | False | E: source animator/control infrastructure |
| `ORG-chin.L` | `chin.L` | False | False | D: constraint/mechanism infrastructure |
| `DEF-chin.L` | `chin.L` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `chin.R` | `jaw_master` | False | False | E: source animator/control infrastructure |
| `ORG-chin.R` | `chin.R` | False | False | D: constraint/mechanism infrastructure |
| `DEF-chin.R` | `chin.R` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `jaw` | `jaw_master` | False | False | E: source animator/control infrastructure |
| `ORG-jaw` | `jaw` | False | False | D: constraint/mechanism infrastructure |
| `DEF-jaw` | `jaw` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `jaw.L.001` | `jaw_master` | False | False | E: source animator/control infrastructure |
| `ORG-jaw.L.001` | `jaw.L.001` | False | False | D: constraint/mechanism infrastructure |
| `DEF-jaw.L.001` | `jaw.L.001` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `jaw.R.001` | `jaw_master` | False | False | E: source animator/control infrastructure |
| `ORG-jaw.R.001` | `jaw.R.001` | False | False | D: constraint/mechanism infrastructure |
| `DEF-jaw.R.001` | `jaw.R.001` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `tongue.003` | `jaw_master` | False | False | E: source animator/control infrastructure |
| `MCH-tongue.001` | `jaw_master` | False | False | D: constraint/mechanism infrastructure |
| `tongue.001` | `MCH-tongue.001` | False | False | E: source animator/control infrastructure |
| `ORG-tongue.001` | `tongue.001` | False | False | D: constraint/mechanism infrastructure |
| `DEF-tongue.001` | `tongue.001` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `MCH-tongue.002` | `jaw_master` | False | False | D: constraint/mechanism infrastructure |
| `tongue.002` | `MCH-tongue.002` | False | False | E: source animator/control infrastructure |
| `ORG-tongue.002` | `tongue.002` | False | False | D: constraint/mechanism infrastructure |
| `DEF-tongue.002` | `tongue.002` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `teeth.T` | `ORG-face` | False | False | E: source animator/control infrastructure |
| `brow.T.L` | `ORG-face` | False | False | E: source animator/control infrastructure |
| `ORG-brow.T.L` | `brow.T.L` | False | False | D: constraint/mechanism infrastructure |
| `DEF-cheek.T.L` | `brow.T.L` | True | True | B: tested facial deformation |
| `DEF-brow.T.L` | `brow.T.L` | True | True | B: tested facial deformation |
| `brow.T.L.001` | `ORG-face` | False | False | E: source animator/control infrastructure |
| `ORG-brow.T.L.001` | `brow.T.L.001` | False | False | D: constraint/mechanism infrastructure |
| `DEF-brow.T.L.001` | `brow.T.L.001` | True | True | B: tested facial deformation |
| `brow.T.L.002` | `ORG-face` | False | False | E: source animator/control infrastructure |
| `ORG-brow.T.L.002` | `brow.T.L.002` | False | False | D: constraint/mechanism infrastructure |
| `DEF-brow.T.L.002` | `brow.T.L.002` | True | True | B: tested facial deformation |
| `brow.T.L.003` | `ORG-face` | False | False | E: source animator/control infrastructure |
| `ORG-brow.T.L.003` | `brow.T.L.003` | False | False | D: constraint/mechanism infrastructure |
| `DEF-brow.T.L.003` | `brow.T.L.003` | True | True | B: tested facial deformation |
| `brow.T.R` | `ORG-face` | False | False | E: source animator/control infrastructure |
| `ORG-brow.T.R` | `brow.T.R` | False | False | D: constraint/mechanism infrastructure |
| `DEF-cheek.T.R` | `brow.T.R` | True | True | B: tested facial deformation |
| `DEF-brow.T.R` | `brow.T.R` | True | True | B: tested facial deformation |
| `brow.T.R.001` | `ORG-face` | False | False | E: source animator/control infrastructure |
| `ORG-brow.T.R.001` | `brow.T.R.001` | False | False | D: constraint/mechanism infrastructure |
| `DEF-brow.T.R.001` | `brow.T.R.001` | True | True | B: tested facial deformation |
| `brow.T.R.002` | `ORG-face` | False | False | E: source animator/control infrastructure |
| `ORG-brow.T.R.002` | `brow.T.R.002` | False | False | D: constraint/mechanism infrastructure |
| `DEF-brow.T.R.002` | `brow.T.R.002` | True | True | B: tested facial deformation |
| `brow.T.R.003` | `ORG-face` | False | False | E: source animator/control infrastructure |
| `ORG-brow.T.R.003` | `brow.T.R.003` | False | False | D: constraint/mechanism infrastructure |
| `DEF-brow.T.R.003` | `brow.T.R.003` | True | True | B: tested facial deformation |
| `jaw.L` | `ORG-face` | False | False | E: source animator/control infrastructure |
| `ORG-jaw.L` | `jaw.L` | False | False | D: constraint/mechanism infrastructure |
| `DEF-jaw.L` | `jaw.L` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `jaw.R` | `ORG-face` | False | False | E: source animator/control infrastructure |
| `ORG-jaw.R` | `jaw.R` | False | False | D: constraint/mechanism infrastructure |
| `DEF-jaw.R` | `jaw.R` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `nose` | `ORG-face` | False | False | E: source animator/control infrastructure |
| `ORG-nose` | `nose` | False | False | D: constraint/mechanism infrastructure |
| `DEF-nose` | `nose` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `nose.L` | `ORG-face` | False | False | E: source animator/control infrastructure |
| `ORG-nose.L` | `nose.L` | False | False | D: constraint/mechanism infrastructure |
| `DEF-nose.L` | `nose.L` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `nose.R` | `ORG-face` | False | False | E: source animator/control infrastructure |
| `ORG-nose.R` | `nose.R` | False | False | D: constraint/mechanism infrastructure |
| `DEF-nose.R` | `nose.R` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `MCH-mouth_lock` | `ORG-face` | False | False | D: constraint/mechanism infrastructure |
| `MCH-jaw_master` | `ORG-face` | False | False | D: constraint/mechanism infrastructure |
| `lip.B` | `MCH-jaw_master` | False | False | E: source animator/control infrastructure |
| `DEF-lip.B.L` | `lip.B` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `DEF-lip.B.R` | `lip.B` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `chin.002` | `lip.B` | False | False | E: source animator/control infrastructure |
| `MCH-jaw_master.001` | `ORG-face` | False | False | D: constraint/mechanism infrastructure |
| `lip.B.L.001` | `MCH-jaw_master.001` | False | False | E: source animator/control infrastructure |
| `ORG-lip.B.L.001` | `lip.B.L.001` | False | False | D: constraint/mechanism infrastructure |
| `DEF-lip.B.L.001` | `lip.B.L.001` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `lip.B.R.001` | `MCH-jaw_master.001` | False | False | E: source animator/control infrastructure |
| `ORG-lip.B.R.001` | `lip.B.R.001` | False | False | D: constraint/mechanism infrastructure |
| `DEF-lip.B.R.001` | `lip.B.R.001` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `MCH-jaw_master.002` | `ORG-face` | False | False | D: constraint/mechanism infrastructure |
| `cheek.B.L.001` | `MCH-jaw_master.002` | False | False | E: source animator/control infrastructure |
| `ORG-cheek.B.L.001` | `cheek.B.L.001` | False | False | D: constraint/mechanism infrastructure |
| `DEF-cheek.B.L.001` | `cheek.B.L.001` | True | True | B: tested facial deformation |
| `cheek.B.R.001` | `MCH-jaw_master.002` | False | False | E: source animator/control infrastructure |
| `ORG-cheek.B.R.001` | `cheek.B.R.001` | False | False | D: constraint/mechanism infrastructure |
| `DEF-cheek.B.R.001` | `cheek.B.R.001` | True | True | B: tested facial deformation |
| `lips.L` | `MCH-jaw_master.002` | False | False | E: source animator/control infrastructure |
| `DEF-cheek.B.L` | `lips.L` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `lips.R` | `MCH-jaw_master.002` | False | False | E: source animator/control infrastructure |
| `DEF-cheek.B.R` | `lips.R` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `MCH-jaw_master.003` | `ORG-face` | False | False | D: constraint/mechanism infrastructure |
| `lip.T.L.001` | `MCH-jaw_master.003` | False | False | E: source animator/control infrastructure |
| `ORG-lip.T.L.001` | `lip.T.L.001` | False | False | D: constraint/mechanism infrastructure |
| `DEF-lip.T.L.001` | `lip.T.L.001` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `lip.T.R.001` | `MCH-jaw_master.003` | False | False | E: source animator/control infrastructure |
| `ORG-lip.T.R.001` | `lip.T.R.001` | False | False | D: constraint/mechanism infrastructure |
| `DEF-lip.T.R.001` | `lip.T.R.001` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `lip.T` | `MCH-jaw_master.003` | False | False | E: source animator/control infrastructure |
| `DEF-lip.T.L` | `lip.T` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `DEF-lip.T.R` | `lip.T` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `nose.005` | `lip.T` | False | False | E: source animator/control infrastructure |
| `MCH-jaw_master.004` | `ORG-face` | False | False | D: constraint/mechanism infrastructure |
| `nose_master` | `MCH-jaw_master.004` | False | False | E: source animator/control infrastructure |
| `nose.002` | `nose_master` | False | False | E: source animator/control infrastructure |
| `ORG-nose.002` | `nose.002` | False | False | D: constraint/mechanism infrastructure |
| `DEF-nose.002` | `nose.002` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `nose.001` | `nose.002` | False | False | E: source animator/control infrastructure |
| `ORG-nose.001` | `nose.001` | False | False | D: constraint/mechanism infrastructure |
| `DEF-nose.001` | `nose.001` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `nose.003` | `nose.002` | False | False | E: source animator/control infrastructure |
| `ORG-nose.003` | `nose.003` | False | False | D: constraint/mechanism infrastructure |
| `DEF-nose.003` | `nose.003` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `nose.004` | `nose_master` | False | False | E: source animator/control infrastructure |
| `ORG-nose.004` | `nose.004` | False | False | D: constraint/mechanism infrastructure |
| `DEF-nose.004` | `nose.004` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `nose.L.001` | `nose_master` | False | False | E: source animator/control infrastructure |
| `ORG-nose.L.001` | `nose.L.001` | False | False | D: constraint/mechanism infrastructure |
| `DEF-nose.L.001` | `nose.L.001` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `nose.R.001` | `nose_master` | False | False | E: source animator/control infrastructure |
| `ORG-nose.R.001` | `nose.R.001` | False | False | D: constraint/mechanism infrastructure |
| `DEF-nose.R.001` | `nose.R.001` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `cheek.T.L.001` | `MCH-jaw_master.004` | False | False | E: source animator/control infrastructure |
| `ORG-cheek.T.L.001` | `cheek.T.L.001` | False | False | D: constraint/mechanism infrastructure |
| `DEF-cheek.T.L.001` | `cheek.T.L.001` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `cheek.T.R.001` | `MCH-jaw_master.004` | False | False | E: source animator/control infrastructure |
| `ORG-cheek.T.R.001` | `cheek.T.R.001` | False | False | D: constraint/mechanism infrastructure |
| `DEF-cheek.T.R.001` | `cheek.T.R.001` | True | True | F: other weighted facial deformation; omitted for current upper-face scope |
| `Shoulder_L` | `Spine2` | True | True | A: body/root |
| `Arm_L` | `Shoulder_L` | True | True | A: body/root |
| `ForeArm_L` | `Arm_L` | True | True | A: body/root |
| `Hand_L` | `ForeArm_L` | True | True | A: body/root |
| `HandPinky1_L` | `Hand_L` | True | True | A: body/root |
| `HandPinky2_L` | `HandPinky1_L` | True | True | A: body/root |
| `HandPinky3_L` | `HandPinky2_L` | True | True | A: body/root |
| `HandRing1_L` | `Hand_L` | True | True | A: body/root |
| `HandRing2_L` | `HandRing1_L` | True | True | A: body/root |
| `HandRing3_L` | `HandRing2_L` | True | True | A: body/root |
| `HandMiddle1_L` | `Hand_L` | True | True | A: body/root |
| `HandMiddle2_L` | `HandMiddle1_L` | True | True | A: body/root |
| `HandMiddle3_L` | `HandMiddle2_L` | True | True | A: body/root |
| `HandIndex1_L` | `Hand_L` | True | True | A: body/root |
| `HandIndex2_L` | `HandIndex1_L` | True | True | A: body/root |
| `HandIndex3_L` | `HandIndex2_L` | True | True | A: body/root |
| `HandTumb1_L` | `Hand_L` | True | True | A: body/root |
| `HandTumb2_L` | `HandTumb1_L` | True | True | A: body/root |
| `IK_Arm_L` | `Shoulder_L` | False | False | E: source animator/control infrastructure |
| `IK_ForeArm_L` | `IK_Arm_L` | False | False | E: source animator/control infrastructure |
| `IK_Hand_L` | `IK_ForeArm_L` | False | False | E: source animator/control infrastructure |
| `IK_HandPinky1_L` | `IK_Hand_L` | False | False | E: source animator/control infrastructure |
| `IK_HandPinky2_L` | `IK_HandPinky1_L` | False | False | E: source animator/control infrastructure |
| `IK_HandPinky3_L` | `IK_HandPinky2_L` | False | False | E: source animator/control infrastructure |
| `IK_HandRing1_L` | `IK_Hand_L` | False | False | E: source animator/control infrastructure |
| `IK_HandRing2_L` | `IK_HandRing1_L` | False | False | E: source animator/control infrastructure |
| `IK_HandRing3_L` | `IK_HandRing2_L` | False | False | E: source animator/control infrastructure |
| `IK_HandMiddle1_L` | `IK_Hand_L` | False | False | E: source animator/control infrastructure |
| `IK_HandMiddle2_L` | `IK_HandMiddle1_L` | False | False | E: source animator/control infrastructure |
| `IK_HandMiddle3_L` | `IK_HandMiddle2_L` | False | False | E: source animator/control infrastructure |
| `IK_HandIndex1_L` | `IK_Hand_L` | False | False | E: source animator/control infrastructure |
| `IK_HandIndex2_L` | `IK_HandIndex1_L` | False | False | E: source animator/control infrastructure |
| `IK_HandIndex3_L` | `IK_HandIndex2_L` | False | False | E: source animator/control infrastructure |
| `IK_HandTumb1_L` | `IK_Hand_L` | False | False | E: source animator/control infrastructure |
| `IK_HandTumb2_L` | `IK_HandTumb1_L` | False | False | E: source animator/control infrastructure |
| `FK_Arm_L` | `Shoulder_L` | False | False | E: source animator/control infrastructure |
| `FK_ForeArm_L` | `FK_Arm_L` | False | False | E: source animator/control infrastructure |
| `FK_Hand_L` | `FK_ForeArm_L` | False | False | E: source animator/control infrastructure |
| `FK_HandPinky1_L` | `FK_Hand_L` | False | False | E: source animator/control infrastructure |
| `FK_HandPinky2_L` | `FK_HandPinky1_L` | False | False | E: source animator/control infrastructure |
| `FK_HandPinky3_L` | `FK_HandPinky2_L` | False | False | E: source animator/control infrastructure |
| `FK_HandRing1_L` | `FK_Hand_L` | False | False | E: source animator/control infrastructure |
| `FK_HandRing2_L` | `FK_HandRing1_L` | False | False | E: source animator/control infrastructure |
| `FK_HandRing3_L` | `FK_HandRing2_L` | False | False | E: source animator/control infrastructure |
| `FK_HandMiddle1_L` | `FK_Hand_L` | False | False | E: source animator/control infrastructure |
| `FK_HandMiddle2_L` | `FK_HandMiddle1_L` | False | False | E: source animator/control infrastructure |
| `FK_HandMiddle3_L` | `FK_HandMiddle2_L` | False | False | E: source animator/control infrastructure |
| `FK_HandIndex1_L` | `FK_Hand_L` | False | False | E: source animator/control infrastructure |
| `FK_HandIndex2_L` | `FK_HandIndex1_L` | False | False | E: source animator/control infrastructure |
| `FK_HandIndex3_L` | `FK_HandIndex2_L` | False | False | E: source animator/control infrastructure |
| `FK_HandTumb1_L` | `FK_Hand_L` | False | False | E: source animator/control infrastructure |
| `FK_HandTumb2_L` | `FK_HandTumb1_L` | False | False | E: source animator/control infrastructure |
| `Shoulder_R` | `Spine2` | True | True | A: body/root |
| `Arm_R` | `Shoulder_R` | True | True | A: body/root |
| `ForeArm_R` | `Arm_R` | True | True | A: body/root |
| `Hand_R` | `ForeArm_R` | True | True | A: body/root |
| `HandPinky1_R` | `Hand_R` | True | True | A: body/root |
| `HandPinky2_R` | `HandPinky1_R` | True | True | A: body/root |
| `HandPinky3_R` | `HandPinky2_R` | True | True | A: body/root |
| `HandRing1_R` | `Hand_R` | True | True | A: body/root |
| `HandRing2_R` | `HandRing1_R` | True | True | A: body/root |
| `HandRing3_R` | `HandRing2_R` | True | True | A: body/root |
| `HandMiddle1_R` | `Hand_R` | True | True | A: body/root |
| `HandMiddle2_R` | `HandMiddle1_R` | True | True | A: body/root |
| `HandMiddle3_R` | `HandMiddle2_R` | True | True | A: body/root |
| `HandIndex1_R` | `Hand_R` | True | True | A: body/root |
| `HandIndex2_R` | `HandIndex1_R` | True | True | A: body/root |
| `HandIndex3_R` | `HandIndex2_R` | True | True | A: body/root |
| `HandTumb1_R` | `Hand_R` | True | True | A: body/root |
| `HandTumb2_R` | `HandTumb1_R` | True | True | A: body/root |
| `IK_Arm_R` | `Shoulder_R` | False | False | E: source animator/control infrastructure |
| `IK_ForeArm_R` | `IK_Arm_R` | False | False | E: source animator/control infrastructure |
| `IK_Hand_R` | `IK_ForeArm_R` | False | False | E: source animator/control infrastructure |
| `IK_HandPinky1_R` | `IK_Hand_R` | False | False | E: source animator/control infrastructure |
| `IK_HandPinky2_R` | `IK_HandPinky1_R` | False | False | E: source animator/control infrastructure |
| `IK_HandPinky3_R` | `IK_HandPinky2_R` | False | False | E: source animator/control infrastructure |
| `IK_HandRing1_R` | `IK_Hand_R` | False | False | E: source animator/control infrastructure |
| `IK_HandRing2_R` | `IK_HandRing1_R` | False | False | E: source animator/control infrastructure |
| `IK_HandRing3_R` | `IK_HandRing2_R` | False | False | E: source animator/control infrastructure |
| `IK_HandMiddle1_R` | `IK_Hand_R` | False | False | E: source animator/control infrastructure |
| `IK_HandMiddle2_R` | `IK_HandMiddle1_R` | False | False | E: source animator/control infrastructure |
| `IK_HandMiddle3_R` | `IK_HandMiddle2_R` | False | False | E: source animator/control infrastructure |
| `IK_HandIndex1_R` | `IK_Hand_R` | False | False | E: source animator/control infrastructure |
| `IK_HandIndex2_R` | `IK_HandIndex1_R` | False | False | E: source animator/control infrastructure |
| `IK_HandIndex3_R` | `IK_HandIndex2_R` | False | False | E: source animator/control infrastructure |
| `IK_HandTumb1_R` | `IK_Hand_R` | False | False | E: source animator/control infrastructure |
| `IK_HandTumb2_R` | `IK_HandTumb1_R` | False | False | E: source animator/control infrastructure |
| `FK_Arm_R` | `Shoulder_R` | False | False | E: source animator/control infrastructure |
| `FK_ForeArm_R` | `FK_Arm_R` | False | False | E: source animator/control infrastructure |
| `FK_Hand_R` | `FK_ForeArm_R` | False | False | E: source animator/control infrastructure |
| `FK_HandPinky1_R` | `FK_Hand_R` | False | False | E: source animator/control infrastructure |
| `FK_HandPinky2_R` | `FK_HandPinky1_R` | False | False | E: source animator/control infrastructure |
| `FK_HandPinky3_R` | `FK_HandPinky2_R` | False | False | E: source animator/control infrastructure |
| `FK_HandRing1_R` | `FK_Hand_R` | False | False | E: source animator/control infrastructure |
| `FK_HandRing2_R` | `FK_HandRing1_R` | False | False | E: source animator/control infrastructure |
| `FK_HandRing3_R` | `FK_HandRing2_R` | False | False | E: source animator/control infrastructure |
| `FK_HandMiddle1_R` | `FK_Hand_R` | False | False | E: source animator/control infrastructure |
| `FK_HandMiddle2_R` | `FK_HandMiddle1_R` | False | False | E: source animator/control infrastructure |
| `FK_HandMiddle3_R` | `FK_HandMiddle2_R` | False | False | E: source animator/control infrastructure |
| `FK_HandIndex1_R` | `FK_Hand_R` | False | False | E: source animator/control infrastructure |
| `FK_HandIndex2_R` | `FK_HandIndex1_R` | False | False | E: source animator/control infrastructure |
| `FK_HandIndex3_R` | `FK_HandIndex2_R` | False | False | E: source animator/control infrastructure |
| `FK_HandTumb1_R` | `FK_Hand_R` | False | False | E: source animator/control infrastructure |
| `FK_HandTumb2_R` | `FK_HandTumb1_R` | False | False | E: source animator/control infrastructure |
| `IK_UpLeg_L` | `Hips` | False | False | E: source animator/control infrastructure |
| `IK_Leg_L` | `IK_UpLeg_L` | False | False | E: source animator/control infrastructure |
| `UpLeg_R` | `Hips` | True | True | A: body/root |
| `Leg_R` | `UpLeg_R` | True | True | A: body/root |
| `Foot_R` | `Leg_R` | True | True | A: body/root |
| `Toe_R` | `Foot_R` | True | True | A: body/root |
| `UpLeg_L` | `Hips` | True | True | A: body/root |
| `Leg_L` | `UpLeg_L` | True | True | A: body/root |
| `Foot_L` | `Leg_L` | True | True | A: body/root |
| `Toe_L` | `Foot_L` | True | True | A: body/root |
| `FK_UpLeg_L` | `Hips` | False | False | E: source animator/control infrastructure |
| `FK_Leg_L` | `FK_UpLeg_L` | False | False | E: source animator/control infrastructure |
| `FK_Foot_L` | `FK_Leg_L` | False | False | E: source animator/control infrastructure |
| `FK_Toe_L` | `FK_Foot_L` | False | False | E: source animator/control infrastructure |
| `IK_UpLeg_R` | `Hips` | False | False | E: source animator/control infrastructure |
| `IK_Leg_R` | `IK_UpLeg_R` | False | False | E: source animator/control infrastructure |
| `FK_UpLeg_R` | `Hips` | False | False | E: source animator/control infrastructure |
| `FK_Leg_R` | `FK_UpLeg_R` | False | False | E: source animator/control infrastructure |
| `FK_Foot_R` | `FK_Leg_R` | False | False | E: source animator/control infrastructure |
| `FK_Toe_R` | `FK_Foot_R` | False | False | E: source animator/control infrastructure |
| `IK_Leg_cntrl_L` | `ROOT` | False | False | E: source animator/control infrastructure |
| `IK_Foot_L` | `IK_Leg_cntrl_L` | False | False | E: source animator/control infrastructure |
| `IK_Toe_L` | `IK_Foot_L` | False | False | E: source animator/control infrastructure |
| `Ik_Knee_target_L` | `IK_Leg_cntrl_L` | False | False | E: source animator/control infrastructure |
| `IK_Leg_cntrl_R` | `ROOT` | False | False | E: source animator/control infrastructure |
| `IK_Foot_R` | `IK_Leg_cntrl_R` | False | False | E: source animator/control infrastructure |
| `IK_Toe_R` | `IK_Foot_R` | False | False | E: source animator/control infrastructure |
| `Ik_Knee_target_R` | `IK_Leg_cntrl_R` | False | False | E: source animator/control infrastructure |
| `IK_Arm_controler_L` | `ROOT` | False | False | E: source animator/control infrastructure |
| `IK_elbow_target_L` | `IK_Arm_controler_L` | False | False | E: source animator/control infrastructure |
| `IK_Arm_controler_R` | `ROOT` | False | False | E: source animator/control infrastructure |
| `IK_elbow_target_R` | `IK_Arm_controler_R` | False | False | E: source animator/control infrastructure |
| `SWITCH_ARMS` | `(root)` | False | False | E: source animator/control infrastructure |
| `MCH-eyes_parent` | `(root)` | False | False | D: constraint/mechanism infrastructure |
| `eyes` | `MCH-eyes_parent` | False | False | E: source animator/control infrastructure |
| `eye.L` | `eyes` | False | False | E: source animator/control infrastructure |
| `eye.R` | `eyes` | False | False | E: source animator/control infrastructure |

## Rigid pieces without ordinary skin membership

| Object | Parent bone |
| --- | --- |
| `GumsLower_lowres.001` | `ORG-teeth.B` |
| `GumsUpper_lowres.001` | `ORG-teeth.T` |
| `character2.004` | `MCH-eye.R` |
| `character2.005` | `MCH-eye.L` |
