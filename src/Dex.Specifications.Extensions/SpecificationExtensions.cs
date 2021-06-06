using System;

namespace Dex.Specifications.Extensions
{
    public static class SpecificationExtensions
    {
        public static Specification<T> And<T>(this Specification<T> current, Func<Specification<T>, Specification<T>> func)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (func == null) throw new ArgumentNullException(nameof(func));

            return current & func(new Specification<T>());
        }

        public static Specification<T> And<T>(this Specification<T> current, Specification<T> specification)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (specification == null) throw new ArgumentNullException(nameof(specification));

            return current & specification;
        }


        public static Specification<T> Or<T>(this Specification<T> current, Func<Specification<T>, Specification<T>> func)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (func == null) throw new ArgumentNullException(nameof(func));

            return current | func(new Specification<T>());
        }

        public static Specification<T> Or<T>(this Specification<T> current, Specification<T> specification)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (specification == null) throw new ArgumentNullException(nameof(specification));

            return current | specification;
        }


        public static Specification<T> Not<T>(this Specification<T> current, Func<Specification<T>, Specification<T>> func)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (func == null) throw new ArgumentNullException(nameof(func));

            return current & new NotSpecification<T>(func(new Specification<T>()));
        }

        public static Specification<T> Not<T>(this Specification<T> current, Specification<T> specification)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (specification == null) throw new ArgumentNullException(nameof(specification));

            return current & !specification;
        }
    }
}