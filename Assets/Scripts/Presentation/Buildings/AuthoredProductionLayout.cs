using System;
using SilverScreen.Domain;
using SilverScreen.Domain.Movie;
using UnityEngine;

namespace SilverScreen.Presentation.Buildings
{
    [DisallowMultipleComponent]
    public sealed class AuthoredProductionLayout : MonoBehaviour
    {
        public bool IsComplete()
        {
            var stations = GetComponent<ProductionStationLayout>();
            var marks = GetComponent<ActorSceneMarkLayout>();
            var slate = GetComponent<SlatePositionLayout>();
            var blocking = GetComponent<SetBlockingPointLayout>();
            if (stations == null || marks == null || slate == null || blocking == null || GetComponent<ProductionCameraLayout>() == null) return false;
            foreach (ProductionStationType type in Enum.GetValues(typeof(ProductionStationType)))
                if (!stations.TryGetPosition(type, out _)) return false;
            foreach (ActorSceneMarkType type in Enum.GetValues(typeof(ActorSceneMarkType)))
                if (!marks.TryGetPosition(type, out _)) return false;
            foreach (SlatePositionType type in Enum.GetValues(typeof(SlatePositionType)))
                if (!slate.TryGetPosition(type, out _)) return false;
            return blocking.TryGetPosition(SceneBlockingPointIds.Center, out _) &&
                blocking.TryGetPosition(SceneBlockingPointIds.StageLeft, out _) && blocking.TryGetPosition(SceneBlockingPointIds.StageRight, out _);
        }
    }
}
