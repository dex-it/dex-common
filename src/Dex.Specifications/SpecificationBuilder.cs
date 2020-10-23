namespace Dex.Specifications
{
    public class SpecificationBuilder<T>
    {
        private Specification<T> _specification;
        
        public Specification<T> And(Specification<T> specification)
        {
            return new AndSpecification<T>(_specification, specification);
        }

        public Specification<T> Or(Specification<T> specification)
        {
            return new OrSpecification<T>(_specification, specification);
        }

        public Specification<T> Build()
        {
            return _specification;
        }
    }
}