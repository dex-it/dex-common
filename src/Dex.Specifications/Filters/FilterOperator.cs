using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using Dex.Specifications.Filters.BooleanOperators;

namespace Dex.Specifications.Filters
{
    /// <summary>
    /// Базовый класс для операторов фильтра
    /// </summary>
    [DataContract]
    public abstract class FilterOperator : IFilterOperator
    {
        /// <summary>
        /// Тип для фильтрации
        /// </summary>
        [DataMember]
        public string Type { get; private set; }


        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="typeName">Тип</param>
        protected FilterOperator(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            Type = typeName;
        }


        private Type _targetType;

        /// <summary>
        /// Информация о типе
        /// </summary>
        protected Type TargetType
        {
            get
            {
                return _targetType ?? (_targetType = System.Type.GetType(Type));
            }
        }


        /// <summary>
        /// Удовлетворяет ли объект условию фильтра
        /// </summary>
        /// <param name="item">Проверяемый объект</param>
        /// <exception cref="ArgumentException">Тип аргумента не соответствует типу фильтра</exception>
        public bool IsSatisfiedBy(object item)
        {
            if (item != null && TargetType != item.GetType() && TargetType.GetTypeInfo().IsAssignableFrom(item.GetType()))
            {
                throw new ArgumentException();
            }

            try
            {
                return (bool)CreateFilter().Compile().DynamicInvoke(item);
            }
            catch (TargetInvocationException error)
            {
                if (error.InnerException != null)
                {
                    throw error.InnerException;
                }

                throw;
            }
        }


        /// <summary>
        /// Создать выражение фильтрации
        /// </summary>
        public abstract LambdaExpression CreateFilter();


        /// <summary>
        /// Создать делегат для выражения фильтрации
        /// </summary>
        protected Type CreateFilterDelegateType()
        {
            return typeof(Func<,>).MakeGenericType(TargetType, typeof(bool));
        }

        /// <summary>
        /// Создать параметр для выражения фильтрации
        /// </summary>
        protected ParameterExpression CreateFilterParameter()
        {
            return Expression.Parameter(TargetType, "item");
        }


        /// <summary>
        /// Инверсия оператора
        /// </summary>
        /// <param name="filterOperator">Оператор</param>
        public static FilterOperator operator !(FilterOperator filterOperator)
        {
            if (filterOperator == null)
            {
                return null;
            }

            return new NotOperator(filterOperator.Type, filterOperator);
        }

        /// <summary>
        /// Логическое ИЛИ операторов
        /// </summary>
        /// <param name="left">Оператор слева</param>
        /// <param name="right">Оператор справа</param>
        /// <exception cref="ArgumentException">Типы операторов не совпадают</exception>
        public static FilterOperator operator |(FilterOperator left, FilterOperator right)
        {
            if (left.TargetType != right.TargetType)
            {
                throw new ArgumentException();
            }

            return new OrOperator(left.Type, left, right);
        }

        /// <summary>
        /// Логическое И операторов
        /// </summary>
        /// <param name="left">Оператор слева</param>
        /// <param name="right">Оператор справа</param>
        /// <exception cref="ArgumentException">Типы операторов не совпадают</exception>
        public static FilterOperator operator &(FilterOperator left, FilterOperator right)
        {
            if (left.TargetType != right.TargetType)
            {
                throw new ArgumentException();
            }

            return new AndOperator(left.Type, left, right);
        }
    }
}