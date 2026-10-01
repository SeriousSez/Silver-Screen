using System;

namespace SilverScreen.Domain.Characters
{
    /// <summary>Persist this data, not an RNG seed or a scene/prefab instance ID.</summary>
    [Serializable]
    public sealed class CharacterAppearance
    {
        public const int CurrentSchema = 1;
        public const string AdultRig = "adult-v1.0.0";
        public int schemaVersion = CurrentSchema;
        public string rigVersion = AdultRig;
        public string appearanceId;
        public string calibrationId;
        public string foundationId;
        public string personalOutfitId;
        public string hairId;
        public float heightMetres;
        public float jawWidth, noseWidth, noseBridge, mouthWidth, cheekWidth, chinHeight;

        public CharacterAppearance Copy() => (CharacterAppearance)MemberwiseClone();

        public void Validate()
        {
            if (schemaVersion != CurrentSchema || rigVersion != AdultRig)
                throw new ArgumentException("Unsupported appearance schema or adult rig version.");
            if (string.IsNullOrWhiteSpace(appearanceId) || string.IsNullOrWhiteSpace(calibrationId) ||
                string.IsNullOrWhiteSpace(foundationId) || string.IsNullOrWhiteSpace(personalOutfitId) || string.IsNullOrWhiteSpace(hairId))
                throw new ArgumentException("Appearance requires stable identity, calibration, foundation, outfit and hair IDs.");
            if (!Finite(heightMetres) || heightMetres < 1.45f || heightMetres > 2.10f)
                throw new ArgumentOutOfRangeException(nameof(heightMetres));
            foreach (float value in new[] { jawWidth, noseWidth, noseBridge, mouthWidth, cheekWidth, chinHeight })
                if (!Finite(value) || value < -1 || value > 1) throw new ArgumentOutOfRangeException("Identity control");
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
