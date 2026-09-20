using System.Linq;
using NUnit.Framework;
using SilverScreen.Domain;
using SilverScreen.Domain.Writing;
using SilverScreen.Domain.Movie;

namespace SilverScreen.Tests.EditMode
{
    public sealed class SpecializedSetConstructionTests
    {
        [TestCase(SetDefinitionIds.GenericInterior)]
        [TestCase(SetDefinitionIds.Office)]
        [TestCase(SetDefinitionIds.LivingRoom)]
        [TestCase(SetDefinitionIds.Bedroom)]
        public void StarterRequirements_ResolveToStageOne(string requirementId)
        {
            var resolver = new OwnedStudioProductionEnvironmentResolver(
                StudioFilmingCapabilities.CreateStarterStudio(SetDefinitionCatalog.CreatePrototype()));

            ProductionEnvironmentResolution result = resolver.ResolveOwnedFacility(requirementId);

            Assert.That(result.FacilityId, Is.EqualTo(StudioFilmingCapabilities.StarterStageId));
            Assert.That(result.RequiredSetDefinitionId, Is.EqualTo(requirementId));
        }

        [Test]
        public void SpecializedRequirements_ResolveToConstructedFacilityInstances()
        {
            CreateServices(out var capabilities, out var construction);
            construction.Construct(SpecializedSetFacilityIds.StreetSet, "street-instance");
            construction.Construct(SpecializedSetFacilityIds.RestaurantCafeSet, "cafe-instance");
            var resolver = new OwnedStudioProductionEnvironmentResolver(capabilities);

            Assert.That(resolver.ResolveOwnedFacility(SetDefinitionIds.Street).FacilityId,
                Is.EqualTo("street-instance"));
            Assert.That(resolver.ResolveOwnedFacility(SetDefinitionIds.RestaurantCafe).FacilityId,
                Is.EqualTo("cafe-instance"));
        }

        [Test]
        public void Resolver_ReturnsNullWhenNoOwnedFacilityCanSatisfyRequirement()
        {
            var resolver = new OwnedStudioProductionEnvironmentResolver(
                StudioFilmingCapabilities.CreateStarterStudio(SetDefinitionCatalog.CreatePrototype()));

            Assert.That(resolver.ResolveOwnedFacility("courtroom"), Is.Null);
        }

        [Test]
        public void Resolver_UsesStableFacilityRegistrationOrderAndSurvivesDuplicateRemoval()
        {
            CreateServices(out var capabilities, out var construction);
            construction.Construct(SpecializedSetFacilityIds.StreetSet, "street-first");
            construction.Construct(SpecializedSetFacilityIds.StreetSet, "street-second");
            var resolver = new OwnedStudioProductionEnvironmentResolver(capabilities);

            Assert.That(resolver.ResolveOwnedFacility(SetDefinitionIds.Street).FacilityId,
                Is.EqualTo("street-first"));
            construction.Remove("street-first");
            Assert.That(resolver.ResolveOwnedFacility(SetDefinitionIds.Street).FacilityId,
                Is.EqualTo("street-second"));
        }

        [Test]
        public void StarterStage_RetainsExactlyItsFourOriginalCapabilities()
        {
            var capabilities = StudioFilmingCapabilities.CreateStarterStudio(SetDefinitionCatalog.CreatePrototype());

            Assert.That(capabilities.Facilities, Has.Count.EqualTo(1));
            Assert.That(capabilities.Facilities[0].SupportedSetDefinitionIds, Is.EquivalentTo(new[]
            {
                SetDefinitionIds.GenericInterior, SetDefinitionIds.Office,
                SetDefinitionIds.LivingRoom, SetDefinitionIds.Bedroom
            }));
            Assert.That(capabilities.CanSatisfy(SetDefinitionIds.Street), Is.False);
            Assert.That(capabilities.CanSatisfy(SetDefinitionIds.RestaurantCafe), Is.False);
        }

        [Test]
        public void ConstructingSpecializedSets_ExpandsStudioCapabilities()
        {
            CreateServices(out var capabilities, out var construction);

            SpecializedSetFacility street = construction.Construct(SpecializedSetFacilityIds.StreetSet, "street-1");
            SpecializedSetFacility cafe = construction.Construct(SpecializedSetFacilityIds.RestaurantCafeSet, "cafe-1");

            Assert.That(street.FilmingCapabilities.Supports(SetDefinitionIds.Street), Is.True);
            Assert.That(cafe.FilmingCapabilities.Supports(SetDefinitionIds.RestaurantCafe), Is.True);
            Assert.That(capabilities.CanSatisfy(SetDefinitionIds.Street), Is.True);
            Assert.That(capabilities.CanSatisfy(SetDefinitionIds.RestaurantCafe), Is.True);
        }

        [Test]
        public void RemovingOneOfDuplicateProviders_PreservesUnionUntilLastProviderIsRemoved()
        {
            CreateServices(out var capabilities, out var construction);
            construction.Construct(SpecializedSetFacilityIds.StreetSet, "street-a");
            construction.Construct(SpecializedSetFacilityIds.StreetSet, "street-b");

            Assert.That(construction.Remove("street-a"), Is.True);
            Assert.That(capabilities.CanSatisfy(SetDefinitionIds.Street), Is.True);
            Assert.That(construction.Remove("street-b"), Is.True);
            Assert.That(capabilities.CanSatisfy(SetDefinitionIds.Street), Is.False);
        }

        [Test]
        public void Generator_UsesNewlyConstructedFacilityWithoutMutatingEarlierContent()
        {
            var definitions = SetDefinitionCatalog.CreatePrototype();
            var capabilities = new StudioFilmingCapabilities(definitions, new[]
            {
                new FilmingFacilityCapabilities("office-only", new[] { SetDefinitionIds.Office })
            });
            var construction = new SpecializedSetConstructionService(
                capabilities, SpecializedSetFacilityDefinition.CreatePrototypeDefinitions());
            var beforeGenerator = new ScreenplayContentGenerator(new SeededScreenplayTitleRandomSource(7), capabilities);
            ScreenplayContent before = beforeGenerator.Generate(
                new ScreenplayProject("before", "Before", new[] { "writer" }, new[] { "drama" }));
            string[] originalRequirements = before.Scenes.Select(scene => scene.RequiredSetDefinitionId).ToArray();

            construction.Construct(SpecializedSetFacilityIds.StreetSet, "street-1");
            var afterGenerator = new ScreenplayContentGenerator(new SeededScreenplayTitleRandomSource(7), capabilities);
            ScreenplayContent after = afterGenerator.Generate(
                new ScreenplayProject("after", "After", new[] { "writer" }, new[] { "drama" }));

            Assert.That(originalRequirements, Is.All.EqualTo(SetDefinitionIds.Office));
            Assert.That(before.Scenes.Select(scene => scene.RequiredSetDefinitionId), Is.EqualTo(originalRequirements));
            Assert.That(after.Scenes.Select(scene => scene.RequiredSetDefinitionId),
                Has.Some.EqualTo(SetDefinitionIds.Street));
        }

        private static void CreateServices(out StudioFilmingCapabilities capabilities,
            out SpecializedSetConstructionService construction)
        {
            capabilities = StudioFilmingCapabilities.CreateStarterStudio(SetDefinitionCatalog.CreatePrototype());
            construction = new SpecializedSetConstructionService(
                capabilities, SpecializedSetFacilityDefinition.CreatePrototypeDefinitions());
        }
    }
}
