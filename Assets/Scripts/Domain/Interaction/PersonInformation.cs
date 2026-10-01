using System;
using System.Collections.Generic;
using SilverScreen.Domain.Recruitment;
using SilverScreen.Domain.Time;

namespace SilverScreen.Domain.Interaction
{
    public sealed class PersonInformation
    {
        public string Category { get; }
        public int Urgency { get; }
        public int Relevance { get; }
        public string Summary { get; }
        public string Expanded { get; }
        public PersonInformation(string category, int urgency, int relevance, string summary, string expanded)
        { Category = category; Urgency = urgency; Relevance = relevance; Summary = summary; Expanded = expanded; }
    }
    public sealed class PersonInformationContext
    {
        public PersonProfile Person;
        public Employee Employee;
        public Candidate Candidate;
        public SimulationDateTime Date;
        public PersonPracticeService Practice;
    }
    public interface IPersonInformationProvider
    {
        void Collect(PersonInformationContext context, List<PersonInformation> information);
    }
    /// <summary>Providers report applicability by emitting cards; ordering is urgency then relevance.</summary>
    public sealed class PersonInformationService
    {
        public List<IPersonInformationProvider> Providers { get; } = new List<IPersonInformationProvider> { new ExistingPersonInformationProvider() };
        public void Collect(PersonInformationContext context, List<PersonInformation> result)
        {
            result.Clear();
            foreach (var provider in Providers) provider.Collect(context, result);
            result.Sort((a, b) => { int order = b.Urgency.CompareTo(a.Urgency); return order != 0 ? order : b.Relevance.CompareTo(a.Relevance); });
        }
    }
    public sealed class ExistingPersonInformationProvider : IPersonInformationProvider
    {
        public void Collect(PersonInformationContext c, List<PersonInformation> cards)
        {
            if (c.Person == null) return;
            var wellbeing = c.Person.Wellbeing;
            string personWellbeing = "Energy: " + wellbeing.Energy + "/100; Stress: " + wellbeing.Stress +
                "/100; Boredom: " + wellbeing.Boredom + "/100; Mood: " + wellbeing.Mood + "/100";
            cards.Add(new PersonInformation("Person wellbeing", 0, 25, personWellbeing, personWellbeing));
            var career = c.Person.Career;
            string workload = career.WorkloadState == PersonWorkloadState.InsufficientHistory
                ? "Insufficient history"
                : career.WorkloadState.ToString();
            string careerSummary = "Career satisfaction: " + career.CareerSatisfaction + "/100";
            PersonCareerGoal activeGoal = null;
            foreach (var goal in career.Goals)
                if (goal.Status == PersonCareerGoalStatus.Active) { activeGoal = goal; break; }
            string goalSummary = activeGoal == null ? "No active career goal" :
                activeGoal.Type + ": " + activeGoal.Status + " (" + activeGoal.Progress + "%)";
            string careerDetails = careerSummary + "\nCareer drive: " + career.CareerDrive + "/100" +
                "\nCareer goal: " + goalSummary +
                "\nRetention: " + career.RetentionState +
                "\nRecent workload: " + career.RecentWorkloadPercent.ToString("F1") +
                "% (" + career.WorkloadWorkedMinutes + "/" + career.WorkloadObservedMinutes + " observed minutes)\nWorkload: " + workload;
            cards.Add(new PersonInformation("Career", 0, 24, careerSummary, careerDetails));
            var e = c.Employee;
            if (c.Candidate != null)
                cards.Add(new PersonInformation("Applicant", 0, 100, "Looking for work", (c.Candidate.IsTalentApplicant ? "Talent applicant; no profession chosen" : "Unemployed; seeking " + c.Candidate.JobSought) + "\nExpected salary: $" + c.Candidate.SalaryExpectation + "/month\nAny available profession may be chosen."));
            if (e != null)
            {
                string activity = e.CurrentIntent.Purpose == EmployeeIntentPurpose.None ? e.CurrentState.ToString() : e.CurrentIntent.Description;
                if (c.Practice?.IsPracticing(e) == true) activity += "\nProgress: " + c.Practice.Progress(e).ToString("P0");
                cards.Add(new PersonInformation("Activity", 0, 90, activity, activity));
                if (!string.IsNullOrWhiteSpace(e.CurrentIntent.TargetBuildingId) && e.CurrentIntent.Purpose != EmployeeIntentPurpose.Practice)
                    cards.Add(new PersonInformation("Work", 20, 100, e.CurrentIntent.Description, e.CurrentIntent.Description + "\nLocation: " + e.CurrentIntent.TargetBuildingId));
                cards.Add(new PersonInformation("Wellbeing", e.Morale < 30 ? 50 : 0, e.Morale < 50 ? 100 : 30, "Morale: " + e.Morale, "Morale: " + e.Morale + "/100"));
                cards.Add(new PersonInformation("Employment", 0, 40, e.Role.ToString(), e.Role + "\nSalary: $" + e.Salary + "/month"));
            }
            var t = c.Person.Talent;
            string abilities = "Acting " + t.ActingAbility + " / Directing " + t.DirectingAbility + " / Writing " + t.WritingAbility;
            foreach (var genre in t.GenreExperience) abilities += "\n" + genre.GenreId + ": " + genre.Experience;
            cards.Add(new PersonInformation("Experience", 0, c.Candidate != null ? 80 : 20, abilities.Split('\n')[0], abilities));
            cards.Add(new PersonInformation("Identity", 0, 10, c.Person.Name, c.Person.Name + "\nAge: " + c.Person.GetAge(c.Date)));
            if (c.Person.TraitIds.Count > 0) cards.Add(new PersonInformation("Personality", 0, 15, string.Join(", ", c.Person.TraitIds), string.Join(", ", c.Person.TraitIds)));
        }
    }
}
