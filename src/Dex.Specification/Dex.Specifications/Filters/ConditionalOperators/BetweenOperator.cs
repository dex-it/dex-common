using System;
using System.Linq.Expressions;
using System.Runtime.Serialization;

namespace Dex.Specifications.Filters.ConditionalOperators
{
    /// <summary>
    /// Оператор "МЕЖДУ"
    /// </summary>
    [DataContract]
    public class BetweenOperator : ConditionalOperator
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="typeName">Тип</param>
        /// <param name="propertyName">Свойство</param>
        /// <param name="beginValue">Начальное значение</param>
        /// <param name="endValue">Конечное значение</param>
        public BetweenOperator(string typeName, string propertyName, object beginValue, object endValue)
            : base(typeName, propertyName)
        {
            InitializeValues(beginValue, endValue);
        }

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="typeName">Тип</param>
        /// <param name="propertyExpression">Лямбда-выражение доступа к свойству</param>
        /// <param name="beginValue">Начальное значение</param>
        /// <param name="endValue">Конечное значение</param>
        public BetweenOperator(string typeName, LambdaExpression propertyExpression, object beginValue, object endValue)
            : base(typeName, propertyExpression)
        {
            InitializeValues(beginValue, endValue);
        }


        /// <summary>
        /// Инициализация значений
        /// </summary>
        /// <param name="beginValue">Начальное значение</param>
        /// <param name="endValue">Конечное значение</param>
        private void InitializeValues(object beginValue, object endValue)
        {
            BeginValue = beginValue;
            EndValue = endValue;
        }


        /// <summary>
        /// Начальное значение
        /// </summary>
        [DataMember]
        public object BeginValue { get; set; }

        /// <summary>
        /// Конечное значение
        /// </summary>
        [DataMember]
        public object EndValue { get; set; }


        /// <summary>
        /// Создать выражение фильтрации
        /// </summary>
        /// <param name="property">Выражение для фильтруемого свойства</param>
        protected override Expression CreateFilter(Expression property)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));
            // property >= BeginValue && property <= EndValue

            Expression betweenExpression;

            if (BeginValue != null || EndValue != null)
            {
                Expression beginExpression = null;
                Expression endExpression = null;

                if (BeginValue != null)
                {
                    beginExpression = Expression.GreaterThanOrEqual(property, CreateConstant(property.Type, BeginValue));
                }

                if (EndValue != null)
                {
                    endExpression = Expression.LessThanOrEqual(property, CreateConstant(property.Type, EndValue));
                }

                if (beginExpression != null && endExpression != null)
                {
                    betweenExpression = Expression.AndAlso(beginExpression, endExpression);
                }
                else if (beginExpression != null)
                {
                    betweenExpression = beginExpression;
                }
                else
                {
                    betweenExpression = endExpression;
                }
            }
            else
            {
                // Открытый период
                betweenExpression = Expression.Constant(true);
            }

            return betweenExpression;
        }
    }
}