using System;
using System.Linq.Expressions;
using Dex.Specifications.Expressions;

namespace Dex.Specifications
{
    /// <summary>
    /// Инверсия спецификации
    /// </summary>
    /// <typeparam name="T">Тип объекта, для которого применяется спецификация</typeparam>
    class NotSpecification<T> : Specification<T>
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="specification">Спецификация</param>
        /// <exception cref="ArgumentNullException"/>
        public NotSpecification(Specification<T> specification)
        {
            if (specification == null)
            {
                throw new ArgumentNullException();
            }

            _specification = specification;
        }


        private readonly Specification<T> _specification;

        /// <summary>
        /// Спецификация
        /// </summary>
        public Specification<T> Specification
        {
            get { return _specification; }
        }


        /// <summary>
        /// Предикат для проверки спецификации
        /// </summary>
        protected override Expression<Func<T, bool>> Predicate
        {
            get
            {
                return ((Expression<Func<T, bool>>)_specification).Not();
            }
        }
    }
}