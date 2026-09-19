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
        StationMissing
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

    public interface IStudioWorldRouter
    {
        StudioRouteResult SendEmployeeToBuilding(Employee employee, BuildingType buildingType, EmployeeIntent intent, Action onArrival);
        StudioRouteResult SendEmployeeToProductionStation(Employee employee, BuildingType buildingType, ProductionStationType stationType, EmployeeIntent intent, Action onArrival);
        void ReleaseEmployee(Employee employee);
    }
}