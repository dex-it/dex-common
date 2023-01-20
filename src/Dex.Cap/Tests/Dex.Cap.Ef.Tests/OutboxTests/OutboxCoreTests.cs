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
            var command = new TestOutboxCommand {Args = "hello"};
            var commandText = serializer.Serialize(command);
            var typeName = typeof(TestOutboxCommand).AssemblyQualifiedName ?? throw new InvalidOperationException();
            var cmd = (TestOutboxCommand) serializer.Deserialize(Type.GetType(typeName, true)!, commandText)!;
            
            Assert.IsNotNull(cmd);
            Assert.AreEqual(command.Args, cmd.Args);
        }
    }
}