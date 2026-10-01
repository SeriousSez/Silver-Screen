using System;
using System.Collections.Generic;
using System.Linq;
using SilverScreen.Domain.Performance;

namespace SilverScreen.Domain.Writing
{
    /// <summary>Edits the screenplay's owned content. It never owns a second movie or any production state.</summary>
    public sealed class ScreenplayAuthoring
    {
        private readonly ScreenplayProject _project;
        private readonly Dictionary<string, IntendedPerformer> _actors = new Dictionary<string, IntendedPerformer>();
        public string IntendedDirectorId { get; private set; }
        public bool IsSubmitted { get; private set; }
        public IReadOnlyList<IntendedPerformer> IntendedActors => Array.AsReadOnly(_actors.Values.OrderBy(actor => actor.CharacterId, StringComparer.Ordinal).ToArray());
        internal ScreenplayAuthoring(ScreenplayProject project) => _project = project;

        public void Rename(string title) => Commit(TemplateValidation.Id(title), _project.Characters, _project.Scenes);
        public void AddCharacter(ScreenplayCharacter character)
        {
            if (character == null) throw new ArgumentNullException(nameof(character));
            if (_project.Characters.Any(item => item.Id == character.Id)) throw new ArgumentException("Duplicate character ID.");
            Commit(_project.Title, _project.Characters.Concat(new[] { character }), _project.Scenes);
        }
        public void ReplaceCharacter(ScreenplayCharacter character)
        {
            if (character == null || !_project.Characters.Any(item => item.Id == character.Id)) throw new ArgumentException("Unknown character.");
            Commit(_project.Title, _project.Characters.Select(item => item.Id == character.Id ? character : item), _project.Scenes);
        }
        public void RemoveCharacter(string id)
        {
            if (_project.Scenes.Any(scene => scene.ParticipatingCharacterIds.Contains(id))) throw new InvalidOperationException("Character is still used by a scene.");
            RequireCharacter(id); _actors.Remove(id);
            Commit(_project.Title, _project.Characters.Where(character => character.Id != id), _project.Scenes);
        }
        public void AddScene(string id, string title, ScreenplaySceneAuthoring plan)
        {
            id = TemplateValidation.Id(id);
            if (_project.Scenes.Any(scene => scene.Id == id)) throw new ArgumentException("Duplicate scene ID.");
            ValidateCharacters(plan);
            Commit(_project.Title, _project.Characters,
                _project.Scenes.Concat(new[] { plan.CreateScene(id, _project.Scenes.Count + 1, title) }));
        }
        public void UpdateScene(string id, string title, ScreenplaySceneAuthoring plan)
        {
            var current = RequireScene(id); ValidateCharacters(plan);
            var replacement = plan.CreateScene(current.Id, current.SceneNumber, title);
            Commit(_project.Title, _project.Characters, _project.Scenes.Select(scene => scene.Id == id ? replacement : scene));
        }
        public void MoveScene(string id, int index)
        {
            var scene = RequireScene(id);
            if (index < 0 || index >= _project.Scenes.Count) throw new ArgumentOutOfRangeException(nameof(index));
            var scenes = _project.Scenes.ToList(); scenes.Remove(scene); scenes.Insert(index, scene);
            Commit(_project.Title, _project.Characters, scenes);
        }
        public void RemoveScene(string id)
        {
            RequireScene(id); Commit(_project.Title, _project.Characters, _project.Scenes.Where(scene => scene.Id != id));
        }

        public void AssignActor(string characterId, string personId, Func<string, PerformerPreviewInfo> resolve = null)
        {
            RequireCharacter(characterId); personId = TemplateValidation.Id(personId);
            if (_actors.Values.Any(actor => actor.CharacterId != characterId && actor.PersonId == personId))
                throw new ArgumentException("An intended actor is already assigned to another character.");
            if (_project.Scenes.SelectMany(scene => scene.Authoring.BackgroundGroups).Any(group => group.SelectedPersonIds.Contains(personId)))
                throw new ArgumentException("An intended actor is also selected as background population.");
            var info = resolve?.Invoke(personId);
            if (info != null && (!info.IsActor || RequiredCapabilities(characterId).Any(capability => !info.Capabilities.Contains(capability))))
                throw new ArgumentException("Known performer is incompatible with the character's template roles.");
            _actors[characterId] = new IntendedPerformer(characterId, personId);
            InvalidateSubmission();
        }
        public void ClearActor(string characterId)
        { RequireCharacter(characterId); if (_actors.Remove(characterId)) InvalidateSubmission(); }
        public void AssignDirector(string personId)
        { IntendedDirectorId = TemplateValidation.Id(personId); InvalidateSubmission(); }
        public void ClearDirector()
        { IntendedDirectorId = null; InvalidateSubmission(); }

        public AuthoredScenePreview Preview(string sceneId, Func<string, PerformerPreviewInfo> resolve = null)
        {
            var scene = RequireScene(sceneId); var plan = scene.Authoring;
            var warnings = new List<string>();
            foreach (var actor in IntendedActors.Where(actor => scene.ParticipatingCharacterIds.Contains(actor.CharacterId)))
            {
                var info = resolve?.Invoke(actor.PersonId);
                if (info == null) warnings.Add("Unresolved performer: " + actor.PersonId);
                else if (!info.Available) warnings.Add("Unavailable performer: " + actor.PersonId);
                else if (!info.IsActor || RequiredCapabilities(actor.CharacterId).Any(capability => !info.Capabilities.Contains(capability)))
                    warnings.Add("Incompatible performer: " + actor.PersonId);
            }
            return new AuthoredScenePreview(scene.Id, plan, IntendedActors.Where(actor => scene.ParticipatingCharacterIds.Contains(actor.CharacterId)), warnings);
        }
        public void Submit()
        {
            if (_project.Characters.Count == 0 || _project.Scenes.Count == 0) throw new InvalidOperationException("Author at least one character and scene before submission.");
            foreach (var scene in _project.Scenes) ValidateCharacters(scene.Authoring);
            IsSubmitted = true; _project.SubmitAuthoredContent();
        }
        internal void InvalidateSubmission() => Commit(_project.Title, _project.Characters, _project.Scenes);
        private void Commit(string title, IEnumerable<ScreenplayCharacter> characters, IEnumerable<ScreenplayScene> scenes)
        {
            var characterCopy = characters.ToArray(); var sceneCopy = scenes.ToArray();
            IsSubmitted = false;
            _project.ReplaceAuthoredContent(title, characterCopy, sceneCopy);
        }
        private void ValidateCharacters(ScreenplaySceneAuthoring plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            foreach (var binding in plan.Bindings) RequireCharacter(binding.CharacterId);
            if (plan.BackgroundGroups.SelectMany(group => group.SelectedPersonIds).Any(id => _actors.Values.Any(actor => actor.PersonId == id)))
                throw new ArgumentException("A principal visualization actor cannot also be selected in a background group.");
        }
        private IEnumerable<string> RequiredCapabilities(string characterId) => _project.Scenes.SelectMany(scene => scene.Authoring.Bindings
            .Where(binding => binding.CharacterId == characterId).SelectMany(binding => scene.Authoring.Template.Roles
                .Single(role => role.Id == binding.RoleId).RequiredCapabilities)).Distinct();
        private void RequireCharacter(string id)
        { if (!_project.Characters.Any(character => character.Id == id)) throw new ArgumentException("Unknown screenplay character: " + id); }
        private ScreenplayScene RequireScene(string id) => _project.Scenes.SingleOrDefault(scene => scene.Id == id)
            ?? throw new ArgumentException("Unknown authored scene: " + id);
    }

    public sealed class AuthoredScenePreview
    {
        public string SourceSceneId { get; }
        public ScenePerformance Performance { get; }
        public IReadOnlyList<AuthoredDialogue> Dialogue { get; }
        public IReadOnlyList<PlannedSound> Sounds { get; }
        public IReadOnlyList<IntendedPerformer> IntendedActors { get; }
        public IReadOnlyList<string> ReferenceWarnings { get; }
        internal AuthoredScenePreview(string id, ScreenplaySceneAuthoring plan, IEnumerable<IntendedPerformer> actors, IEnumerable<string> warnings)
        {
            SourceSceneId = id; Performance = plan.ResolvePerformance(); Sounds = SceneSoundPlanner.Resolve(plan);
            var active = plan.ResolveBeatReferences().Select(beat => beat.Key).ToHashSet();
            Dialogue = Array.AsReadOnly(plan.Dialogue.Where(line => active.Contains(line.Beat.Key)).ToArray());
            IntendedActors = TemplateValidation.Copy(actors); ReferenceWarnings = Array.AsReadOnly(warnings.ToArray());
        }
    }
}
