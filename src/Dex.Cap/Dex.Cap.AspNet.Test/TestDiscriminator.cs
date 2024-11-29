using Dex.Cap.Outbox;

namespace Dex.Cap.AspNet.Test
{
    internal class TestDiscriminator : BaseOutboxTypeDiscriminator
    {
        public TestDiscriminator()
        {
            Add<TestOutboxCommand>("15CAD1F5-4C0D-4816-B5D1-E2340144C4AA");
        }
    }
}