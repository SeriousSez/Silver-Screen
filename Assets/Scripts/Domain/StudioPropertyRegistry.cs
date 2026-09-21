using System;
using System.Collections.Generic;

namespace SilverScreen.Domain
{
    public interface IStudioPropertyRegistry
    {
        IReadOnlyList<StudioCompany> StudioCompanies { get; }
        IReadOnlyList<PropertyParcel> Properties { get; }
        IReadOnlyList<StudioCampus> Campuses { get; }

        StudioCompany GetStudioCompany(string companyId);
        PropertyParcel GetProperty(string parcelId);
        string GetCurrentOwnerId(string parcelId);
        IReadOnlyList<PropertyParcel> GetPropertiesOwnedByStudioCompany(string companyId);
        StudioCampus GetCampus(string campusId);
        IReadOnlyList<StudioCampus> GetCampusesOwnedByStudioCompany(string companyId);
        bool ParcelBelongsToCampus(string parcelId);
        StudioCampus GetCampusForParcel(string parcelId);
        IReadOnlyList<PropertyParcel> GetPropertiesInCampus(string campusId);
        bool RenameProperty(string parcelId, string displayName);
        bool RenameCampus(string campusId, string displayName);
    }

    public sealed class StudioPropertyRegistry : IStudioPropertyRegistry
    {
        private readonly List<StudioCompany> _studioCompanies = new List<StudioCompany>();
        private readonly List<PropertyParcel> _properties = new List<PropertyParcel>();
        private readonly List<StudioCampus> _campuses = new List<StudioCampus>();
        private readonly Dictionary<string, StudioCompany> _companiesById =
            new Dictionary<string, StudioCompany>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, PropertyParcel> _propertiesById =
            new Dictionary<string, PropertyParcel>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, StudioCampus> _campusesById =
            new Dictionary<string, StudioCampus>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<StudioCompany> StudioCompanies => _studioCompanies;
        public IReadOnlyList<PropertyParcel> Properties => _properties;
        public IReadOnlyList<StudioCampus> Campuses => _campuses;

        public StudioPropertyRegistry(
            IEnumerable<StudioCompany> studioCompanies,
            IEnumerable<PropertyParcel> properties,
            IEnumerable<StudioCampus> campuses)
        {
            if (studioCompanies == null) throw new ArgumentNullException(nameof(studioCompanies));
            if (properties == null) throw new ArgumentNullException(nameof(properties));
            if (campuses == null) throw new ArgumentNullException(nameof(campuses));

            foreach (StudioCompany company in studioCompanies)
            {
                if (company == null || _companiesById.ContainsKey(company.Id)) continue;
                _companiesById.Add(company.Id, company);
                _studioCompanies.Add(company);
            }

            foreach (StudioCampus campus in campuses)
            {
                if (campus == null || _campusesById.ContainsKey(campus.Id)) continue;
                if (!_companiesById.ContainsKey(campus.OwningStudioCompanyId))
                    throw new ArgumentException("A campus owner must be a known studio company.", nameof(campuses));
                _campusesById.Add(campus.Id, campus);
                _campuses.Add(campus);
            }

            foreach (PropertyParcel property in properties)
            {
                if (property == null || _propertiesById.ContainsKey(property.Id)) continue;
                RegisterProperty(property);
            }
        }

        public StudioCompany GetStudioCompany(string companyId) =>
            GetById(_companiesById, companyId);

        public PropertyParcel GetProperty(string parcelId) => GetById(_propertiesById, parcelId);

        public string GetCurrentOwnerId(string parcelId) => GetProperty(parcelId)?.OwnerId;

        public IReadOnlyList<PropertyParcel> GetPropertiesOwnedByStudioCompany(string companyId)
        {
            var owned = new List<PropertyParcel>();
            string normalized = NormalizeOptionalId(companyId);
            if (normalized == null || !_companiesById.ContainsKey(normalized)) return owned;
            foreach (PropertyParcel property in _properties)
                if (string.Equals(property.OwnerId, normalized, StringComparison.OrdinalIgnoreCase))
                    owned.Add(property);
            return owned;
        }

        public StudioCampus GetCampus(string campusId) => GetById(_campusesById, campusId);

        public IReadOnlyList<StudioCampus> GetCampusesOwnedByStudioCompany(string companyId)
        {
            var owned = new List<StudioCampus>();
            string normalized = NormalizeOptionalId(companyId);
            if (normalized == null || !_companiesById.ContainsKey(normalized)) return owned;
            foreach (StudioCampus campus in _campuses)
                if (string.Equals(campus.OwningStudioCompanyId, normalized, StringComparison.OrdinalIgnoreCase))
                    owned.Add(campus);
            return owned;
        }

        public bool ParcelBelongsToCampus(string parcelId) => GetCampusForParcel(parcelId) != null;

        public StudioCampus GetCampusForParcel(string parcelId)
        {
            PropertyParcel property = GetProperty(parcelId);
            return property == null ? null : GetCampus(property.StudioCampusId);
        }

        public IReadOnlyList<PropertyParcel> GetPropertiesInCampus(string campusId)
        {
            var members = new List<PropertyParcel>();
            StudioCampus campus = GetCampus(campusId);
            if (campus == null) return members;
            foreach (string parcelId in campus.ParcelIds)
            {
                PropertyParcel property = GetProperty(parcelId);
                if (property != null) members.Add(property);
            }
            return members;
        }

        public bool RenameProperty(string parcelId, string displayName) =>
            GetProperty(parcelId)?.Rename(displayName) == true;

        public bool RenameCampus(string campusId, string displayName) =>
            GetCampus(campusId)?.Rename(displayName) == true;

        public static StudioPropertyRegistry CreatePrototype()
        {
            var companies = new[]
            {
                new StudioCompany(StudioCompanyIds.SilverScreenStudios, StudioIdentity.DefaultName),
                new StudioCompany(StudioCompanyIds.MajesticPictures, "Majestic Pictures"),
                new StudioCompany(StudioCompanyIds.ColumbiaHeightsStudios, "Columbia Heights Studios")
            };
            var campuses = new[]
            {
                new StudioCampus(
                    StudioCampusIds.SilverScreenMainStudio,
                    StudioCompanyIds.SilverScreenStudios,
                    "Main Studio")
            };
            var properties = new[]
            {
                new PropertyParcel(
                    PropertyParcelIds.SilverScreenMainStudio,
                    "Main Studio Property",
                    StudioCompanyIds.SilverScreenStudios,
                    StudioCampusIds.SilverScreenMainStudio),
                new PropertyParcel(
                    PropertyParcelIds.CarltonApartments,
                    "Carlton Apartments",
                    PropertyOwnerIds.PrivateResidentialOwner,
                    isPotentiallyPurchasable: true),
                new PropertyParcel(
                    PropertyParcelIds.DowntownWarehouse,
                    "Downtown Warehouse",
                    PropertyOwnerIds.PrivateCommercialOwner,
                    isPotentiallyPurchasable: true),
                new PropertyParcel(
                    PropertyParcelIds.HawthorneHouse,
                    "Hawthorne House",
                    PropertyOwnerIds.PrivateResidentialOwner),
                new PropertyParcel(
                    PropertyParcelIds.PacificTextileWorks,
                    "Pacific Textile Works",
                    PropertyOwnerIds.PrivateIndustrialOwner,
                    isPotentiallyPurchasable: true)
            };
            return new StudioPropertyRegistry(companies, properties, campuses);
        }

        private void RegisterProperty(PropertyParcel property)
        {
            StudioCampus campus = GetCampus(property.StudioCampusId);
            if (property.StudioCampusId != null && campus == null)
                throw new ArgumentException("A property references an unknown campus.", nameof(property));
            if (campus != null && !string.Equals(
                    campus.OwningStudioCompanyId,
                    property.OwnerId,
                    StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("A campus property must be owned by the campus company.", nameof(property));

            _propertiesById.Add(property.Id, property);
            _properties.Add(property);
            campus?.AddParcel(property.Id);
        }

        private static TValue GetById<TValue>(Dictionary<string, TValue> values, string id)
            where TValue : class
        {
            string normalized = NormalizeOptionalId(id);
            if (normalized == null) return null;
            values.TryGetValue(normalized, out TValue value);
            return value;
        }

        private static string NormalizeOptionalId(string value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
    }
}
