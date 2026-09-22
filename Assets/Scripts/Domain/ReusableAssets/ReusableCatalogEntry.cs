using System;
using System.Collections.Generic;

namespace SilverScreen.Domain.ReusableAssets
{
    public enum PlacementSurface { Wall, Ceiling, Ground, DoorLeaf }
    public enum FixtureSuitability { Indoor, Outdoor, Both }
    public enum FixtureRole { Architectural, Decorative, Both }
    public enum FixtureHandedness { None, LeftHinge, RightHinge }
    public enum InsertRole { AcousticFabric, Poster, StudioNotice, Photograph, AdvertisingSignage, PlayerMoviePoster }
    public enum ArtworkFit { FitWithMat, Crop }

    /// <summary>Catalog facts without Unity asset references or development provenance.</summary>
    public sealed class ReusableCatalogEntry
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Category { get; }
        public string Subcategory { get; }
        public int AvailableFromYear { get; }
        public int? AvailableUntilYear { get; }
        public IReadOnlyList<string> EnvironmentTags { get; }

        public ReusableCatalogEntry(string id, string displayName, string category, string subcategory,
            int availableFromYear, int? availableUntilYear, IEnumerable<string> environmentTags)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A stable ID is required.", nameof(id));
            if (availableUntilYear.HasValue && availableUntilYear < availableFromYear)
                throw new ArgumentException("Availability must not end before it begins.", nameof(availableUntilYear));
            Id = id; DisplayName = displayName; Category = category; Subcategory = subcategory;
            AvailableFromYear = availableFromYear; AvailableUntilYear = availableUntilYear;
            EnvironmentTags = new List<string>(environmentTags ?? Array.Empty<string>()).AsReadOnly();
        }

        public bool IsAvailable(int year) => year >= AvailableFromYear && (!AvailableUntilYear.HasValue || year <= AvailableUntilYear.Value);
    }

    /// <summary>Aspect-preserving layout; centred UV crop or centred plane with visible mat.</summary>
    public readonly struct ArtworkLayout
    {
        public readonly float Width, Height, U, V, OffsetU, OffsetV;
        private ArtworkLayout(float width, float height, float u, float v)
        { Width = width; Height = height; U = u; V = v; OffsetU = (1-u)/2; OffsetV = (1-v)/2; }

        public static ArtworkLayout Calculate(float apertureWidth, float apertureHeight, float imageWidth, float imageHeight, ArtworkFit fit)
        {
            if (!(apertureWidth > 0 && apertureHeight > 0 && imageWidth > 0 && imageHeight > 0)
                || float.IsInfinity(apertureWidth + apertureHeight + imageWidth + imageHeight))
                throw new ArgumentOutOfRangeException(nameof(apertureWidth), "Image and aperture dimensions must be finite and positive.");
            float apertureAspect = apertureWidth / apertureHeight, imageAspect = imageWidth / imageHeight;
            if (fit == ArtworkFit.Crop)
                return imageAspect > apertureAspect
                    ? new ArtworkLayout(apertureWidth, apertureHeight, apertureAspect/imageAspect, 1)
                    : new ArtworkLayout(apertureWidth, apertureHeight, 1, imageAspect/apertureAspect);
            return imageAspect > apertureAspect
                ? new ArtworkLayout(apertureWidth, apertureWidth/imageAspect, 1, 1)
                : new ArtworkLayout(apertureHeight*imageAspect, apertureHeight, 1, 1);
        }
    }
}
