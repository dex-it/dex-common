using System;
using System.Linq;
using System.Linq.Expressions;
using Dex.Specifications.Expressions;

namespace Dex.Specifications
{
    /// <summary>
    /// Объединение спецификаций по И
    /// </summary>
    /// <typeparam name="T">Тип объекта, для которого применяется спецификация</typeparam>
    class AndSpecification<T> : CompositeSpecification<T>
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        public AndSpecification()
        {
        }

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="specifications">Список спецификаций</param>
        public AndSpecification(params Specification<T>[] specifications)
            : base(specifications)
        {
        }


        /// <summary>
        /// Предикат для проверки спецификации
        /// </summary>
        protected override Expression<Func<T, bool>> Predicate
        {
            get
            {
                Expression<Func<T, bool>> result = Specifications.First();
                return Specifications.Skip(1).Aggregate(result, (current, specification) => current.And((Expression<Func<T, bool>>)specification));
            }
        }
    }
}