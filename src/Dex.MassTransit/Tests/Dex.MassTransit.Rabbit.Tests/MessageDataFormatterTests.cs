using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Dex.MassTransit.Rabbit.Tests;

/// <summary>
/// Подготовка тела сообщения для лога в <see cref="MessageDataFormatter"/>.
/// </summary>
/// <remarks>
/// Размер тела ничем не ограничен сверху, поэтому проверяется не только результат, но и то, что
/// обход графа прекращается на лимите: иначе на большом сообщении обработчик ошибки платит полной
/// сериализацией на каждый ретрай.
/// </remarks>
[TestFixture]
public class MessageDataFormatterTests
{
    [Test]
    public void Format_WhenBodyExceedsLimit_StopsWalkingTheGraph()
    {
        var message = new CountingMessage(itemCount: 5000);

        MessageDataFormatter.Format(message, limit: 1000);

        Assert.That(message.Visited, Is.GreaterThan(0));
        Assert.That(message.Visited, Is.LessThan(message.Items.Length / 2));
    }

    [Test]
    public void Format_WhenBodyFitsLimit_WalksWholeGraph()
    {
        var message = new CountingMessage(itemCount: 3);

        var messageData = MessageDataFormatter.Format(message, limit: 4000);

        Assert.That(message.Visited, Is.EqualTo(3));
        Assert.That(messageData, Does.Not.EndWith("..."));
    }

    [Test]
    public void Format_LimitCountsBytesNotChars()
    {
        // кириллица занимает два байта на символ
        var message = new { Name = new string('я', 100) };

        var messageData = MessageDataFormatter.Format(message, limit: 40);

        Assert.That(messageData, Does.EndWith("..."));
        Assert.That(Encoding.UTF8.GetByteCount(messageData[..^3]), Is.LessThanOrEqualTo(40));
    }

    [Test]
    public void Format_WhenLimitIsNotPositive_ReturnsOnlyTruncationMark()
    {
        Assert.That(MessageDataFormatter.Format(new { Name = "abc" }, limit: 0), Is.EqualTo("..."));
        Assert.That(MessageDataFormatter.Format(new { Name = "abc" }, limit: -1), Is.EqualTo("..."));
    }

    [Test]
    public void Format_WhenMessageIsNull_ReturnsJsonNull()
    {
        Assert.That(MessageDataFormatter.Format<object>(null, limit: 4000), Is.EqualTo("null"));
    }

    /// <summary>
    /// Сообщение, считающее обращения сериализатора к элементам тела.
    /// </summary>
    public sealed class CountingMessage
    {
        public CountingMessage(int itemCount) => Items = Enumerable.Range(0, itemCount).Select(_ => new Item(this)).ToArray();

        public Item[] Items { get; }

        internal int Visited { get; private set; }

        private void Visit() => Visited++;

        public sealed class Item(CountingMessage owner)
        {
            public string Name
            {
                get
                {
                    owner.Visit();

                    return "значение";
                }
            }
        }
    }
}