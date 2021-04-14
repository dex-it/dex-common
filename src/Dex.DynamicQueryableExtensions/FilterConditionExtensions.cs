using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization;
using Dex.DynamicQueryableExtensions.Data;

// ReSharper disable NotResolvedInText

namespace Dex.DynamicQueryableExtensions
{
    public static class FilterConditionExtensions
    {
        private static readonly FilterOperation[] ArrayOperations =
        {
            FilterOperation.LK,
            FilterOperation.ILK,
            FilterOperation.IN,
            FilterOperation.NI
        };

        /// <summary>
        /// <![CDATA[а именно условие > 01.04.2020 and < 05.04.2021 включает все 5 число до 23:59:59,
        /// обсдуить нужен ли такой подход, или это ложится на вызывающу строну ]]>
        /// </summary>
        /// <param name="source"></param>
        /// <param name="filterParams"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public static IQueryable<T> Filter<T>(this IQueryable<T> source, IFilterCondition[] filterParams)
        {
            if (filterParams == null || filterParams.Length == 0)
                return source;

            var typeProperties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var filterList = new List<FilterCompareElement>(filterParams.Length);

            var makeFill = typeof(FilterConditionExtensions).GetMethod(nameof(ParseValues),
                BindingFlags.NonPublic | BindingFlags.Static);

            //подготавливаем фильтрацию
            foreach (var item in filterParams)
            {
                var filterItem = new FilterCompareElement
                {
                    Field = item.FieldName,
                    Operation = item.Operation
                };

                var prop = GetPropertyByAttribute(filterItem.Field, typeProperties);

                if (prop is null)
                {
                    throw new Exception(
                        $"Field name {filterItem.Field} from filter, doesn't exist in entity {typeof(T).Name}.");
                }

                filterItem.Property = prop;

                // 3. Значение
                var masItem = item.Value;
                if (prop.PropertyType != typeof(string))
                {
                    // ReSharper disable once PossibleNullReferenceException
                    var genericFill = makeFill.MakeGenericMethod(prop.PropertyType);

                    var genericFillFunc =
                        (Func<IEnumerable<string>, FilterOperation, IList>) genericFill.CreateDelegate(
                            typeof(Func<IEnumerable<string>, FilterOperation, IList>));

                    var valueList = genericFillFunc(masItem, filterItem.Operation);

                    filterItem.Value =
                        filterItem.Operation == FilterOperation.IN || filterItem.Operation == FilterOperation.NI
                            ? valueList
                            : valueList[0];
                }
                else
                {
                    filterItem.Value =
                        filterItem.Operation == FilterOperation.IN || filterItem.Operation == FilterOperation.NI
                            ? masItem
                            : masItem[0];
                }

                filterItem.Property = prop;
                filterList.Add(filterItem);
            }

            //применияем фильтрацию
            foreach (var filterItem in filterList)
            {
                var prop = filterItem.Property;

                var pe = Expression.Parameter(typeof(T), "exprFilter");
                Expression left = Expression.Property(pe, prop.Name);

                // операции не на массивах
                if (!ArrayOperations.Contains(filterItem.Operation))
                {
                    Expression right = Expression.Constant(filterItem.Value, prop.PropertyType);
                    Expression e1;

                    switch (filterItem.Operation)
                    {
                        case FilterOperation.LT:
                            e1 = Expression.LessThan(left, right);
                            break;
                        case FilterOperation.LE:
                            e1 = Expression.LessThanOrEqual(left, right);
                            break;
                        case FilterOperation.EQ:
                            e1 = Expression.Equal(left, right);
                            break;
                        case FilterOperation.GE:
                            e1 = Expression.GreaterThanOrEqual(left, right);
                            break;
                        case FilterOperation.GT:
                            e1 = Expression.GreaterThan(left, right);
                            break;
                        case FilterOperation.NE:
                            e1 = Expression.NotEqual(left, right);
                            break;
                        default:
                            throw new NotSupportedException($"Filter's operator doesn't supported, {nameof(FilterOperation)} = {filterItem.Operation}");
                    }

                    var lambda = Expression.Lambda<Func<T, bool>>(e1, pe);
                    source = source.Where(lambda);
                }
                // операции на массивах
                else
                {
                    Expression<Func<T, bool>> lambda;

                    switch (filterItem.Operation)
                    {
                        case FilterOperation.LK:
                        case FilterOperation.ILK:
                            var parameterExp = Expression.Parameter(typeof(T), "type");
                            var propertyExp = Expression.Property(parameterExp, prop.Name);
                            var method = typeof(string).GetMethod("Contains", new[] {typeof(string)})
                                         ?? throw new ArgumentNullException("typeof(string).GetMethod(\"Contains\", new[] { typeof(string) })");
                            var someValue = Expression.Constant(filterItem.Value, typeof(string));
                            MethodCallExpression containsMethodExp;

                            if (filterItem.Operation == FilterOperation.ILK)
                            {
                                var toLowerMethod = typeof(string).GetMethod("ToLower", new Type[0])
                                                    ?? throw new ArgumentNullException("typeof(string).GetMethod(\"ToLower\", new Type[0])");
                                var lowerMethodExp = Expression.Call(propertyExp, toLowerMethod);
                                containsMethodExp = Expression.Call(lowerMethodExp, method, someValue);
                            }
                            else
                            {
                                containsMethodExp = Expression.Call(propertyExp, method, someValue);
                            }

                            lambda = Expression.Lambda<Func<T, bool>>(containsMethodExp, parameterExp);
                            break;

                        case FilterOperation.IN:
                        case FilterOperation.NI:
                            var listGeneric = typeof(List<>).MakeGenericType(prop.PropertyType);
                            var parameterExpIn = Expression.Parameter(typeof(T), "type");
                            var propertyExpIn = Expression.Property(parameterExpIn, prop.Name);
                            var methodIn = listGeneric.GetMethod("Contains", new[] {prop.PropertyType})
                                           ?? throw new ArgumentNullException("listGeneric.GetMethod(\"Contains\", new[] {prop.PropertyType})");
                            var someValueIn = Expression.Constant(filterItem.Value, listGeneric);
                            var containsMethodExpIn = Expression.Call(someValueIn, methodIn, propertyExpIn);

                            if (filterItem.Operation == FilterOperation.IN)
                            {
                                lambda = Expression.Lambda<Func<T, bool>>(containsMethodExpIn, parameterExpIn);
                            }
                            else
                            {
                                var notContains = Expression.Not(containsMethodExpIn);
                                lambda = Expression.Lambda<Func<T, bool>>(notContains, parameterExpIn);
                            }

                            break;

                        default:
                            throw new NotSupportedException($"Filter's operator doesn't supported, {nameof(FilterOperation)} = {filterItem.Operation}");
                    }

                    source = source.Where(lambda);
                }
            }

            return source;
        }

        private static IList ParseValues<T>(IEnumerable<string> values, FilterOperation operation)
        {
            var list = new List<T>();
            FillList(list, values, operation);
            return list;
        }

        private static void FillList<T>(ICollection<T> mas, IEnumerable<string> values, FilterOperation operation)
        {
            var destType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

            foreach (var val in values)
            {
                if (val.Equals("NULL", StringComparison.OrdinalIgnoreCase))
                {
                    mas.Add(default);
                }
                else
                {
                    if (destType == typeof(DateTime))
                    {
                        var dateValue = DateTime.Parse(val, CultureInfo.InvariantCulture);

                        if (operation == FilterOperation.GE)
                        {
                            dateValue = dateValue.Date;
                        }
                        else if (operation == FilterOperation.LE)
                        {
                            dateValue = dateValue.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
                        }

                        mas.Add((T) Convert.ChangeType(dateValue, destType));
                    }
                    else
                    {
                        mas.Add((T) Convert.ChangeType(val, destType));
                    }
                }
            }
        }

        private static PropertyInfo GetPropertyByAttribute(string fieldName, IEnumerable<PropertyInfo> properties)
        {
            return properties.FirstOrDefault(x =>
            {
                var jsonAttr = (JsonPropertyNameAttribute) Attribute.GetCustomAttribute(x, typeof(JsonPropertyNameAttribute));

                return jsonAttr is null
                    ? string.Equals(x.Name, fieldName, StringComparison.OrdinalIgnoreCase)
                    : string.Equals(jsonAttr.Name, fieldName, StringComparison.OrdinalIgnoreCase);
            });
        }

        private record FilterCompareElement
        {
            /// <summary>
            /// Поле
            /// </summary>
            public string Field { get; init; }

            /// <summary>
            /// Операция
            /// </summary>
            public FilterOperation Operation { get; init; }

            /// <summary>
            /// Значение
            /// </summary>
            public object Value { get; set; }

            /// <summary>
            /// Проперти поля фильтрации
            /// </summary>
            public PropertyInfo Property { get; set; }
        }
    }
}