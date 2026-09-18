using NUnit.Framework;
using SilverScreen.Domain;

namespace SilverScreen.Tests.EditMode
{
    public sealed class StudioIdentityTests
    {
        [Test]
        public void UsesDefaultStudioName()
        {
            var identity = new StudioIdentity();

            Assert.That(identity.Name, Is.EqualTo(StudioIdentity.DefaultName));
        }

        [Test]
        public void AcceptsValidRename()
        {
            var identity = new StudioIdentity();

            bool renamed = identity.TryRename("Sunset Gate Pictures");

            Assert.That(renamed, Is.True);
            Assert.That(identity.Name, Is.EqualTo("Sunset Gate Pictures"));
        }

        [Test]
        public void TrimsRenameWhitespace()
        {
            var identity = new StudioIdentity();

            bool renamed = identity.TryRename("  Golden Era Films  ");

            Assert.That(renamed, Is.True);
            Assert.That(identity.Name, Is.EqualTo("Golden Era Films"));
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase(null)]
        public void RejectsEmptyRename(string requestedName)
        {
            var identity = new StudioIdentity();

            bool renamed = identity.TryRename(requestedName);

            Assert.That(renamed, Is.False);
            Assert.That(identity.Name, Is.EqualTo(StudioIdentity.DefaultName));
        }

        [Test]
        public void AcceptsMaximumLengthAndRejectsLongerName()
        {
            var identity = new StudioIdentity();
            string maximum = new string('A', StudioIdentity.MaximumNameLength);

            Assert.That(identity.TryRename(maximum), Is.True);
            Assert.That(identity.Name, Is.EqualTo(maximum));
            Assert.That(identity.TryRename(maximum + "B"), Is.False);
            Assert.That(identity.Name, Is.EqualTo(maximum));
        }
    }
}
