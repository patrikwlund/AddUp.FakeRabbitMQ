using System.Linq;
using NUnit.Framework;

namespace fake_rabbit.tests
{
    [TestFixture]
    public class FakeConnectionTests
    {
        [Test]
        public void CreateModel_CreatesANewModel()
        {
            // Arrange
            var connection = new FakeConnection();

            // Act
            var result = connection.CreateModel();

            // Assert
            Assert.That(connection.Models,Has.Count.EqualTo(1));
            Assert.That(connection.Models,Is.EquivalentTo(new[]{result}));
        }

        [Test]
        public void CreateModel_MultipleTimes_CreatesManyModels()
        {
            // Arrange
            var connection = new FakeConnection();

            // Act
            var result1 = connection.CreateModel();
            var result2 = connection.CreateModel();

            // Assert
            Assert.That(connection.Models, Has.Count.EqualTo(2));
            Assert.That(connection.Models, Is.EquivalentTo(new[] { result1, result2 }));
        }

        [Test]
        public void Close_NoArguments_ClosesTheConnection()
        {
            // Arrange
            var connection = new FakeConnection();

            // Act
            connection.Close();

            // Assert
            Assert.That(connection.IsOpen,Is.False);
            Assert.That(connection.CloseReason, Is.Not.Null);
        }

        [Test]
        public void Close_TimeoutArguments_ClosesTheConnection()
        {
            // Arrange
            var connection = new FakeConnection();

            // Act
            connection.Close(timeout:2);

            // Assert
            Assert.That(connection.IsOpen, Is.False);
            Assert.That(connection.CloseReason, Is.Not.Null);
        }

        [Test]
        public void Close_ReasonArguments_ClosesTheConnection()
        {
            // Arrange
            var connection = new FakeConnection();

            // Act
            connection.Close(reasonCode:3,reasonText:"foo");

            // Assert
            Assert.That(connection.IsOpen, Is.False);
            Assert.That(connection.CloseReason.ReplyCode, Is.EqualTo(3));
            Assert.That(connection.CloseReason.ReplyText, Is.EqualTo("foo"));
        }

        [Test]
        public void Close_AllArguments_ClosesTheConnection()
        {
            // Arrange
            var connection = new FakeConnection();

            // Act
            connection.Close(reasonCode: 3, reasonText: "foo",timeout:4);

            // Assert
            Assert.That(connection.IsOpen, Is.False);
            Assert.That(connection.CloseReason.ReplyCode, Is.EqualTo(3));
            Assert.That(connection.CloseReason.ReplyText, Is.EqualTo("foo"));
        }

        [Test]
        public void Close_ClosesAllModels()
        {
            // Arrange
            var connection = new FakeConnection();
            connection.CreateModel();

            // Act
            connection.Close();

            // Assert
            Assert.IsTrue(connection.Models.All(m=>m.IsOpen == false));
            Assert.IsTrue(connection.Models.All(m=>m.IsClosed == true));
        }

        [Test]
        public void Abort_NoArguments_AbortsTheConnection()
        {
            // Arrange
            var connection = new FakeConnection();

            // Act
            connection.Close();

            // Assert
            Assert.That(connection.IsOpen, Is.False);
            Assert.That(connection.CloseReason, Is.Not.Null);
        }

        [Test]
        public void Abort_TimeoutArguments_AbortsTheConnection()
        {
            // Arrange
            var connection = new FakeConnection();

            // Act
            connection.Abort(timeout: 2);

            // Assert
            Assert.That(connection.IsOpen, Is.False);
            Assert.That(connection.CloseReason, Is.Not.Null);
        }

        [Test]
        public void Abort_ReasonArguments_AbortsTheConnection()
        {
            // Arrange
            var connection = new FakeConnection();

            // Act
            connection.Abort(reasonCode: 3, reasonText: "foo");

            // Assert
            Assert.That(connection.IsOpen, Is.False);
            Assert.That(connection.CloseReason.ReplyCode, Is.EqualTo(3));
            Assert.That(connection.CloseReason.ReplyText, Is.EqualTo("foo"));
        }

        [Test]
        public void Abort_AllArguments_AbortsTheConnection()
        {
            // Arrange
            var connection = new FakeConnection();

            // Act
            connection.Abort(reasonCode: 3, reasonText: "foo", timeout: 4);

            // Assert
            Assert.That(connection.IsOpen, Is.False);
            Assert.That(connection.CloseReason.ReplyCode, Is.EqualTo(3));
            Assert.That(connection.CloseReason.ReplyText, Is.EqualTo("foo"));
        }

        [Test]
        public void Abort_AbortsAllModels()
        {
            // Arrange
            var connection = new FakeConnection();
            connection.CreateModel();

            // Act
            connection.Abort();

            // Assert
            Assert.IsTrue(connection.Models.All(m => m.IsOpen == false));
            Assert.IsTrue(connection.Models.All(m => m.IsClosed == true));
        }
    }
}