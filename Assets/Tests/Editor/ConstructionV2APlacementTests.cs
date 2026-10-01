using NUnit.Framework;
using SilverScreen.Domain.Buildings;
using SilverScreen.Domain.Finance;
using SilverScreen.Domain.Resources;
using SilverScreen.Domain.Time;
using SilverScreen.Domain.Tutorial;
using SilverScreen.Domain.Work;
using SilverScreen.Presentation.Buildings;

namespace SilverScreen.Tests.EditMode
{
    public sealed class ConstructionV2APlacementTests
    {
        [Test]
        public void ConfirmationRechecksWorldBlockersWithoutChargingOrCreatingASite()
        {
            var clock = new SimulationClock(1930, 1, 1, 8, 0);
            using var work = new WorkService(clock, new SimulationScheduler(clock));
            var finance = new StudioFinances(clock.CurrentTime);
            var resources = new ResourceReservationBook();
            string obstruction = null;
            using var service = new BuildingConstructionService(StudioBuildingDefinitions.Create(), new PlacementRect(0, 0, 100, 100),
                null, clock, work, finance, resources, worldValidation: (d, p) => obstruction);
            Assert.That(service.Validate(StarterFeatureIds.Casting, default), Is.Null);
            var cash = finance.CurrentCash; int transactions = finance.Transactions.Count;
            obstruction = "Insufficient clearance";
            Assert.That(service.Place(StarterFeatureIds.Casting, default, out var error), Is.Null);
            Assert.That(error, Is.EqualTo(obstruction));
            Assert.That(service.Buildings, Is.Empty); Assert.That(resources.Count, Is.Zero);
            Assert.That(finance.CurrentCash, Is.EqualTo(cash)); Assert.That(finance.Transactions.Count, Is.EqualTo(transactions));
            obstruction = null;
            var site = service.Place(StarterFeatureIds.Casting, new BuildingPose(2, 3, 90), out error);
            Assert.That(error, Is.Null); Assert.That(site.Pose.X, Is.EqualTo(2)); Assert.That(site.Pose.Z, Is.EqualTo(3));
            Assert.That(site.Pose.Yaw, Is.EqualTo(90)); Assert.That(site.State, Is.EqualTo(BuildingLifecycle.Planned));
            clock.Advance(600d);
            Assert.That(service.Progress(site), Is.Zero); Assert.That(site.IsOperational, Is.False);
        }

        [Test]
        public void AffordabilityChangesBetweenSelectionAndConfirmationAreRespected()
        {
            var clock = new SimulationClock(1930, 1, 1, 8, 0);
            using var work = new WorkService(clock, new SimulationScheduler(clock));
            var finance = new StudioFinances(clock.CurrentTime, Money.FromDollars(12000));
            using var service = new BuildingConstructionService(StudioBuildingDefinitions.Create(), new PlacementRect(0, 0, 100, 100),
                null, clock, work, finance, new ResourceReservationBook());
            Assert.That(service.AvailabilityReason(StarterFeatureIds.Casting), Is.Null);
            finance.TryRecordExpense(Money.FromDollars(1), FinancialTransactionCategory.BuildingConstruction, clock.CurrentTime, "Other expense");
            Assert.That(service.AvailabilityReason(StarterFeatureIds.Casting), Is.EqualTo("Insufficient funds"));
            Assert.That(service.Place(StarterFeatureIds.Casting, default, out _), Is.Null);
            Assert.That(finance.CurrentCash, Is.EqualTo(Money.FromDollars(11999))); Assert.That(service.Buildings, Is.Empty);
        }

        [Test]
        public void OffsetFootprintRotatesAroundInstanceOriginRatherThanItsOwnCenter()
        {
            var footprint = new PlacementRect(3, 1, 6, 2);
            var pose = new BuildingPose(10, 20, 90);
            var corners = footprint.Corners(pose);
            Assert.That(corners[0].X, Is.EqualTo(10).Within(.001));
            Assert.That(corners[0].Z, Is.EqualTo(20).Within(.001));
            Assert.That(corners[2].X, Is.EqualTo(12).Within(.001));
            Assert.That(corners[2].Z, Is.EqualTo(14).Within(.001));
            Assert.That(PlacementRect.Overlaps(footprint, pose, new PlacementRect(11, 17, 1, 1), default), Is.True);
            Assert.That(PlacementRect.Overlaps(footprint, pose, new PlacementRect(16, 21, 1, 1), default), Is.False);
        }

        [Test]
        public void RotationGestureSnapsTapTurnsToLotOrientationAndAllowsArbitraryYaw()
        {
            var gesture = new BuildingRotationGesture();
            gesture.Begin(37f, 100f, false, 0f);
            Assert.That(gesture.End(102f, .1f), Is.EqualTo(90f).Within(.001f));
            gesture.Begin(90f, 100f, false, 1f);
            Assert.That(gesture.End(100f, 1.1f), Is.EqualTo(180f).Within(.001f));
            gesture.Begin(90.005f, 100f, false, 1.5f);
            Assert.That(gesture.End(100f, 1.6f), Is.EqualTo(180f).Within(.001f));
            gesture.Begin(103f, 100f, false, 2f);
            Assert.That(gesture.End(100f, 2.1f), Is.EqualTo(180f).Within(.001f));
            gesture.Begin(358f, 100f, false, 3f);
            Assert.That(gesture.End(100f, 3.1f), Is.EqualTo(0f).Within(.001f));
            gesture.Begin(37f, 100f, true, 4f);
            Assert.That(gesture.End(100f, 4.1f), Is.EqualTo(0f).Within(.001f));
            gesture.Begin(103f, 100f, true, 5f);
            Assert.That(gesture.End(100f, 5.1f), Is.EqualTo(90f).Within(.001f));
            gesture.Begin(5f, 100f, true, 6f);
            Assert.That(gesture.End(100f, 6.1f), Is.EqualTo(0f).Within(.001f));
            gesture.Begin(358f, 100f, true, 7f);
            Assert.That(gesture.End(100f, 7.1f), Is.EqualTo(270f).Within(.001f));
            gesture.Begin(60f, 100f, false, 8f, 23f);
            Assert.That(gesture.End(100f, 8.1f), Is.EqualTo(113f).Within(.001f));
            gesture.Begin(60f, 100f, true, 9f, 23f);
            Assert.That(gesture.End(100f, 9.1f), Is.EqualTo(23f).Within(.001f));
            gesture.Begin(37f, 100f, false, 2f);
            Assert.That(gesture.Update(115f, 2.25f), Is.EqualTo(42.25f).Within(.001f));
            Assert.That(gesture.IsFreeRotating, Is.True);
            Assert.That(gesture.End(115f, 2.3f), Is.EqualTo(42.25f).Within(.001f));
        }

        [Test]
        public void RotationMagnetOnlyAssistsNearFifteenDegreeAngles()
        {
            var gesture = new BuildingRotationGesture();
            gesture.Begin(0f, 0f, false, 0f);
            Assert.That(gesture.Update(40f, .25f), Is.EqualTo(15f).Within(.001f), "14 degrees should magnetize to 15.");
            gesture.Begin(0f, 0f, false, 1f);
            Assert.That(gesture.Update(60f, 1.25f), Is.EqualTo(21f).Within(.001f), "Angles outside the two-degree range stay free.");
        }
    }
}
