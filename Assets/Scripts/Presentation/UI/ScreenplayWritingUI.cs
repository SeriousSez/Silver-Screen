using System.Collections.Generic;
using System.Globalization;
using System.Text;
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
        private Button _startButton;
        private TextMeshProUGUI _startButtonText;
        private int _genreIndex;
        private string _selectedIdeaId;
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
            if (_driver?.IdeaCoordinator != null)
            {
                _driver.IdeaCoordinator.IdeaAdded -= HandleIdeaChanged;
                _driver.IdeaCoordinator.IdeaChanged -= HandleIdeaChanged;
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
            _startButton = ManagementUIFactory.Button("Start", input, "START WRITING", ManagementUIFactory.Gold, Color.black);
            _startButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 190f;
            _startButtonText = _startButton.GetComponentInChildren<TextMeshProUGUI>();
            _startButton.onClick.AddListener(StartWriting);

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
            if (_driver?.IdeaCoordinator != null)
            {
                _driver.IdeaCoordinator.IdeaAdded += HandleIdeaChanged;
                _driver.IdeaCoordinator.IdeaChanged += HandleIdeaChanged;
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
            AddLabel(_screenplayContent, "IDEA LIBRARY");
            var ideas = _driver?.IdeaCoordinator?.Library;
            if (ideas == null || ideas.Count == 0) AddLabel(_screenplayContent, "No ideas developed yet.");
            else foreach (var idea in ideas) AddIdea(idea);
            UpdateAssignmentMode();
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
            bool fromIdea = screenplay.AcquisitionSource == ScreenplayAcquisitionSource.DevelopedFromIdea;
            bool showContent = screenplay.Status == ScreenplayStatus.Completed &&
                               screenplay.ContentStatus == ScreenplayContentStatus.Ready;
            float height = showContent ? CalculateCompletedHeight(screenplay) : fromIdea ? 176f : 112f;
            var row = ManagementUIFactory.Rect("Screenplay_" + screenplay.Id, _screenplayContent, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, height));
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            ManagementUIFactory.Background(row, ManagementUIFactory.Panel);
            var writerLines = new List<string>();
            foreach (var contributor in screenplay.Contributors)
            {
                var writer = FindEmployee(contributor.WriterId);
                writerLines.Add($"{writer?.Name ?? contributor.WriterId}: {contributor.Status}");
            }
            string credits = screenplay.Status == ScreenplayStatus.Completed ? "\nCredits: " + Names(screenplay.CreditedWriterIds) : string.Empty;
            var genres = new List<string>();
            foreach (string genreId in screenplay.GenreIds) genres.Add(ToDisplayName(genreId));
            string origin = fromIdea ? "Developed from Story Idea" : "Commissioned";
            string creative = fromIdea
                ? $"\nSetting: {DisplayId(screenplay.SettingId)}  •  Protagonist: {DisplayId(screenplay.ProtagonistArchetypeId)}\nAntagonist: {(string.IsNullOrEmpty(screenplay.AntagonistArchetypeId) ? "None" : DisplayId(screenplay.AntagonistArchetypeId))}  •  Theme: {DisplayId(screenplay.ThemeId)}"
                : string.Empty;
            string content = showContent ? BuildScreenplayContent(screenplay) :
                screenplay.Status == ScreenplayStatus.Completed && screenplay.ContentStatus == ScreenplayContentStatus.Failed
                    ? "\n<color=#E6A33E>Structured content could not be finalized.</color>"
                    : string.Empty;
            string evaluation = BuildEvaluation(screenplay);
            var text = ManagementUIFactory.Text("Text", row, $"<b>{screenplay.Title}</b>\n{string.Join(" / ", genres)}  •  {origin}\n{screenplay.Status}  •  {screenplay.Progress:P0}{creative}\nWriters: {string.Join("  |  ", writerLines)}{credits}{evaluation}{content}", 13f, TextAlignmentOptions.TopLeft, Color.white);
            ManagementUIFactory.SetOffsets(text.rectTransform, 12f, 4f, 12f, 4f);
        }

        private static float CalculateCompletedHeight(ScreenplayProject screenplay)
        {
            int lines = 12 + screenplay.Characters.Count * 2;
            foreach (ScreenplayScene scene in screenplay.Scenes)
                lines += 3 + scene.Beats.Count * 2;
            return Mathf.Max(240f, lines * 18f + 20f);
        }

        private static string BuildEvaluation(ScreenplayProject screenplay)
        {
            if (screenplay.EvaluationStatus == ScreenplayEvaluationStatus.Failed)
                return "\n<color=#E6A33E>Creative evaluation unavailable.</color>";
            ScreenplayEvaluation value = screenplay.Evaluation;
            if (screenplay.EvaluationStatus != ScreenplayEvaluationStatus.Ready || value == null)
                return string.Empty;

            string Score(double score) => (score / 10d).ToString("0.0", CultureInfo.InvariantCulture);
            return $"\n\n<color=#E6A33E><b>{Score(value.OverallQuality)} / 10</b></color>" +
                   $"\nCharacters {Score(value.CharacterDevelopment)}  •  Structure {Score(value.StructureCoherence)}  •  Dialogue {Score(value.Dialogue)}" +
                   $"\nPacing {Score(value.Pacing)}  •  Genre {Score(value.GenreExecution)}  •  Variety {Score(value.SceneVariety)}  •  Theme {Score(value.ThematicExecution)}";
        }

        private static string BuildScreenplayContent(ScreenplayProject screenplay)
        {
            var builder = new StringBuilder("\n\n<b>CHARACTERS</b>");
            foreach (ScreenplayCharacter character in screenplay.Characters)
            {
                builder.Append("\n").Append(character.Name).Append(" — ").Append(character.Role);
                if (!string.IsNullOrEmpty(character.ArchetypeId))
                    builder.Append(" — ").Append(DisplayId(character.ArchetypeId));
            }

            builder.Append("\n\n<b>SCENES</b>");
            foreach (ScreenplayScene scene in screenplay.Scenes)
            {
                builder.Append("\n\n<b>").Append(scene.SceneNumber).Append(". ")
                    .Append(scene.LocationType == ScreenplaySceneLocation.Interior ? "INT. " : "EXT. ")
                    .Append(DisplayId(scene.LocationId).ToUpperInvariant()).Append(" - ")
                    .Append(scene.TimeOfDay.ToString().ToUpperInvariant()).Append("</b>");
                if (!string.IsNullOrEmpty(scene.Title)) builder.Append("  ").Append(scene.Title);
                builder.Append("\nCharacters: ");
                for (int i = 0; i < scene.ParticipatingCharacterIds.Count; i++)
                {
                    if (i > 0) builder.Append(", ");
                    builder.Append(FindCharacterName(screenplay, scene.ParticipatingCharacterIds[i]));
                }
                foreach (ScreenplayBeat beat in scene.Beats)
                {
                    builder.Append("\n<color=#E6A33E>").Append(beat.BeatType.ToString().ToUpperInvariant());
                    if (beat.PerformingCharacterId != null)
                        builder.Append(" — ").Append(FindCharacterName(screenplay, beat.PerformingCharacterId));
                    builder.Append("</color>\n").Append(beat.Content);
                }
            }
            return builder.ToString();
        }

        private static string FindCharacterName(ScreenplayProject screenplay, string characterId)
        {
            foreach (ScreenplayCharacter character in screenplay.Characters)
                if (character.Id == characterId) return character.Name;
            return characterId;
        }

        private void StartWriting()
        {
            ScreenplayProject screenplay;
            if (!string.IsNullOrEmpty(_selectedIdeaId))
            {
                StoryIdea idea = FindIdea(_selectedIdeaId);
                screenplay = _driver?.Coordinator?.CreateFromIdea(idea, _selectedWriterIds);
                _status.text = screenplay != null
                    ? $"Development started from {idea.Title}. Writers are travelling to the Script Office."
                    : "Select 1–4 idle Writers and ensure enough Script Office stations are free.";
            }
            else
            {
                screenplay = _driver?.Coordinator?.CreateCommissioned(GenreIds[_genreIndex], _selectedWriterIds);
                _status.text = screenplay != null
                    ? "Writers assigned. They are travelling to the Script Office."
                    : "Select 1–4 idle Writers (one per available station).";
            }
            if (screenplay == null) return;
            _selectedIdeaId = null;
            _selectedWriterIds.Clear();
            Refresh();
        }

        private void AddIdea(StoryIdea idea)
        {
            bool complete = idea.State == IdeaDevelopmentState.Completed;
            float height = complete ? 210f : 86f;
            var row = ManagementUIFactory.Rect("Idea_" + idea.Id, _screenplayContent, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, height));
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            ManagementUIFactory.Background(row, ManagementUIFactory.Panel);
            string creator = FindEmployee(idea.CreatorWriterId)?.Name ?? idea.CreatorWriterId;
            string value;
            if (!complete)
            {
                value = $"<b>{creator}</b>\n{(idea.State == IdeaDevelopmentState.Traveling ? "Travelling to develop an idea..." : "Developing an idea...")}\n{idea.Progress:P0}";
            }
            else
            {
                var genres = new List<string>(); foreach (string genre in idea.GenreIds) genres.Add(ToDisplayName(genre));
                string development = idea.HasScreenplayDevelopment
                    ? $"\nDeveloped as: {FindScreenplay(idea.DevelopedScreenplayId)?.Title ?? idea.DevelopedScreenplayId}"
                    : string.Empty;
                value = $"<b>{idea.Title}</b>\n{string.Join(" / ", genres)}\nSetting: {DisplayId(idea.SettingId)}\nProtagonist: {DisplayId(idea.ProtagonistArchetypeId)}\nAntagonist: {(string.IsNullOrEmpty(idea.AntagonistArchetypeId) ? "None" : DisplayId(idea.AntagonistArchetypeId))}\nTheme: {DisplayId(idea.ThemeId)}\nIdea by: {creator}{development}";
            }
            var text = ManagementUIFactory.Text("Text", row, value, 13f, TextAlignmentOptions.MidlineLeft, Color.white);
            ManagementUIFactory.SetOffsets(text.rectTransform, 12f, complete ? 38f : 5f, 12f, 5f);
            if (complete && !idea.HasScreenplayDevelopment)
            {
                var buttonRect = ManagementUIFactory.Rect("DevelopScreenplay", row,
                    new Vector2(.55f, 0f), new Vector2(1f, 0f), new Vector2(0f, 5f), new Vector2(-8f, 34f));
                var button = ManagementUIFactory.Button("Button", buttonRect,
                    _selectedIdeaId == idea.Id ? "SELECTED FOR DEVELOPMENT" : "DEVELOP SCREENPLAY",
                    _selectedIdeaId == idea.Id ? ManagementUIFactory.Gold : ManagementUIFactory.PanelRaised,
                    _selectedIdeaId == idea.Id ? Color.black : Color.white);
                button.onClick.AddListener(() => SelectIdeaForDevelopment(idea.Id));
            }
        }

        private void SelectIdeaForDevelopment(string ideaId)
        {
            StoryIdea idea = FindIdea(ideaId);
            if (idea == null || idea.State != IdeaDevelopmentState.Completed ||
                idea.HasScreenplayDevelopment) return;
            _selectedIdeaId = ideaId;
            _selectedWriterIds.Clear();
            _status.text = $"Developing from Idea: {idea.Title}. Select 1–4 Writers, then start development.";
            Refresh();
        }

        private void UpdateAssignmentMode()
        {
            StoryIdea selectedIdea = FindIdea(_selectedIdeaId);
            if (selectedIdea == null || selectedIdea.State != IdeaDevelopmentState.Completed ||
                selectedIdea.HasScreenplayDevelopment)
            {
                _selectedIdeaId = null;
                if (_genreButtonText != null)
                    _genreButtonText.text = "GENRE: " + GenreNames[_genreIndex].ToUpperInvariant();
                if (_startButtonText != null) _startButtonText.text = "START WRITING";
                return;
            }
            if (_genreButtonText != null)
                _genreButtonText.text = "FROM IDEA: " + selectedIdea.Title.ToUpperInvariant();
            if (_startButtonText != null) _startButtonText.text = "START DEVELOPMENT";
        }

        private static string DisplayId(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return "None";
            string[] words = id.Split('-');
            for (int i = 0; i < words.Length; i++) if (words[i].Length > 0) words[i] = char.ToUpperInvariant(words[i][0]) + words[i].Substring(1);
            return string.Join(" ", words);
        }

        private void CycleGenre()
        {
            if (!string.IsNullOrEmpty(_selectedIdeaId))
            {
                _selectedIdeaId = null;
                _selectedWriterIds.Clear();
                _status.text = "Idea development cancelled. Choose a genre to commission a screenplay.";
                Refresh();
                return;
            }
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
        private StoryIdea FindIdea(string id)
        { var ideas = _driver?.IdeaCoordinator?.Library; if (ideas != null) foreach (var idea in ideas) if (idea.Id == id) return idea; return null; }
        private ScreenplayProject FindScreenplay(string id)
        { var screenplays = _driver?.Coordinator?.Library; if (screenplays != null) foreach (var screenplay in screenplays) if (screenplay.Id == id) return screenplay; return null; }
        private void HandleEmployeeAdded(Employee unused) => Refresh();
        private void HandleScreenplayChanged(ScreenplayProject unused) => Refresh();
        private void HandleIdeaChanged(StoryIdea unused) => Refresh();
        private static void AddLabel(Transform parent, string value)
        { var row = ManagementUIFactory.Rect("Empty", parent, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, 44f)); row.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f; ManagementUIFactory.Text("Text", row, value, 14f, TextAlignmentOptions.MidlineLeft, ManagementUIFactory.Muted); }
        private static void Clear(Transform parent) { for (int i = parent.childCount - 1; i >= 0; i--) Destroy(parent.GetChild(i).gameObject); }
    }
}
