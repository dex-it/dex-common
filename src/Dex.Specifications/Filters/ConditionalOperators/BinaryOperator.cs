using System.Linq.Expressions;
using System.Runtime.Serialization;

namespace Dex.Specifications.Filters.ConditionalOperators
{
    /// <summary>
    /// Бинарный оператор
    /// </summary>
    [DataContract]
    public abstract class BinaryOperator : ConditionalOperator
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="typeName">Тип</param>
        /// <param name="propertyName">Свойство</param>
        /// <param name="value">Значение фильтра</param>
        protected BinaryOperator(string typeName, string propertyName, object value)
            : base(typeName, propertyName)
        {

            InitializeValue(value);
        }

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="typeName">Тип</param>
        /// <param name="propertyExpression">Лямбда-выражение доступа к свойству</param>
        /// <param name="value">Значение фильтра</param>
        protected BinaryOperator(string typeName, LambdaExpression propertyExpression, object value)
            : base(typeName, propertyExpression)
        {

            InitializeValue(value);
        }

        private void InitializeValue(object value)
        {
            Value = value;
        }


        /// <summary>
        /// Значение фильтра
        /// </summary>
        [DataMember]
        public object Value { get; set; }
    }
}