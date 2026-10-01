using SilverScreen.Domain;
using System.Collections.Generic;
using System.Linq;
using SilverScreen.Presentation.Interaction;
using UnityEngine;

namespace SilverScreen.Presentation.Buildings
{
    /// <summary>Revision-1 interior workforce layout, in building-local metres. No source geometry or legacy anchors move.</summary>
    public static class StudioServicesContextualRooms
    {
        public static void Install(StudioServicesFacility facility)
        {
            if (facility.transform.Find("ContextualWorkforceRooms") != null) return;
            var cutaway = facility.GetComponent<BuildingCutawayController>();
            if (cutaway == null || facility.Anchor("HireConstructionWorker") == null) return;
            // These two thin authored partitions divide workshop/office and office/supply.
            // Their LOD1/2 triangles share retained-interior batches; separate them without hiding furnishings.
            var partitions = facility.transform.Find("InteriorPartitions");
            var lods = facility.transform.Find("GeneratedLODRepresentations");
            var sources = new List<MeshRenderer>();
            if (partitions != null) sources.AddRange(partitions.GetComponentsInChildren<MeshRenderer>(true));
            var props = facility.transform.Find("InteriorProps");
            var lighting = facility.transform.Find("InteriorLighting");
            if (props != null) sources.AddRange(props.GetComponentsInChildren<MeshRenderer>(true));
            if (lighting != null) sources.AddRange(lighting.GetComponentsInChildren<MeshRenderer>(true));
            if (lods != null) sources.AddRange(lods.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.name.StartsWith("RetainedInteriorAndYard")));
            var sections = new List<Bounds>();
            var supports = new List<string>();
            var mountedColliders = new List<Collider>();
            // Extract mounted fixtures before wall triangles so they follow their support
            // intact, including the same geometry in the retained-interior LOD batches.
            foreach (var parent in new[] { lighting, partitions, props })
                if (parent != null) foreach (Transform fixture in parent)
                {
                    string support = parent == lighting ? "Roof" :
                        fixture.name.StartsWith("ToolRackCarpenter_") ? "OfficeWorkshopPartition" :
                        fixture.name.StartsWith("ClockSchoolhouse_") || fixture.name.StartsWith("NoticeBoardTimber_") ||
                        fixture.name.StartsWith("GroundsToolRack_") ? "OfficeSupplyPartition" : null;
                    if (support == null) continue;
                    var renderers = fixture.GetComponentsInChildren<Renderer>(true);
                    if (renderers.Length == 0) continue;
                    mountedColliders.AddRange(fixture.GetComponentsInChildren<Collider>(true));
                    var bounds = new Bounds(facility.transform.InverseTransformPoint(renderers[0].bounds.center),Vector3.zero);
                    foreach (var renderer in renderers)
                        for (int corner = 0; corner < 8; corner++)
                        {
                            var b = renderer.bounds;
                            bounds.Encapsulate(facility.transform.InverseTransformPoint(new Vector3(
                                (corner & 1) == 0 ? b.min.x : b.max.x,
                                (corner & 2) == 0 ? b.min.y : b.max.y,
                                (corner & 4) == 0 ? b.min.z : b.max.z)));
                        }
                    bounds.Expand(.035f);
                    sections.Add(bounds); supports.Add(support);
                }
            int partitionStart = sections.Count;
            sections.AddRange(new[] {
                new Bounds(new Vector3(-.25f,1.7f,0),new Vector3(.28f,3.6f,8.6f)),
                new Bounds(new Vector3(-3.5f,1.7f,.3f),new Vector3(6.8f,3.6f,.32f))
            });
            var partitionColliders = facility.GetComponentsInChildren<Collider>(true).Where(c =>
                c.name.StartsWith("Office workshop partition") || c.name.StartsWith("Office store partition") || c.name == "Store doorway header").ToArray();
            cutaway.AddInteriorSections(sections.ToArray(),sources.ToArray(),partitionColliders.Concat(mountedColliders).ToArray());
            var added = cutaway.Groups.Where(g => g.Id.StartsWith("InteriorSection")).ToArray();
            added[partitionStart].Id = "OfficeWorkshopPartition";
            added[partitionStart + 1].Id = "OfficeSupplyPartition";
            for (int i = 0; i < supports.Count; i++) added[i].SupportGroupId = supports[i];
            var focus = new List<Vector3>();
            // Replace the old tiny hiring overlays in runtime instances only. Leave the prefab's source bindings intact.
            foreach (var spot in facility.GetComponentsInChildren<PersonInteractionSpot>())
                if (spot.Activity == PersonSpotActivity.Hire)
                { spot.enabled = false; var oldTarget = spot.GetComponent<ContextualDropTarget>(); if (oldTarget != null) oldTarget.enabled = false; }
            var root = new GameObject("ContextualWorkforceRooms").transform; root.SetParent(facility.transform, false);
            Add("Construction", ProfessionalRole.ConstructionWorker, false,
                new[] { new Rect(-.05f, -4.1f, 6.65f, 8.2f) }, new Vector2(2.65f,-1.1f), 4.3f,
                new Vector3(2.5f,0,-.25f), Quaternion.identity);
            Add("Groundskeeping", ProfessionalRole.Groundskeeper, false,
                new[] { new Rect(-6.6f,-4.1f,6.15f,4.2f) },
                new Vector2(-3.4f,-2.8f), 4.3f, new Vector3(-2.5f,0,-2.6f), Quaternion.identity);
            Add("Dismiss", ProfessionalRole.ConstructionWorker, true,
                new[] { new Rect(-6.6f,.5f,6.15f,3.6f) },
                new Vector2(-3.7f,1.45f), 3.4f, new Vector3(-3.1f,0,1.8f), Quaternion.Euler(0,90,0));
            cutaway.SetInteriorFocusPoints(focus.ToArray());

            void Add(string name, ProfessionalRole role, bool dismiss, Rect[] shape, Vector2 label, float labelWidth, Vector3 position, Quaternion rotation)
            {
                var room = new GameObject(name + " room"); room.transform.SetParent(root, false);
                var placement = new GameObject("Placement").transform; placement.SetParent(room.transform, false);
                placement.localPosition = position; placement.localRotation = rotation;
                var action = room.AddComponent<WorkforceRoomAction>();
                action.Configure(facility.FacilityId + ":workforce:" + name, facility.FacilityId, role, dismiss, facility.Anchor("ApplicantExit"));
                room.AddComponent<ContextualDropTarget>().ConfigureRoom(action, placement, cutaway, shape, label, labelWidth);
                focus.Add(new Vector3(label.x,.06f,label.y));
                foreach (var rect in shape)
                {
                    focus.Add(new Vector3(rect.center.x,.06f,rect.center.y));
                    foreach (float x in new[] { rect.xMin + .2f,rect.xMax - .2f })
                        foreach (float z in new[] { rect.yMin + .2f,rect.yMax - .2f }) focus.Add(new Vector3(x,.06f,z));
                }
            }
        }
    }
}
