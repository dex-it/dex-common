using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Dex.Extensions
{
    public static class MemberExpressionExtensions
    {
        public static Func<T, object> GetValueGetter<T>(this PropertyInfo propertyInfo)
        {
            if (propertyInfo == null) throw new ArgumentNullException(nameof(propertyInfo));
            if (typeof(T) != propertyInfo.DeclaringType)
            {
                throw new ArgumentOutOfRangeException(nameof(propertyInfo));
            }

            var instance = Expression.Parameter(propertyInfo.DeclaringType, "i");
            var property = Expression.Property(instance, propertyInfo);
            var convert = Expression.TypeAs(property, typeof(object));
            return (Func<T, object>)Expression.Lambda(convert, instance).Compile();
        }

        public static Func<object, object> GetValueGetter(this PropertyInfo propertyInfo)
        {
            if (propertyInfo == null) throw new ArgumentNullException(nameof(propertyInfo));
            
            var declaringType = propertyInfo.DeclaringType;
            var propertyName = propertyInfo.Name;

            return GetValueGetter(declaringType, propertyName);
        }

        public static Func<object, object> GetValueGetter(this Type declaringType, string propertyName)
        {
            var instance = Expression.Parameter(typeof (object), "i");
            var property = Expression.Property(Expression.TypeAs(instance, declaringType), propertyName);
            var convert = Expression.TypeAs(property, typeof (object));
            return (Func<object, object>) Expression.Lambda(convert, instance).Compile();
        }

        public static Action<object, object> GetValueSetter(this Type declaringType, string propertyName)
        {
            var propertyInfo = declaringType.GetTypeInfo().GetProperty(propertyName);
            if (propertyInfo == null)
            {
                throw new ArgumentException($"Type {declaringType} does not contain property {propertyName}", nameof(propertyName));
            }

            var instance = Expression.Parameter(typeof(object), "i");
            var setMethod = propertyInfo.GetSetMethod();
            var argument = Expression.Parameter(typeof(object), "a");
            var setterCall = Expression.Call(
                Expression.Convert(instance, declaringType),
                setMethod,
                Expression.Convert(argument, propertyInfo.PropertyType));

            return (Action<object, object>)Expression.Lambda(setterCall, instance, argument).Compile();
        }

        public static Action<T, object> GetValueSetter<T>(this PropertyInfo propertyInfo)
        {
            if (propertyInfo == null) throw new ArgumentNullException(nameof(propertyInfo));
            if (typeof(T) != propertyInfo.DeclaringType)
            {
                throw new ArgumentOutOfRangeException(nameof(propertyInfo));
            }

            var instance = Expression.Parameter(propertyInfo.DeclaringType, "i");
            var argument = Expression.Parameter(typeof(object), "a");
            var setterCall = Expression.Call(
                instance,
                propertyInfo.GetSetMethod(),
                Expression.Convert(argument, propertyInfo.PropertyType));

            return (Action<T, object>)Expression.Lambda(setterCall, instance, argument).Compile();
        }
    }
}