using System;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace Dex.Specifications.Filters
{
    /// <summary>
    /// Условный оператор
    /// </summary>
    [DataContract]
    public abstract class ConditionalOperator : FilterOperator
    {
        /// <summary>
        /// Свойство для фильтрации
        /// </summary>
        [DataMember]
        public string Property { get; private set; }


        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="typeName">Тип</param>
        /// <param name="propertyName">Свойство</param>
        /// <exception cref="ArgumentNullException">Не задано имя типа или свойства</exception>
        /// <exception cref="ArgumentException">Не найдено определение свойства</exception>
        protected ConditionalOperator(string typeName, string propertyName)
            : base(typeName)
        {

            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentNullException();
            }

            Property = propertyName;
        }

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="typeName">Тип</param>
        /// <param name="propertyExpression">Лямбда-выражение доступа к свойству</param>
        protected ConditionalOperator(string typeName, LambdaExpression propertyExpression)
            : this(typeName, propertyExpression.Body.ToString())
        {
        }


        /// <summary>
        /// Создать выражение фильтрации
        /// </summary>
        public override LambdaExpression CreateFilter()
        {
            var param = CreateFilterParameter();
            var property = CreateFilterProperty(param);

            var expression = CreateFilter(property);

            if (expression == null || expression.Type != typeof(bool))
            {
                throw new InvalidOperationException();
            }

            return Expression.Lambda(CreateFilterDelegateType(), expression, param);
        }

        /// <summary>
        /// Создать выражение фильтрации
        /// </summary>
        /// <param name="property">Выражение для фильтруемого свойства</param>
        protected abstract Expression CreateFilter(Expression property);


        /// <summary>
        /// Создать выражение для фильтруемого свойства
        /// </summary>
        /// <param name="parameter">Параметр лямбда-выражения</param>
        private Expression CreateFilterProperty(ParameterExpression parameter)
        {
            Expression result = parameter;

            var properties = Property.Split('.');

            if (properties.Length > 1)
            {
                properties = properties.Skip(1).ToArray();
            }

            var methodTemplate = new Regex(@"(?<method>.*)\(\)");

            foreach (var term in properties)
            {
                var methodMatch = methodTemplate.Match(term);

                result = methodMatch.Success
                                ? Expression.Call(result, methodMatch.Groups["method"].Value, new Type[] { })
                                : (Expression)Expression.PropertyOrField(result, term);
            }

            return result;
        }


        /// <summary>
        /// Создать постоянное значение заданного типа
        /// </summary>
        /// <param name="target">Результирующий тип</param>
        /// <param name="value">Значение</param>
        protected Expression CreateConstant(Type target, object value)
        {
            Expression result = Expression.Constant(value);

            // Если требуется явное приведение к типу
            if (value != null && target != value.GetType())
            {
                result = Expression.Convert(result, target);
            }

            return result;
        }
    }
}