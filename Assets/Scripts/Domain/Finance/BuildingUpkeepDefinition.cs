using System;
using System.Collections.Generic;

namespace SilverScreen.Domain.Finance
{
    [Serializable]
    public sealed class BuildingUpkeepDefinition
    {
        private static readonly IReadOnlyList<BuildingUpkeepDefinition> DefaultDefinitions =
            new List<BuildingUpkeepDefinition>
            {
                new BuildingUpkeepDefinition(BuildingType.StudioOffice, "Studio Headquarters", Money.FromDollars(2000)),
                new BuildingUpkeepDefinition(BuildingType.CastingOffice, "Casting Office", Money.FromDollars(1000)),
                new BuildingUpkeepDefinition(BuildingType.SoundStage, "Sound Stage 1", Money.FromDollars(3000))
            };

        public BuildingUpkeepDefinition(BuildingType buildingType, string displayName, Money monthlyCost)
        {
            BuildingType = buildingType;
            DisplayName = displayName ?? buildingType.ToString();
            MonthlyCost = monthlyCost;
        }

        public BuildingType BuildingType { get; }
        public string DisplayName { get; }
        public Money MonthlyCost { get; }

        public static IReadOnlyList<BuildingUpkeepDefinition> Defaults => DefaultDefinitions;

        public static BuildingUpkeepDefinition ForType(BuildingType buildingType)
        {
            foreach (var definition in DefaultDefinitions)
            {
                if (definition.BuildingType == buildingType) return definition;
            }

            return null;
        }
    }
}
