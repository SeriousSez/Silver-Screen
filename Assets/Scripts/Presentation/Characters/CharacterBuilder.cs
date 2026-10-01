using System;
using UnityEngine;
using SilverScreen.Domain.Characters;

namespace SilverScreen.Presentation.Characters
{
    public static class CharacterBuilder
    {
        /// <summary>Same saved profile builds studio and detached performance instances.</summary>
        public static CharacterView Build(CharacterCatalog catalog, CharacterAppearance profile, Transform parent = null, string costumeOverride = null)
        {
            var calibration = catalog.Resolve(profile, costumeOverride);
            var root = new GameObject("Character_" + profile.appearanceId);
            try
            {
                root.transform.SetParent(parent, false);
                var model = UnityEngine.Object.Instantiate(calibration.model, root.transform, false);
                model.name = "Visual";
                // FBX may synthesize a body-only LODGroup from mesh suffixes.
                // Replace its registration before constructing the complete character LODs.
                foreach (var lod in model.GetComponentsInChildren<LODGroup>())
                { lod.SetLODs(Array.Empty<LOD>()); if (Application.isPlaying) UnityEngine.Object.Destroy(lod); else UnityEngine.Object.DestroyImmediate(lod); }
                var animator = model.GetComponent<Animator>();
                if (animator != null) animator.enabled = false; // Gate driver owns the common calibrated skeleton.
                var view = root.AddComponent<CharacterView>();
                view.Initialize(profile, calibration.referenceHeight);
                return view;
            }
            catch
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(root); else UnityEngine.Object.DestroyImmediate(root);
                throw;
            }
        }
    }
}
