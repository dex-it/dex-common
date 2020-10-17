using System;
using System.Linq.Expressions;

namespace Dex.Specifications
{
    /// <summary>
    /// Пустая спецификация
    /// </summary>
    /// <typeparam name="T">Тип объекта, для которого применяется спецификация</typeparam>
    public class NullSpecification<T> : Specification<T>
    {
        /// <summary>
        /// Предикат для проверки спецификации
        /// </summary>
        protected override Expression<Func<T, bool>> Predicate
        {
            get
            {
                return (item => true);
            }
        }
    }
}