using SilverScreen.Domain;
using SilverScreen.Domain.Interaction;
using SilverScreen.Domain.Recruitment;

namespace SilverScreen.Presentation.Interaction
{
    public readonly struct PersonDropContext
    {
        public readonly Candidate Candidate;
        public readonly Employee Employee;
        public readonly RecruitmentCoordinator Recruitment;
        public readonly PersonPracticeService Practice;
        public readonly SilverScreen.Presentation.Employees.StudioEmployeeManager Workforce;
        public readonly System.Func<WorkforceRoomAction, Employee, bool> RequestDismissal;
        public PersonDropContext(Candidate candidate, Employee employee, RecruitmentCoordinator recruitment, PersonPracticeService practice,
            SilverScreen.Presentation.Employees.StudioEmployeeManager workforce = null,
            System.Func<WorkforceRoomAction, Employee, bool> requestDismissal = null)
        { Candidate = candidate; Employee = employee; Recruitment = recruitment; Practice = practice; Workforce = workforce; RequestDismissal = requestDismissal; }
    }

    /// <summary>Actions supply their existing domain rules. Targets own geometry; carrying owns physical placement.</summary>
    public interface IPersonDropAction
    {
        string TargetId { get; }
        bool CanExecute(PersonDropContext context);
        // Synchronous, no side effects on false. Called after tentative physical placement, before release.
        bool TryExecute(PersonDropContext context);
    }

    public interface IContextualRoomAction : IPersonDropAction
    {
        bool IsRelevant(PersonDropContext context);
        bool IsCurrent(PersonDropContext context);
        string FloorLabel(PersonDropContext context);
    }

    public interface IPersonDropCompleted
    {
        // Runs after literal placement and carry restoration, not during the tentative domain callback.
        void OnPlaced(PersonDropContext context, UnityEngine.Bounds assignmentArea);
    }
}
