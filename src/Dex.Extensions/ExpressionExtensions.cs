using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Dex.Extensions
{
    public static class ExpressionExtensions
    {
        public static PropertyInfo GetPropertyInfo<TSource, TProperty>(
            this Expression<Func<TSource, TProperty>> propertyLambda)
        {
            if (propertyLambda.ToString().Count(x => x == '.') > 1)
                throw new ArgumentException("only simple access supported, x.Property");

            var type = typeof(TSource);

            if ((propertyLambda.Body is MemberExpression member) is false)
                throw new ArgumentException($"Expression '{propertyLambda}' refers to a method, not a property.");

            var propInfo = member.Member as PropertyInfo;
            if (propInfo == null)
                throw new ArgumentException($"Expression '{propertyLambda}' refers to a field, not a property.");

            if (type != propInfo.ReflectedType
                && !type.IsSubclassOf(propInfo.ReflectedType ?? throw new InvalidOperationException()))
                throw new ArgumentException(
                    $"Expression '{propertyLambda}' refers to a property that is not from type {type}.");

            return propInfo;
        }

        public static IEnumerable<Exception> GetInnerExceptions(this Exception exception, int count = 5)
        {
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(exception);
#endif

            if (count <= 0) yield break;

            var innerException = exception;
            do
            {
                yield return innerException;

                innerException = innerException.InnerException;
                count--;
            } while (innerException != null && count > 0);
        }
    }
}