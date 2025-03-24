using System;
using Dex.Cap.Outbox;
using Dex.Outbox.Command.Test;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.OutboxTests
{
    public class OutboxCoreTests
    {
        [Test]
        public void SerializerTest()
        {
            var serializer = new DefaultOutboxSerializer();
            var messageId = Guid.Parse("391804C8-C326-4EA7-9494-DBCFE73ACECE");
            var command = new TestOutboxCommand {Args = "hello", TestId = messageId};
            var commandText = serializer.Serialize(command);
            var typeName = typeof(TestOutboxCommand).AssemblyQualifiedName ?? throw new InvalidOperationException();
            var cmd = (TestOutboxCommand) serializer.Deserialize(Type.GetType(typeName, true)!, commandText)!;
            
            Assert.IsNotNull(cmd);
            Assert.AreEqual(command.Args, cmd.Args);
            Assert.AreEqual(messageId, cmd.TestId);
        }
    }
}