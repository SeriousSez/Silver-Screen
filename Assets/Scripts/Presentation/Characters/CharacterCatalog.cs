using System;
using SilverScreen.Domain.Characters;
using UnityEngine;

namespace SilverScreen.Presentation.Characters
{
    [CreateAssetMenu(menuName = "SilverScreen/Characters/Adult catalog")]
    public sealed class CharacterCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Calibration
        {
            public string id, foundationId, outfitId, hairId;
            public float referenceHeight;
            public GameObject model;
            public Avatar avatar;
        }
        public string rigVersion = CharacterAppearance.AdultRig;
        public Calibration[] calibrations = Array.Empty<Calibration>();

        public Calibration Resolve(CharacterAppearance appearance, string costumeOverride = null)
        {
            if (appearance == null) throw new ArgumentNullException(nameof(appearance));
            appearance.Validate();
            if (rigVersion != appearance.rigVersion) throw new ArgumentException("Catalog rig mismatch.");
            var item = Array.Find(calibrations, x => x.id == appearance.calibrationId);
            if (item == null || item.model == null) throw new ArgumentException("Unknown appearance calibration.");
            if (item.foundationId != appearance.foundationId || item.hairId != appearance.hairId ||
                item.outfitId != (costumeOverride ?? appearance.personalOutfitId))
                throw new ArgumentException("This gate catalog has no fitted asset for the requested foundation/hair/outfit combination.");
            if (Mathf.Abs(appearance.heightMetres / item.referenceHeight - 1) > .0501f)
                throw new ArgumentException("Height exceeds the calibration's tested five-percent envelope.");
            return item;
        }

        public CharacterAppearance CreateReference(string id)
        {
            var c = Array.Find(calibrations, x => x.id == id) ?? throw new ArgumentException("Unknown calibration.");
            return new CharacterAppearance { appearanceId = "gate1-" + id, calibrationId = id,
                foundationId = c.foundationId, personalOutfitId = c.outfitId, hairId = c.hairId, heightMetres = c.referenceHeight };
        }
    }
}
