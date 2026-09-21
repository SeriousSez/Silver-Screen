using System;
using System.Collections.Generic;
using SilverScreen.Domain;
using UnityEngine;

namespace SilverScreen.Presentation.Buildings
{
    [DisallowMultipleComponent]
    public sealed class FilmingLocationView : MonoBehaviour
    {
        public string FilmingLocationId { get; private set; }
        public string DisplayName { get; private set; }
        public Transform Entrance { get; private set; }

        public void Initialize(FilmingLocation location, Transform entrance)
        {
            if (location == null) throw new ArgumentNullException(nameof(location));
            FilmingLocationId = location.Id;
            DisplayName = location.DisplayName;
            Entrance = entrance;
        }
    }

    public sealed class FilmingLocationViewRegistry
    {
        private readonly List<FilmingLocationView> _views = new List<FilmingLocationView>();
        private readonly Dictionary<string, FilmingLocationView> _viewsById =
            new Dictionary<string, FilmingLocationView>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<FilmingLocationView> Views => _views;

        public bool Register(FilmingLocationView view)
        {
            if (view == null || string.IsNullOrWhiteSpace(view.FilmingLocationId) ||
                _viewsById.ContainsKey(view.FilmingLocationId))
            {
                return false;
            }

            _views.Add(view);
            _viewsById.Add(view.FilmingLocationId, view);
            return true;
        }

        public FilmingLocationView GetView(string filmingLocationId)
        {
            if (string.IsNullOrWhiteSpace(filmingLocationId)) return null;
            _viewsById.TryGetValue(filmingLocationId.Trim(), out FilmingLocationView view);
            return view;
        }
    }
}
