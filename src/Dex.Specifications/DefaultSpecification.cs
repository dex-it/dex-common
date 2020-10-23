namespace Dex.Specifications
{
    public class DefaultSpecification<T> : Specification<T>
    {
        public DefaultSpecification() : base(e => true)
        {
            
        }
    }
}