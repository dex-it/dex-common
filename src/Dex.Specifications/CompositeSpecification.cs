using System.Collections.Generic;

namespace Dex.Specifications
{
    /// <summary>
    /// Композитная спецификация
    /// </summary>
    /// <typeparam name="T">Тип объекта, для которого применяется спецификация</typeparam>
    abstract class CompositeSpecification<T> : Specification<T>
    {
        /// <summary>
        /// Список спецификаций
        /// </summary>
        protected readonly List<Specification<T>> Specifications;


        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="specifications">Список спецификаций</param>
        protected CompositeSpecification(params Specification<T>[] specifications)
        {
            Specifications = new List<Specification<T>>(specifications);
        }


        /// <summary>
        /// Добавить спецификацию
        /// </summary>
        /// <param name="specification">Спецификация</param>
        public void Add(Specification<T> specification)
        {
            Specifications.Add(specification);
        }


        /// <summary>
        /// Удалить спецификацию
        /// </summary>
        /// <param name="specification">Спецификация</param>
        public void Remove(Specification<T> specification)
        {
            Specifications.Remove(specification);
        }
    }
}