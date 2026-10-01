using System.Collections.Generic;
using SilverScreen.Domain;
using SilverScreen.Domain.Tutorial;
using SilverScreen.Presentation.Buildings;
using SilverScreen.Presentation.Employees;
using SilverScreen.Presentation.Movie;
using SilverScreen.Presentation.SimulationTime;
using SilverScreen.Presentation.Writing;
using UnityEngine;

namespace SilverScreen.Presentation.Tutorial
{
    /// <summary>Session composition. Establishes authored facilities in place without changing saved assets.</summary>
    public sealed class StudioGuidanceDriver : MonoBehaviour
    {
        private readonly Dictionary<string, StudioBuildingView> _starterViews = new Dictionary<string, StudioBuildingView>();
        private SimulationTimeDriver _time;
        private ScreenplayWritingDriver _writing;
        private StudioEmployeeManager _employees;
        private StudioWorldRouter _router;
        private TutorialGameEvents _events;
        private bool _freshStudio;
        private AnnouncementRequest _displayed;
        private float _displayUntil;
        public TutorialSession Tutorial { get; private set; }
        public StarterEstablishmentService Establishment { get; private set; }
        public StudioAnnouncementQueue Announcements { get; private set; }
        private SilverScreen.Domain.Buildings.IOperationalBuildings _operationalBuildings;
        public void BindConstruction(SilverScreen.Domain.Buildings.BuildingConstructionService construction)
        {
            Establishment.Established -= ActivateFacility;
            _operationalBuildings = construction;
        }
        public bool CanRecruit => !_freshStudio || Establishment.IsEstablished(StarterFeatureIds.Casting);

        public void Initialize(NewStudioOptions options, bool freshStudio, SimulationTimeDriver time,
            StudioEmployeeManager employees, ScreenplayWritingDriver writing)
        {
            if (Tutorial != null) return;
            _time = time; _employees = employees; _writing = writing; _freshStudio = freshStudio;
            Tutorial = new TutorialSession(options, time.Clock);
            Announcements = new StudioAnnouncementQueue();
            _router = FindAnyObjectByType<StudioWorldRouter>();
            foreach (var view in FindObjectsByType<StudioBuildingView>(FindObjectsInactive.Exclude))
            {
                string id = view.BuildingType == BuildingType.StudioOffice ? StarterFeatureIds.Headquarters :
                    view.BuildingType == BuildingType.CastingOffice ? StarterFeatureIds.Casting : null;
                if (view.BuildingType == BuildingType.SoundStage &&
                    view.GetComponent<SoundStageIdentity>()?.FacilityId == StudioFilmingCapabilities.StarterStageId) id = StarterFeatureIds.Stage;
                if (id != null && !_starterViews.ContainsKey(id)) _starterViews.Add(id, view);
            }
            Establishment = new StarterEstablishmentService(Tutorial,
                id => _starterViews.TryGetValue(id, out var view) && view != null);
            Establishment.Established += ActivateFacility;
            if (freshStudio)
            {
                foreach (var view in _starterViews.Values) view.gameObject.SetActive(false);
                _writing?.FilmingCapabilities?.RemoveFacility(StudioFilmingCapabilities.StarterStageId);
                _router?.RefreshBuildings();
            }
            if (_writing?.FacilityConstruction != null)
                _writing.FacilityConstruction.AdditionalAvailability = id => Tutorial.Allows(StarterFeatureIds.SpecializedSets);
        }

        private void Start()
        {
            if (Tutorial == null) return;
            var production = FindAnyObjectByType<MovieProductionDriver>()?.Coordinator;
            if (production == null)
            {
                Debug.LogError("Studio guidance requires the existing MovieProductionDriver.", this);
                Tutorial.Skip(); return;
            }
            _events = new TutorialGameEvents(Tutorial, _operationalBuildings ?? Establishment, production.Slate, Announcements, _time.Clock, _writing?.Coordinator);
            if (_employees != null) _employees.OnEmployeeAdded += _events.EmployeeHired;
        }
        private void ActivateFacility(string id)
        {
            _starterViews[id].gameObject.SetActive(true);
            if (id == StarterFeatureIds.Stage && _writing?.FilmingCapabilities != null)
            {
                var starter = StudioFilmingCapabilities.CreateStarterStudio(_writing.KnownSetDefinitions);
                _writing.FilmingCapabilities.AddFacility(starter.Facilities[0]);
            }
            _router?.RefreshBuildings();
            _writing?.MovieProductionService?.RetryUnresolvedEnvironment();
        }
        // Only text expiry uses a frame timer. Objectives and PA requests use domain events.
        private void Update()
        {
            if (Announcements == null || (_displayed != null && Time.unscaledTime < _displayUntil)) return;
            _displayed = Announcements.TakeNext(); _displayUntil = Time.unscaledTime + 8f;
        }
        private void OnGUI()
        {
            if (Tutorial == null) return;
            var message = Tutorial.OpenMessage;
            if (message != null)
            {
                GUILayout.BeginArea(new Rect(Screen.width - 450, 70, 430, 280), GUI.skin.box);
                GUILayout.Label(message.Title);
                GUILayout.Label(message.Body, new GUIStyle(GUI.skin.label) { wordWrap = true });
                if (GUILayout.Button("Continue")) Tutorial.Continue();
                if (GUILayout.Button("Skip tutorial")) Tutorial.Skip();
                GUILayout.EndArea();
            }
            if (_displayed != null)
            {
                GUILayout.BeginArea(new Rect(Screen.width - 450, Screen.height - 135, 430, 100), GUI.skin.box);
                GUILayout.Label("Studio PA / " + _displayed.Priority);
                GUILayout.Label(StudioAnnouncementPresentation.ResolveText(_displayed), new GUIStyle(GUI.skin.label) { wordWrap = true });
                GUILayout.EndArea();
            }
        }
        private void OnDestroy()
        {
            if (_employees != null && _events != null) _employees.OnEmployeeAdded -= _events.EmployeeHired;
            if (Establishment != null) Establishment.Established -= ActivateFacility;
            if (_writing?.FacilityConstruction != null) _writing.FacilityConstruction.AdditionalAvailability = null;
            _events?.Dispose(); Tutorial?.Dispose();
        }
        private void OnDisable() => Tutorial?.SetPresentationActive(false);
        private void OnEnable() => Tutorial?.SetPresentationActive(true);
    }
}
