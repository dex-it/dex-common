using Dex.Cap.Outbox;

namespace Dex.Cap.AspNet.Test
{
    internal class TestDiscriminator : BaseOutboxTypeDiscriminator
    {
        public TestDiscriminator()
        {
            Add<TestOutboxCommand>(nameof(TestOutboxCommand));
        }
    }
}