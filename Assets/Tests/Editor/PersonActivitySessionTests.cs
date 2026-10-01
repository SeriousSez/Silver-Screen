using System.Collections.Generic;
using NUnit.Framework;
using SilverScreen.Domain;

namespace SilverScreen.Tests.EditMode
{
    public sealed class PersonActivitySessionTests
    {
        [Test]
        public void SessionReservesPeopleAssignsDistinctPositionsAndAllowsLateJoiningToCapacity()
        {
            var registry = new PersonActivitySessionRegistry();
            var rules = new PersonActivitySessionRules(PersonAutonomousActivity.Socialize, 2, 4, true, 45);
            var first = Employee("session-first");
            var second = Employee("session-second");
            var third = Employee("session-third");
            var fourth = Employee("session-fourth");
            var fifth = Employee("session-fifth");
            var session = registry.Create(rules, new AutonomyPosition(1, 0, 1), first, second);

            Assert.That(session, Is.Not.Null);
            Assert.That(registry.IsReserved(first.Id), Is.True);
            Assert.That(registry.IsReserved(second.Id), Is.True);
            Assert.That(registry.Create(rules, default, first, third), Is.Null);
            Assert.That(registry.MarkArrived(session.Id, first.Id, out _), Is.True);
            Assert.That(session.State, Is.EqualTo(PersonActivitySessionState.Forming));
            Assert.That(registry.MarkArrived(session.Id, second.Id, out var becameActive), Is.True);
            Assert.That(becameActive, Is.True);
            Assert.That(session.State, Is.EqualTo(PersonActivitySessionState.Active));

            var thirdParticipant = registry.TryJoin(session.Id, third);
            var fourthParticipant = registry.TryJoin(session.Id, fourth);
            Assert.That(thirdParticipant, Is.Not.Null);
            Assert.That(fourthParticipant, Is.Not.Null);
            Assert.That(thirdParticipant.HasArrived, Is.False);
            Assert.That(registry.TryJoin(session.Id, fifth), Is.Null);
            Assert.That(session.ParticipantCount, Is.EqualTo(4));
            Assert.That(session.Participants[0].PositionIndex, Is.Not.EqualTo(session.Participants[1].PositionIndex));
            Assert.That(session.Participants[0].PositionIndex, Is.Not.EqualTo(thirdParticipant.PositionIndex));
            Assert.That(session.Participants[1].PositionIndex, Is.Not.EqualTo(fourthParticipant.PositionIndex));
        }

        [Test]
        public void SessionAcceptsConfigurableInitialParticipantCountsAndCancelsBelowMinimum()
        {
            var registry = new PersonActivitySessionRegistry();
            var rules = new PersonActivitySessionRules(PersonAutonomousActivity.Socialize, 3, 5, false, 30);
            var participants = new List<Employee>
            {
                Employee("variable-first"),
                Employee("variable-second"),
                Employee("variable-third")
            };
            Assert.That(registry.Create(rules, default,
                new[] { participants[0], participants[1] }), Is.Null);

            var session = registry.Create(rules, default, participants);
            Assert.That(session, Is.Not.Null);
            Assert.That(session.ParticipantCount, Is.EqualTo(3));
            Assert.That(registry.RemoveParticipant(participants[1].Id).State,
                Is.EqualTo(PersonActivitySessionState.Cancelled));
            Assert.That(registry.Find(session.Id), Is.Null);
            Assert.That(registry.IsReserved(participants[0].Id), Is.False);
            Assert.That(registry.IsReserved(participants[2].Id), Is.False);
        }

        private static Employee Employee(string id) =>
            new Employee(id, id, EmployeeRole.Actor, 50, 1000);
    }
}
