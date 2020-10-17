using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Dex.Specifications.Expressions;

namespace Dex.Specifications
{
    /// <summary>
    /// Базовый класс спецификации
    /// </summary>
    /// <typeparam name="T">Тип объекта, для которого применяется спецификация</typeparam>
    public class Specification<T>
    {
        protected Specification()
        {
        }

        public Specification(Expression<Func<T, bool>> predicate)
        {
            _predicate = predicate;
        }

        public bool IsSatisfiedBy(T item)
        {
            return CompiledPredicate(item);
        }

        private Func<T, bool> _compiledPredicate;
        private Func<T, bool> CompiledPredicate => _compiledPredicate ?? (_compiledPredicate = _predicate.Compile());

        private Expression<Func<T, bool>> _predicate;

        protected virtual Expression<Func<T, bool>> Predicate
        {
            get { return _predicate; }
            set
            {
                _predicate = value;
                _compiledPredicate = null;
            }
        }

        public Specification<TO> In<TO>(Expression<Func<TO, T>> selector)
        {
            var parametersMap = new Dictionary<string, Expression> { { Predicate.Parameters[0].Name, selector.Body } };

            var creator = Expression.Lambda<Func<TO, bool>>(Predicate.Body, selector.Parameters);
            var result = ParameterExpressionToPropertyRewriter.ReplaceParameters(parametersMap, creator);
            return new Specification<TO>((Expression<Func<TO, bool>>)result);
        }

        public static Specification<T> operator !(Specification<T> specification)
        {
            return new NotSpecification<T>(specification);
        }

        public static Specification<T> operator |(Specification<T> left, Specification<T> right)
        {
            return new OrSpecification<T>(left, right);
        }

        public static Specification<T> operator &(Specification<T> left, Specification<T> right)
        {
            return new AndSpecification<T>(left, right);
        }

        public static implicit operator Predicate<T>(Specification<T> specification)
        {
            return specification.IsSatisfiedBy;
        }

        public static implicit operator Func<T, bool>(Specification<T> specification)
        {
            return specification.IsSatisfiedBy;
        }

        public static implicit operator Expression<Func<T, bool>>(Specification<T> specification)
        {
            return specification.Predicate;
        }
    }
}