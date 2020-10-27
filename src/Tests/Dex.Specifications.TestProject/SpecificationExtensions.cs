using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Dex.Specifications.EntityFramework;

namespace Dex.Specifications.TestProject
{
    public static class SpecificationExtensions
    {
        public static Specification<T> Like<T>(this Specification<T> current, Expression<Func<T, string>> expression, string pattern)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            if (string.IsNullOrWhiteSpace(pattern)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(pattern));

            return current & new CaseSensitiveLikeSpecification<T>(expression, pattern);
        }

        public static Specification<T> Equal<T, TProperty>(this Specification<T> current, Expression<Func<T, TProperty>> expression, TProperty property)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            if (property == null) throw new ArgumentNullException(nameof(property));

            return current & new EqualSpecification<T, TProperty>(expression, property);
        }

        public static Specification<T> In<T, TProperty>(this Specification<T> current, Expression<Func<T, TProperty>> expression, IEnumerable<TProperty> elements)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            if (elements == null) throw new ArgumentNullException(nameof(elements));

            return current & new InSpecification<T, TProperty>(expression, elements);
        }

        public static Specification<T> And<T>(this Specification<T> current, Func<Specification<T>, Specification<T>> func)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));

            return func(current) & new Specification<T>(e => true);
        }

        public static Specification<T> Or<T>(this Specification<T> current, Func<Specification<T>, Specification<T>> func)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));

            return func(current) | new Specification<T>(e => true);
        }
        
        public static Specification<T> And<T>(this Specification<T> current, Specification<T> specification)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (specification == null) throw new ArgumentNullException(nameof(specification));

            return current & specification;
        }

        public static Specification<T> Or<T>(this Specification<T> current, Specification<T> specification)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (specification == null) throw new ArgumentNullException(nameof(specification));

            return current | specification;
        }
    }
}