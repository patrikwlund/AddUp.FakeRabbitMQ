using NUnit.Framework;

namespace fake_rabbit.tests
{
    [TestFixture]
    public class FakeModelTests
    {
        [Test]
        public void CreateBasicProperties_ReturnsBasicProperties()
        {
            // Arrange
            var model = new FakeModel();

            // Act
            var result = model.CreateBasicProperties();

            // Assert
            Assert.That(result,Is.Not.Null);
        }

        [Test]
        public void CreateFileProperties_ReturnsFileProperties()
        {
            // Arrange
            var model = new FakeModel();

            // Act
            var result = model.CreateFileProperties();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void CreateStreamProperties_ReturnsStreamProperties()
        {
            // Arrange
            var model = new FakeModel();

            // Act
            var result = model.CreateStreamProperties();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void ChannelFlow_SetsIfTheChannelIsActive(bool value)
        {
            // Arrange
            var model = new FakeModel();

            // Act
            model.ChannelFlow(value);

            // Assert
            Assert.That(model.IsChannelFlowActive,Is.EqualTo(value));
        }
    }
}