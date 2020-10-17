using System;
using System.Linq.Expressions;
using Dex.Specifications.Filters;

namespace Dex.Specifications
{
    /// <summary>
    /// Спецификация на основе настраиваемого условного фильтра
    /// </summary>
    /// <typeparam name="T">Тип объекта, для которого применяется спецификация</typeparam>
    public class FilterSpecification<T> : Specification<T>
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="filterOperatorFunc">Выражение для построения оператора фильтра</param>
        public FilterSpecification(Func<IFilterBuilder<T>, IFilterOperator> filterOperatorFunc)
        {
            _filterOperatorFunc = filterOperatorFunc;
        }


        private readonly Func<IFilterBuilder<T>, IFilterOperator> _filterOperatorFunc;

        /// <summary>
        /// Оператор фильтра
        /// </summary>
        public IFilterOperator Operator
        {
            get
            {
                return _filterOperatorFunc(new FilterBuilder<T>());
            }
        }


        /// <summary>
        /// Предикат для проверки спецификации
        /// </summary>
        protected override Expression<Func<T, bool>> Predicate
        {
            get
            {
                return (Expression<Func<T, bool>>)Operator.CreateFilter();
            }
        }
    }
}