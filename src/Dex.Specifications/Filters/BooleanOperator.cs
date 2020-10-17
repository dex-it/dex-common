using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Dex.Specifications.Filters
{
    /// <summary>
    /// Логический оператор
    /// </summary>
    [DataContract]
    public abstract class BooleanOperator : FilterOperator
    {

        [DataMember]
        private List<IFilterOperator> _operators;

        /// <summary>
        /// Список операторов
        /// </summary>
        public IEnumerable<IFilterOperator> Operators
        {
            get
            {
                return _operators.AsReadOnly();
            }
        }


        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="typeName">Тип</param>
        /// <param name="operators">Список операторов</param>
        protected BooleanOperator(string typeName, params IFilterOperator[] operators)
            : base(typeName)
        {

            InitializeOperators(operators);
        }

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="typeName">Тип</param>
        /// <param name="operators">Список операторов</param>
        protected BooleanOperator(string typeName, IEnumerable<IFilterOperator> operators)
            : base(typeName)
        {

            InitializeOperators(operators);
        }


        /// <summary>
        /// Инициализировать список операторов
        /// </summary>
        /// <param name="operators">Список операторов</param>
        /// <exception cref="ArgumentException">Типы операторов не совпадают</exception>
        private void InitializeOperators(IEnumerable<IFilterOperator> operators)
        {
            if (operators != null)
            {
                operators = operators.ToArray();
                // Если типы операторов не совпадают
                if (operators.Any() && operators.Select(o => o.Type.ToLower()).Distinct().Count() > 1)
                {
                    throw new ArgumentException();
                }

                _operators = operators.ToList();
            }
            else
            {
                _operators = new List<IFilterOperator>();
            }
        }


        /// <summary>
        /// Добавить оператор
        /// </summary>
        /// <param name="filterOperator">Оператор</param>
        /// <exception cref="ArgumentNullException">Оператор не задан</exception>
        /// <exception cref="ArgumentException">Тип оператора не соответствует типу данного фильтра</exception>
        public void Add(IFilterOperator filterOperator)
        {
            if (filterOperator == null)
            {
                throw new ArgumentNullException();
            }

            if (filterOperator.Type != Type)
            {
                throw new ArgumentException();
            }

            _operators.Add(filterOperator);
        }

        /// <summary>
        /// Удалить оператор
        /// </summary>
        /// <param name="filterOperator">Оператор</param>
        /// <exception cref="ArgumentNullException">Оператор не задан</exception>
        public void Remove(IFilterOperator filterOperator)
        {
            if (filterOperator == null)
            {
                throw new ArgumentNullException();
            }

            _operators.Remove(filterOperator);
        }
    }
}