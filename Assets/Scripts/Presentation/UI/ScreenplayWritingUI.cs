using System.Collections.Generic;
using SilverScreen.Domain;
using SilverScreen.Domain.Writing;
using SilverScreen.Presentation.Employees;
using SilverScreen.Presentation.Writing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SilverScreen.Presentation.UI
{
    public sealed class ScreenplayWritingUI : MonoBehaviour
    {
        private readonly HashSet<string> _selectedWriterIds = new HashSet<string>();
        private StudioEmployeeManager _employees;
        private ScreenplayWritingDriver _driver;
        private Button _genreButton;
        private TextMeshProUGUI _genreButtonText;
        private int _genreIndex;
        private static readonly string[] GenreIds = { "drama", "comedy", "action", "romance", "thriller", "horror" };
        private static readonly string[] GenreNames = { "Drama", "Comedy", "Action", "Romance", "Thriller", "Horror" };
        private RectTransform _writerContent;
        private RectTransform _screenplayContent;
        private TextMeshProUGUI _status;

        public void Initialize(StudioEmployeeManager employees, ScreenplayWritingDriver driver)
        {
            _employees = employees; _driver = driver; Build(); Bind(); Refresh();
        }

        private void OnDestroy()
        {
            if (_employees != null) _employees.OnEmployeeAdded -= HandleEmployeeAdded;
            if (_driver?.Coordinator != null)
            {
                _driver.Coordinator.ScreenplayAdded -= HandleScreenplayChanged;
                _driver.Coordinator.ScreenplayChanged -= HandleScreenplayChanged;
            }
        }

        private void Build()
        {
            ManagementUIFactory.Background(GetComponent<RectTransform>(), ManagementUIFactory.Panel);
            var heading = ManagementUIFactory.Rect("Heading", transform, new Vector2(0f, .88f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            ManagementUIFactory.Text("Text", heading, "SCREENPLAY DEVELOPMENT", 28f, TextAlignmentOptions.Center, ManagementUIFactory.Gold).fontStyle = FontStyles.Bold;

            var input = ManagementUIFactory.Rect("Assignment", transform, new Vector2(.04f, .70f), new Vector2(.96f, .87f), Vector2.zero, Vector2.zero);
            var layout = input.gameObject.AddComponent<HorizontalLayoutGroup>(); layout.spacing = 8f; layout.childControlWidth = true; layout.childControlHeight = true;
            _genreButton = ManagementUIFactory.Button("Genre", input, "GENRE: DRAMA", ManagementUIFactory.PanelRaised, Color.white);
            _genreButton.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            _genreButtonText = _genreButton.GetComponentInChildren<TextMeshProUGUI>();
            _genreButton.onClick.AddListener(CycleGenre);
            var start = ManagementUIFactory.Button("Start", input, "START WRITING", ManagementUIFactory.Gold, Color.black);
            start.gameObject.AddComponent<LayoutElement>().preferredWidth = 170f; start.onClick.AddListener(StartWriting);

            var writers = ManagementUIFactory.Rect("Writers", transform, new Vector2(.04f, .30f), new Vector2(.47f, .68f), Vector2.zero, Vector2.zero);
            ManagementUIFactory.Background(writers, ManagementUIFactory.PanelRaised);
            ManagementUIFactory.ScrollView("WriterList", writers, out _writerContent);
            var screenplays = ManagementUIFactory.Rect("Screenplays", transform, new Vector2(.50f, .14f), new Vector2(.96f, .68f), Vector2.zero, Vector2.zero);
            ManagementUIFactory.Background(screenplays, ManagementUIFactory.PanelRaised);
            ManagementUIFactory.ScrollView("ScreenplayList", screenplays, out _screenplayContent);
            var statusRect = ManagementUIFactory.Rect("Status", transform, new Vector2(.04f, .14f), new Vector2(.47f, .28f), Vector2.zero, Vector2.zero);
            _status = ManagementUIFactory.Text("Text", statusRect, string.Empty, 14f, TextAlignmentOptions.TopLeft, ManagementUIFactory.Muted);
        }

        private void Bind()
        {
            if (_employees != null) _employees.OnEmployeeAdded += HandleEmployeeAdded;
            if (_driver?.Coordinator != null)
            {
                _driver.Coordinator.ScreenplayAdded += HandleScreenplayChanged;
                _driver.Coordinator.ScreenplayChanged += HandleScreenplayChanged;
            }
        }

        public void Refresh()
        {
            if (_writerContent == null || _screenplayContent == null) return;
            _selectedWriterIds.RemoveWhere(id =>
            {
                var writer = FindEmployee(id);
                return writer == null || writer.Role != EmployeeRole.Writer ||
                       !(_driver?.Coordinator?.CanAssignWriter(id) ?? false);
            });
            Clear(_writerContent); Clear(_screenplayContent);
            int eligible = 0;
            if (_employees != null) foreach (var employee in _employees.AllEmployees)
            {
                if (employee.Role != EmployeeRole.Writer) continue;
                eligible++;
                var toggle = CreateToggle(_writerContent, $"{employee.Name}  •  Skill {employee.Skill}", employee.Id);
                toggle.interactable = _driver?.Coordinator?.CanAssignWriter(employee.Id) ?? false;
            }
            if (eligible == 0) AddLabel(_writerContent, "No hired Writers. Recruit one through the Script Office.");

            var library = _driver?.Coordinator?.Library;
            if (library == null || library.Count == 0) AddLabel(_screenplayContent, "No screenplays in development.");
            else foreach (var screenplay in library) AddScreenplay(screenplay);
        }

        private Toggle CreateToggle(Transform parent, string label, string writerId)
        {
            var row = ManagementUIFactory.Rect("Writer_" + writerId, parent, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, 42f));
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 42f;
            ManagementUIFactory.Background(row, ManagementUIFactory.Panel);

            var box = ManagementUIFactory.Rect("Checkbox", row, new Vector2(0f, .12f), new Vector2(.10f, .88f), Vector2.zero, Vector2.zero);
            var boxImage = ManagementUIFactory.Background(box, new Color(.03f, .04f, .05f, 1f));
            var toggle = box.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = boxImage;
            var check = ManagementUIFactory.Rect("Checkmark", box, new Vector2(.22f, .22f), new Vector2(.78f, .78f), Vector2.zero, Vector2.zero);
            var checkImage = ManagementUIFactory.Background(check, ManagementUIFactory.Gold);
            checkImage.raycastTarget = false;
            toggle.graphic = checkImage;
            toggle.isOn = _selectedWriterIds.Contains(writerId);

            var labelRect = ManagementUIFactory.Rect("WriterLabel", row, new Vector2(.11f, 0f), Vector2.one, Vector2.zero, Vector2.zero);
            var labelImage = ManagementUIFactory.Background(labelRect, Color.clear);
            var labelButton = labelRect.gameObject.AddComponent<Button>();
            labelButton.targetGraphic = labelImage;
            var text = ManagementUIFactory.Text("Text", labelRect, label, 14f, TextAlignmentOptions.MidlineLeft, Color.white);
            ManagementUIFactory.SetOffsets(text.rectTransform, 6f, 2f, 6f, 2f);
            text.raycastTarget = false;

            toggle.onValueChanged.AddListener(selected =>
            {
                if (selected) _selectedWriterIds.Add(writerId);
                else _selectedWriterIds.Remove(writerId);
            });
            labelButton.onClick.AddListener(() => { if (toggle.interactable) toggle.isOn = !toggle.isOn; });
            return toggle;
        }

        private void AddScreenplay(ScreenplayProject screenplay)
        {
            var row = ManagementUIFactory.Rect("Screenplay_" + screenplay.Id, _screenplayContent, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, 112f));
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 112f;
            ManagementUIFactory.Background(row, ManagementUIFactory.Panel);
            var writerLines = new List<string>();
            foreach (var contributor in screenplay.Contributors)
            {
                var writer = FindEmployee(contributor.WriterId);
                writerLines.Add($"{writer?.Name ?? contributor.WriterId}: {contributor.Status}");
            }
            string credits = screenplay.Status == ScreenplayStatus.Completed ? "\nCredits: " + Names(screenplay.CreditedWriterIds) : string.Empty;
            string genre = ToDisplayName(screenplay.PrimaryGenreId);
            var text = ManagementUIFactory.Text("Text", row, $"<b>{screenplay.Title}</b>\n{genre}  •  {screenplay.Status}  •  {screenplay.Progress:P0}\nWriters: {string.Join("  |  ", writerLines)}{credits}", 13f, TextAlignmentOptions.MidlineLeft, Color.white);
            ManagementUIFactory.SetOffsets(text.rectTransform, 12f, 4f, 12f, 4f);
        }

        private void StartWriting()
        {
            var screenplay = _driver?.Coordinator?.CreateCommissioned(GenreIds[_genreIndex], _selectedWriterIds);
            _status.text = screenplay != null ? "Writers assigned. They are travelling to the Script Office." : "Select 1–4 idle Writers (one per available station).";
            if (screenplay != null) { _selectedWriterIds.Clear(); Refresh(); }
        }

        private void CycleGenre()
        {
            _genreIndex = (_genreIndex + 1) % GenreIds.Length;
            if (_genreButtonText != null) _genreButtonText.text = "GENRE: " + GenreNames[_genreIndex].ToUpperInvariant();
        }

        private static string ToDisplayName(string genreId)
        {
            for (int i = 0; i < GenreIds.Length; i++) if (GenreIds[i] == genreId) return GenreNames[i];
            return genreId;
        }

        private string Names(IReadOnlyList<string> ids)
        { var names = new List<string>(); foreach (string id in ids) names.Add(FindEmployee(id)?.Name ?? id); return string.Join(", ", names); }
        private Employee FindEmployee(string id)
        { if (_employees != null) foreach (var employee in _employees.AllEmployees) if (employee.Id == id) return employee; return null; }
        private void HandleEmployeeAdded(Employee unused) => Refresh();
        private void HandleScreenplayChanged(ScreenplayProject unused) => Refresh();
        private static void AddLabel(Transform parent, string value)
        { var row = ManagementUIFactory.Rect("Empty", parent, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, 44f)); row.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f; ManagementUIFactory.Text("Text", row, value, 14f, TextAlignmentOptions.MidlineLeft, ManagementUIFactory.Muted); }
        private static void Clear(Transform parent) { for (int i = parent.childCount - 1; i >= 0; i--) Destroy(parent.GetChild(i).gameObject); }
    }
}
