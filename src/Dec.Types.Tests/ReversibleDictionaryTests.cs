using System;
using Dex.Types;
using NUnit.Framework;

namespace Dec.Types.Test
{
    [TestFixture]
    public class ReversibleDictionaryTests
    {
        [Test]
        public void Indexer_AddNewKeyValuePair_ShouldAddKeyValuePair()
        {
            // Arrange
            var reversibleDictionary = new ReversibleDictionary<int, string>();

            // Act
            reversibleDictionary[1] = "One";
            Assert.Multiple(() =>
            {
                // Assert
                Assert.That(reversibleDictionary[1], Is.EqualTo("One"));
                Assert.That(reversibleDictionary.TryGetKey("One", out var key) ? key : -1, Is.EqualTo(1));
            });
        }

        [Test]
        public void Indexer_UpdateExistingValue_ShouldUpdateValue()
        {
            // Arrange
            var reversibleDictionary = new ReversibleDictionary<int, string>
            {
                { 1, "One" }
            };

            // Act
            reversibleDictionary[1] = "UpdatedOne";

            // Assert
            Assert.That(reversibleDictionary[1], Is.EqualTo("UpdatedOne"));
        }
        
        [Test]
        public void Indexer_UpdateDuplicateValue_ShouldUpdateValue()
        {
            // Arrange
            var reversibleDictionary = new ReversibleDictionary<int, string>
            {
                { 1, "One" },
                { 2, "Two" }
            };

            // Assert
            Assert.Throws<ArgumentException>(() => reversibleDictionary[2] = "One");
        }

        [Test]
        public void Indexer_AddDuplicateValue_ShouldThrowException()
        {
            // Arrange
            var reversibleDictionary = new ReversibleDictionary<int, string>
            {
                { 1, "One" },
                { 2, "Two" }
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => reversibleDictionary[3] = "Two");
        }

        [Test]
        public void Add_WhenKeyAndValueDoNotExist_ShouldAddKeyValuePair()
        {
            // Arrange
            var reversibleDictionary = new ReversibleDictionary<int, string>();

            // Act
            reversibleDictionary.Add(1, "One");
            Assert.Multiple(() =>
            {

                // Assert
                Assert.That(reversibleDictionary[1], Is.EqualTo("One"));
                Assert.That(reversibleDictionary.TryGetKey("One", out var key) ? key : -1, Is.EqualTo(1));
            });
        }

        [Test]
        public void Add_WhenKeyOrValueAlreadyExist_ShouldThrowException()
        {
            // Arrange
            var reversibleDictionary = new ReversibleDictionary<int, string>();
            reversibleDictionary.Add(1, "One");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => reversibleDictionary.Add(1, "AnotherOne"));
            Assert.Throws<ArgumentException>(() => reversibleDictionary.Add(2, "One"));
        }

        [Test]
        public void Remove_WhenKeyExists_ShouldRemoveKeyValuePair()
        {
            // Arrange
            var reversibleDictionary = new ReversibleDictionary<int, string>
            {
                { 1, "One" },
                { 2, "Two" }
            };

            // Act
            var result = reversibleDictionary.Remove(1);
            Assert.Multiple(() =>
            {
                // Assert
                Assert.That(result, Is.True);
                Assert.That(reversibleDictionary.ContainsKey(1), Is.False);
                Assert.That(reversibleDictionary.ContainsValue("One"), Is.False);
            });
        }

        [Test]
        public void Remove_WhenKeyDoesNotExist_ShouldReturnFalse()
        {
            // Arrange
            var reversibleDictionary = new ReversibleDictionary<int, string>
            {
                { 1, "One" }
            };

            // Act
            var result = reversibleDictionary.Remove(2);

            // Assert
            Assert.That(result, Is.False);
        }
    }
}