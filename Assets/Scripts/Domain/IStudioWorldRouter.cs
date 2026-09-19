using System;

namespace SilverScreen.Domain
{
    public enum StudioRouteResult
    {
        Started,
        EmployeeMissing,
        AgentMissing,
        BuildingMissing,
        NavigationRejected,
        StationMissing,
        MarkMissing,
        SlateMissing,
        SequenceInProgress
    }

    public enum ProductionStationType
    {
        Director,
        ActorWaitingA,
        ActorWaitingB,
        ActorWaitingC,
        Camera,
        Sound,
        CrewWaiting
    }
    public enum ActorSceneMarkType
    {
        ActorMarkA,
        ActorMarkB,
        ActorMarkC
    }

    public enum SlatePositionType
    {
        SlateStaging,
        SlateMark
    }

    public interface IStudioWorldRouter
    {
        StudioRouteResult SendEmployeeToBuilding(Employee employee, BuildingType buildingType, EmployeeIntent intent, Action onArrival);
        StudioRouteResult SendEmployeeToProductionStation(Employee employee, BuildingType buildingType, ProductionStationType stationType, EmployeeIntent intent, Action onArrival);
        StudioRouteResult SendEmployeeToSceneMark(Employee employee, BuildingType buildingType, ActorSceneMarkType markType, EmployeeIntent intent, Action onArrival);
        StudioRouteResult StartSlateSequence(BuildingType buildingType, Action onCompleted, Action<StudioRouteResult> onFailed);
        void ReleaseEmployee(Employee employee);
    }
}
