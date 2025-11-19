using System;
using NUnit.Framework;

namespace Dex.Types.Test
{
    [TestFixture]
    public class UniqueValueDictionaryTests
    {
        [Test]
        public void Indexer_AddNewKeyValuePair_ShouldAddKeyValuePair()
        {
            // Arrange
            var dict = new UniqueValueDictionary<int, string>();

            // Act
            dict[1] = "One";
            Assert.Multiple(() =>
            {
                // Assert
                Assert.That(dict[1], Is.EqualTo("One"));
                Assert.That(dict.TryGetKey("One", out var key) ? key : -1, Is.EqualTo(1));
            });
        }

        [Test]
        public void Indexer_UpdateExistingValue_ShouldUpdateValue()
        {
            // Arrange
            var dict = new UniqueValueDictionary<int, string>
            {
                { 1, "One" }
            };

            // Act
            dict[1] = "UpdatedOne";

            // Assert
            Assert.That(dict[1], Is.EqualTo("UpdatedOne"));
        }

        [Test]
        public void Indexer_UpdateDuplicateValue_ShouldUpdateValue()
        {
            // Arrange
            var dict = new UniqueValueDictionary<int, string>
            {
                { 1, "One" },
                { 2, "Two" }
            };
            if (dict == null) throw new ArgumentNullException(nameof(dict));

            // Assert
            Assert.Throws<ArgumentException>(() => dict[2] = "One");
        }

        [Test]
        public void Indexer_UpdateDuplicateValue_ShouldUpdateValue2()
        {
            // Arrange
            var dict = new UniqueValueDictionary<int, string>
            {
                { 1, "One" },
                { 2, "Two" }
            };

            // act
            dict[1] = "R";

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(dict.ContainsValue("One"), Is.False);
                Assert.That(dict.ContainsValue("Two"), Is.True);
                Assert.That(dict.ContainsValue("R"), Is.True);
            });
        }

        [Test]
        public void Indexer_AddDuplicateValue_ShouldThrowException()
        {
            // Arrange
            var dict = new UniqueValueDictionary<int, string>
            {
                { 1, "One" },
                { 2, "Two" }
            };
            if (dict == null) throw new ArgumentNullException(nameof(dict));

            // Act & Assert
            Assert.Throws<ArgumentException>(() => dict[3] = "Two");
        }

        [Test]
        public void Add_WhenKeyAndValueDoNotExist_ShouldAddKeyValuePair()
        {
            // Arrange & Act
            var dict = new UniqueValueDictionary<int, string> { { 1, "One" } };

            Assert.Multiple(() =>
            {
                // Assert
                Assert.That(dict[1], Is.EqualTo("One"));
                Assert.That(dict.TryGetKey("One", out var key) ? key : -1, Is.EqualTo(1));
            });
        }

        [Test]
        public void Add_WhenKeyOrValueAlreadyExist_ShouldThrowException()
        {
            // Arrange
            var dict = new UniqueValueDictionary<int, string> { { 1, "One" } };
            if (dict == null) throw new ArgumentNullException(nameof(dict));

            // Act & Assert
            Assert.Throws<ArgumentException>(() => dict.Add(1, "AnotherOne"));
            Assert.Throws<ArgumentException>(() => dict.Add(2, "One"));
        }

        [Test]
        public void Initialize_WithDuplicate_ShouldThrowException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
            {
                _ = new UniqueValueDictionary<int, string>
                {
                    { 1, "One" },
                    { 2, "One" },
                };
            });

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
            {
                _ = new UniqueValueDictionary<int, string>
                {
                    { 1, "One" },
                    // ReSharper disable once DuplicateKeyCollectionInitialization
                    { 1, "Two" },
                };
            });
        }

        [Test]
        public void Remove_WhenKeyExists_ShouldRemoveKeyValuePair()
        {
            // Arrange
            var dict = new UniqueValueDictionary<int, string>
            {
                { 1, "One" },
                { 2, "Two" }
            };

            // Act
            var result = dict.Remove(1);
            Assert.Multiple(() =>
            {
                // Assert
                Assert.That(result, Is.True);
                Assert.That(dict.ContainsKey(1), Is.False);
                Assert.That(dict.ContainsValue("One"), Is.False);
            });
        }

        [Test]
        public void Remove_WhenKeyDoesNotExist_ShouldReturnFalse()
        {
            // Arrange
            var dict = new UniqueValueDictionary<int, string>
            {
                { 1, "One" }
            };

            // Act
            var result = dict.Remove(2);

            // Assert
            Assert.That(result, Is.False);
        }
    }
}