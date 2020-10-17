using System;
using System.Linq;
using System.Linq.Expressions;
using Dex.Specifications.Expressions;

namespace Dex.Specifications
{
    /// <summary>
    /// Объединение спецификаций по ИЛИ
    /// </summary>
    /// <typeparam name="T">Тип объекта, для которого применяется спецификация</typeparam>
    class OrSpecification<T> : CompositeSpecification<T>
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        public OrSpecification()
        {
        }

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="specifications">Список спецификаций</param>
        public OrSpecification(params Specification<T>[] specifications)
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
                return Specifications.Skip(1).Aggregate(result, (current, specification) => current.Or((Expression<Func<T, bool>>)specification));
            }
        }
    }
}