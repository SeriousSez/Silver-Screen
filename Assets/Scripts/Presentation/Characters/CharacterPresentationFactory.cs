using System;
using SilverScreen.Domain;
using SilverScreen.Presentation.Employees;
using SilverScreen.Presentation.Interaction;
using SilverScreen.Presentation.Recruitment;
using UnityEngine;
using UnityEngine.AI;

namespace SilverScreen.Presentation.Characters
{
    public readonly struct CharacterPresentationResult
    {
        public readonly CharacterPresentation Presentation;
        public readonly string Diagnostic;
        public bool IsCanonical => Presentation != null && Presentation.IsCanonical;
        public bool Succeeded => Diagnostic == null;
        public CharacterPhysicalProfile PhysicalProfile => IsCanonical ? Presentation.PhysicalProfile : CharacterPhysicalProfile.Legacy;
        internal CharacterPresentationResult(CharacterPresentation presentation, string diagnostic)
        { Presentation = presentation; Diagnostic = diagnostic; }
    }

    /// <summary>Explicit opt-in only. Never loads a catalogue or regenerates assets from an Update loop.</summary>
    public static class CharacterPresentationFactory
    {
        public static CharacterPresentationResult Attach(GameObject root, PersonProfile person, CharacterFamilyCatalog catalog,
            CharacterPhysicalProfile? profileOverride = null)
        {
            if (root == null || person == null) throw new ArgumentNullException(root == null ? nameof(root) : nameof(person));
            var current = root.GetComponent<CharacterPresentation>();
            var employee = root.GetComponent<EmployeeAgent>(); var candidate = root.GetComponent<CandidateAgent>();
            var boundPerson = employee != null ? employee.Employee?.Person : candidate != null ? candidate.Candidate?.Person : null;
            if (boundPerson != null && !ReferenceEquals(boundPerson, person))
                return new CharacterPresentationResult(current, "Root agent belongs to a different person.");
            var held = root.GetComponent<HeldPersonPresentation>();
            if (current != null && !ReferenceEquals(current.Person, person))
                return new CharacterPresentationResult(current, "Existing presentation belongs to a different person.");
            if ((held != null && held.State != HeldPersonPresentationState.Normal) || (current != null && current.IsPosingHeld))
                return new CharacterPresentationResult(current, "Explicit rebuild deferred until held/settling presentation releases ownership.");
            string reason = null; CharacterFamilyCatalogEntry entry = default;
            if (!person.PresentationIdentity.IsCanonical) reason = "Presentation family is unassigned or unknown; legacy representation retained.";
            else if (catalog == null) reason = "Local character catalogue absent; run Character Runtime setup.";
            else catalog.TryResolve(person.PresentationIdentity.Family, out entry, out reason);
            if (reason == null && (root.transform.lossyScale - Vector3.one).sqrMagnitude > .000001f) reason = "Canonical person root must have unit world scale.";
            if (reason == null && Vector3.Dot(root.transform.up, Vector3.up) < .99999f) reason = "Canonical person root must remain upright.";
            var profile = reason == null ? profileOverride ?? entry.Definition.DefaultPhysicalProfile : CharacterPhysicalProfile.Legacy;
            if (reason == null) profile.TryValidate(out reason);
            var adapter = root.GetComponent<HeldPersonPoseAdapter>();
            if (reason == null && adapter != null && adapter != current) reason = "Root already has another visual pose authority.";
            if (reason == null && (root.GetComponents<NavMeshAgent>().Length != 1 || root.GetComponents<CapsuleCollider>().Length != 1))
                reason = "Existing person root requires one navigation agent and one capsule collider.";
            if (reason != null)
            {
                if (current == null) EnsureLegacy(root);
                // A failed rebuild preserves the entire working representation, including its physical fit.
                return new CharacterPresentationResult(current, reason);
            }
            if (current != null && current.Definition == entry.Definition && current.PhysicalProfile.Equals(profile))
                return new CharacterPresentationResult(current, null);

            // Validate a detached instance before touching a working renderer/profile. Wrapper whitelist
            // prevents simulation components from running during instantiation.
            var visual = UnityEngine.Object.Instantiate(entry.Visual);
            visual.gameObject.SetActive(false);
            if (!visual.TryValidate(entry.Definition, out reason))
            { CharacterPresentation.DestroyOwned(visual.gameObject); if (current == null) EnsureLegacy(root); return new CharacterPresentationResult(current, reason); }
            var face = new CharacterFacialController(visual.Face, entry.Definition.FacialBinding);
            var nav = root.GetComponent<NavMeshAgent>();
            var feet = root.transform.position - Vector3.up * (nav != null ? nav.baseOffset : 1);
            root.GetComponent<CandidateAgent>()?.PreparePresentationReplacement();
            var oldVisual = current != null ? current.Visual : null;
            if (current == null) current = root.AddComponent<CharacterPresentation>();
            visual.transform.SetParent(root.transform, false);
            current.Initialize(person, entry.Definition, profile, visual, face);
            if (employee != null && employee.Employee != null) employee.BindDomain(employee.Employee);
            if (candidate != null && candidate.Candidate != null) candidate.RebindPresentation();
            root.transform.position = feet + Vector3.up * profile.BaseOffset;
            var legacyRenderer = root.GetComponent<MeshRenderer>(); if (legacyRenderer != null) legacyRenderer.enabled = false;
            visual.gameObject.SetActive(true);
            if (oldVisual != null) { oldVisual.gameObject.SetActive(false); CharacterPresentation.DestroyOwned(oldVisual.gameObject); }
            return new CharacterPresentationResult(current, null);
        }

        private static void EnsureLegacy(GameObject root)
        {
            var nav = root.GetComponent<NavMeshAgent>();
            var feet = root.transform.position - Vector3.up * (nav != null ? nav.baseOffset : 1);
            var filter = root.GetComponent<MeshFilter>(); if (filter == null) filter = root.AddComponent<MeshFilter>();
            var renderer = root.GetComponent<MeshRenderer>(); if (renderer == null) renderer = root.AddComponent<MeshRenderer>();
            filter.sharedMesh = EmployeeNavigationProfile.BodyMesh;
            if (renderer.sharedMaterial == null)
            {
                var primitive = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                primitive.SetActive(false);
                renderer.sharedMaterial = primitive.GetComponent<MeshRenderer>().sharedMaterial;
                CharacterPresentation.DestroyOwned(primitive);
            }
            if (root.GetComponent<CapsuleCollider>() == null) root.AddComponent<CapsuleCollider>();
            EmployeeNavigationProfile.Apply(root);
            root.transform.position = feet + Vector3.up;
        }
    }
}
