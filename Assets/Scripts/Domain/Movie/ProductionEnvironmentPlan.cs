using System;
using System.Collections.Generic;

namespace SilverScreen.Domain.Movie
{
    public enum ProductionEnvironmentPlanStatus
    {
        Resolved,
        NoOwnedFacilityAvailable
    }

    public sealed class ProductionEnvironmentScenePlan
    {
        public string MovieSceneId { get; }
        public int SceneNumber { get; }
        public string SceneTitle { get; }
        public string RequiredSetDefinitionId { get; }
        public ProductionEnvironmentPlanStatus Status { get; }
        public ProductionEnvironmentResolution Resolution { get; }
        public bool IsResolved => Resolution != null;
        public ProductionEnvironmentFulfillmentSource? FulfillmentSource => Resolution?.Source;
        public string FacilityId => Resolution?.FacilityId;
        public string FacilityDisplayName => Resolution?.FacilityDisplayName;

        internal ProductionEnvironmentScenePlan(MovieScene scene, ProductionEnvironmentResolution resolution)
        {
            if (scene == null) throw new ArgumentNullException(nameof(scene));

            MovieSceneId = scene.Id;
            SceneNumber = scene.SceneNumber;
            SceneTitle = scene.Title ?? string.Empty;
            RequiredSetDefinitionId = scene.RequiredSetDefinitionId;
            Resolution = resolution;
            Status = resolution != null
                ? ProductionEnvironmentPlanStatus.Resolved
                : ProductionEnvironmentPlanStatus.NoOwnedFacilityAvailable;
        }
    }

    public sealed class ProductionEnvironmentPlan
    {
        private readonly List<ProductionEnvironmentScenePlan> _scenePlans;

        public string MovieProjectId { get; }
        public IReadOnlyList<ProductionEnvironmentScenePlan> ScenePlans => _scenePlans;
        public bool IsFullyResolved => _scenePlans.TrueForAll(scene => scene.IsResolved);

        internal ProductionEnvironmentPlan(string movieProjectId,
            List<ProductionEnvironmentScenePlan> scenePlans)
        {
            MovieProjectId = movieProjectId;
            _scenePlans = scenePlans ?? throw new ArgumentNullException(nameof(scenePlans));
        }
    }

    public sealed class ProductionEnvironmentPlanner
    {
        private readonly IProductionEnvironmentResolver _resolver;

        public ProductionEnvironmentPlanner(IProductionEnvironmentResolver resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        public ProductionEnvironmentPlan CreatePlan(MovieProject movie)
        {
            if (movie == null) throw new ArgumentNullException(nameof(movie));

            var scenePlans = new List<ProductionEnvironmentScenePlan>(movie.Scenes.Count);
            foreach (MovieScene scene in movie.Scenes)
            {
                ProductionEnvironmentResolution resolution =
                    _resolver.ResolveOwnedFacility(scene.RequiredSetDefinitionId);
                scenePlans.Add(new ProductionEnvironmentScenePlan(scene, resolution));
            }

            return new ProductionEnvironmentPlan(movie.Id, scenePlans);
        }
    }
}
