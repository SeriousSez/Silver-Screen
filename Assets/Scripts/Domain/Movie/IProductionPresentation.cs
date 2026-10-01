using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SilverScreen.Domain.Writing;

namespace SilverScreen.Domain.Movie
{
    public enum PresentationStatus { Running, Completed, Failed, Canceled }
    public interface IPresentationSession : IDisposable { PresentationStatus Status { get; } }
    public interface IProductionPresentation { IPresentationSession Begin(TakePresentationSnapshot snapshot); }

    /// <summary>A detached presentation request. No mutable take, work service or production callbacks are exposed.</summary>
    public sealed class TakePresentationSnapshot
    {
        public string MovieId { get; }
        public string SceneId { get; }
        public string TakeId { get; }
        public long ActivityRevision { get; }
        public string FacilityId { get; }
        public string MovieTitle { get; }
        public int SceneNumber { get; }
        public int TakeNumber { get; }
        public IReadOnlyList<ScreenplayBeat> Beats { get; }
        public IReadOnlyList<SceneShot> Shots { get; }
        public IReadOnlyList<BeatPerformanceResult> Performances { get; }
        public IReadOnlyDictionary<string, string> Cast { get; }

        public TakePresentationSnapshot(MovieProject movie, MovieScene scene, MovieTake take, string facilityId, long revision)
        {
            MovieId = movie.Id; SceneId = scene.Id; TakeId = take.Id; ActivityRevision = revision;
            FacilityId = facilityId; MovieTitle = movie.Title; SceneNumber = scene.SceneNumber; TakeNumber = take.TakeNumber;
            Beats = Array.AsReadOnly(scene.Beats.Select(b => new ScreenplayBeat(b.Id, b.Order, b.BeatType, b.Content, b.PerformingCharacterId,
                b.TargetCharacterId, b.EmotionalIntention, b.BlockingTargetId, b.IntendedIntensity)).ToArray());
            Shots = Array.AsReadOnly(scene.Shots.Select(s => new SceneShot(s.Id, s.Order, s.ShotType, s.SubjectCharacterId, s.ScreenplayBeatId)).ToArray());
            Performances = Array.AsReadOnly(take.PerformanceResults.ToArray());
            Cast = new ReadOnlyDictionary<string, string>(movie.Roles.Where(r => r.AssignedActorId != null).ToDictionary(r => r.Id, r => r.AssignedActorId));
        }

        public SceneShot GetShotForBeat(string beatId) => Shots.FirstOrDefault(s => s.ScreenplayBeatId == beatId);
    }
}
