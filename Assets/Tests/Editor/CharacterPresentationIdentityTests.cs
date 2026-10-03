using System;
using System.Linq;
using NUnit.Framework;
using SilverScreen.Domain;
using SilverScreen.Domain.Characters;
using SilverScreen.Domain.Recruitment;
using SilverScreen.Domain.Time;

namespace SilverScreen.Tests.EditMode
{
    public sealed class CharacterPresentationIdentityTests
    {
        internal static PersonProfile Person(CharacterFamily family = CharacterFamily.Unassigned) => new PersonProfile(
            Guid.NewGuid().ToString(), "Reference person", new SimulationDateTime(1900, 1, 1, 0, 0), ProfessionalRole.Unassigned,
            new TalentProfile(50, 50), appearanceProfileId: "appearance:independent", presentationIdentity: new PersonPresentationIdentity(family));

        [TestCase(CharacterFamily.AdultFemale)] [TestCase(CharacterFamily.AdultMale)]
        [TestCase(CharacterFamily.Boy)] [TestCase(CharacterFamily.Girl)]
        public void IdentitySurvivesRenameHireEveryProfessionAndDismissal(CharacterFamily family)
        {
            var person = Person(family); var identity = person.PresentationIdentity; var candidate = new Candidate(person, 10);
            person.Rename("Changed name"); candidate.MarkWaiting(0); Assert.That(candidate.MarkHired());
            var employee = new Employee(candidate.Person, EmployeeRole.Actor, 10); var roster = new WorkforceRoster(); roster.Add(employee);
            foreach (EmployeeRole role in Enum.GetValues(typeof(EmployeeRole)))
            { employee.ChangeProfession(role); Assert.That(employee.Person.PresentationIdentity, Is.EqualTo(identity)); }
            Assert.That(roster.TryDismiss(employee)); Assert.That(roster.FormerEmployees.Single().Person, Is.SameAs(person));
            Assert.That(person.PresentationIdentity.Family, Is.EqualTo(family));
            Assert.That(person.AppearanceProfileId, Is.EqualTo("appearance:independent"));
        }
        [Test] public void DefaultAndUnknownAreNeverFemale()
        {
            Assert.That(default(PersonPresentationIdentity).Family, Is.EqualTo(CharacterFamily.Unassigned));
            Assert.That(Person().PresentationIdentity.IsCanonical, Is.False);
            var unknown = new PersonPresentationIdentity((CharacterFamily)812);
            Assert.That(unknown.IsCanonical, Is.False); Assert.That((int)unknown.Family, Is.EqualTo(812));
            Assert.That(new Employee("legacy", "Legacy", EmployeeRole.Actor, 30, 10).Person.PresentationIdentity.IsCanonical, Is.False);
        }
        [Test] public void IdentityContainsOnlyUnityFreeValueData()
        {
            var fields = typeof(PersonPresentationIdentity).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(fields.Length, Is.EqualTo(1)); Assert.That(fields.Single().FieldType, Is.EqualTo(typeof(CharacterFamily)));
            Assert.That(typeof(PersonProfile).GetProperties().Any(p => typeof(UnityEngine.Object).IsAssignableFrom(p.PropertyType)), Is.False);
        }
    }
}
