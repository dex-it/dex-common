using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Dex.Specifications.EntityFramework;

namespace Dex.Specifications.TestProject
{
    public class Sp<T>
    {
        private readonly Specification<T> _specification;

        public Sp()
        {
            _specification = new DefaultSpecification<T>();
        }
        
        private Sp(Specification<T> specification)
        {
            _specification = specification ?? throw new ArgumentNullException(nameof(specification));
        }
        
        public Sp<T> Like(Expression<Func<T, string>> expression, string pattern)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            if (string.IsNullOrWhiteSpace(pattern)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(pattern));

            return new Sp<T>(_specification & new CaseSensitiveLikeSpecification<T>(expression, pattern));
        }

        public Sp<T> Equal<TProperty>(Expression<Func<T, TProperty>> expression, TProperty property)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            if (property == null) throw new ArgumentNullException(nameof(property));

            return new Sp<T>(_specification & new EqualSpecification<T, TProperty>(expression, property));
        }

        public Sp<T> In<TProperty>(Expression<Func<T, TProperty>> expression, IEnumerable<TProperty> elements)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            if (elements == null) throw new ArgumentNullException(nameof(elements));

            return new Sp<T>(_specification & new InSpecification<T, TProperty>(expression, elements));
        }

        public Sp<T> And(Func<Sp<T>, Sp<T>> builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            var sp = builder(new Sp<T>())._specification;
            return new Sp<T>(And(sp));
        }

        public Sp<T> Or(Func<Sp<T>, Sp<T>> builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            var sp = builder(new Sp<T>())._specification;
            return new Sp<T>(Or(sp));
        }
        
        private Specification<T> And(Specification<T> specification)
        {
            return _specification & specification;
        }

        private Specification<T> Or(Specification<T> specification)
        {
            return _specification | specification;
        }
        
        public static implicit operator Expression<Func<T, bool>>(Sp<T> sp)
        {
            return sp._specification;
        }
    }
}