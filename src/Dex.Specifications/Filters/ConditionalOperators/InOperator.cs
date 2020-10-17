using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;

namespace Dex.Specifications.Filters.ConditionalOperators
{
    /// <summary>
    /// Оператор "ВХОДИТ"
    /// </summary>
    [DataContract]
    public class InOperator : BinaryOperator
    {
        public InOperator(string typeName, string propertyName, IEnumerable value)
            : base(typeName, propertyName, value)
        {
        }

        public InOperator(string typeName, LambdaExpression propertyExpression, IEnumerable value)
            : base(typeName, propertyExpression, value)
        {
        }

        protected override Expression CreateFilter(Expression property)
        {
            // Value.Contains(property)

            Expression result;

            var values = Value as IEnumerable;

            // Если коллекция содержит элементы
            if (values != null && values.Cast<object>().Any())
            {
                var containsMethod = Value.GetType().GetTypeInfo().GetMethod("Contains", new[] { property.Type });

                if (containsMethod != null)
                {
                    // Если коллекция имеет метод Contains()
                    result = Expression.Call(Expression.Constant(Value), containsMethod, property);
                }
                else
                {
                    // Поиск метода расширения Contains() для заданной коллекции
                    containsMethod = typeof(Enumerable).GetTypeInfo().GetMethods().Where(method => method.Name == "Contains")
                        .Select(method => new { method, parameters = method.GetParameters() })
                        .Where(m => m.parameters.Length == 2)
                        .Select(m => m.method)
                        .FirstOrDefault();

                    if (containsMethod != null)
                    {
                        containsMethod = containsMethod.MakeGenericMethod(property.Type);
                        result = Expression.Call(containsMethod, Expression.Constant(Value), property);
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }
            }
            else
            {
                result = Expression.Constant(false);
            }

            return result;
        }
    }
}