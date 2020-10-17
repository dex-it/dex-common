using System;
using System.Linq;
using System.Linq.Expressions;

namespace Dex.Specifications.Expressions
{
    /// <summary>
    /// Методы расширения для работы с лямбда-выражениями
    /// </summary>
    /// <remarks>
    /// Данные методы используются для объединения условий спецификаций, 
    /// объединенных соответствующими операторами.
    /// </remarks>
    public static class ExpressionExtensions
    {
        /// <summary>
        /// Инвертировать выражение операцией логического отрицания
        /// </summary>
        /// <typeparam name="T">Тип данных, над которым производится выражение</typeparam>
        /// <param name="first">Заданное выражение</param>
        /// <returns>Инвертированное выражение</returns>
        public static Expression<Func<T, bool>> Not<T>(this Expression<Func<T, bool>> first)
        {
            return Expression.Lambda<Func<T, bool>>(Expression.Not(first.Body), first.Parameters);
        }

        /// <summary>
        /// Объединить выражение операцией логического И
        /// </summary>
        /// <typeparam name="T">Тип данных, над которым производится выражение</typeparam>
        /// <param name="first">Заданное выражение</param>
        /// <param name="second">Выражение для объединения</param>
        /// <returns>Объединенное выражение</returns>
        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
        {
            return first.Compose(second, Expression.AndAlso);
        }

        /// <summary>
        /// Объединить выражение операцией логического ИЛИ
        /// </summary>
        /// <typeparam name="T">Тип данных, над которым производится выражение</typeparam>
        /// <param name="first">Заданное выражение</param>
        /// <param name="second">Выражение для объединения</param>
        /// <returns>Объединенное выражение</returns>
        public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
        {
            return first.Compose(second, Expression.OrElse);
        }

        /// <summary>
        /// Составить выражение из текущего и заданного
        /// </summary>
        /// <typeparam name="T">Тип данных, над которым производится выражение</typeparam>
        /// <param name="first">Заданное выражение</param>
        /// <param name="second">Выражение для объединения</param>
        /// <param name="expressionType">Тип операции для объединения</param>
        /// <returns>Объединенное выражение</returns>
        public static Expression<T> Compose<T>(this Expression<T> first, Expression<T> second, Func<Expression, Expression, Expression> expressionType)
        {
            // Замена именованных параметров второго выражения соответствующими параметрами первого
            var parameterExpressionMap = first.Parameters.Select((f, i) => new { f, s = second.Parameters[i] }).ToDictionary(p => p.s, p => p.f);
            var secondBody = ParameterExpressionRewriter.ReplaceParameters(parameterExpressionMap, second.Body);

            // Объединение выражений заданной операцией
            return Expression.Lambda<T>(expressionType(first.Body, secondBody), first.Parameters);
        }

        /// <summary>
        /// Составить выражение из текущего и заданного
        /// </summary>
        /// <param name="first">Заданное выражение</param>
        /// <param name="second">Выражение для объединения</param>
        /// <param name="expressionType">Тип операции для объединения</param>
        /// <returns>Объединенное выражение</returns>
        public static LambdaExpression Compose(this LambdaExpression first, LambdaExpression second, Func<Expression, Expression, Expression> expressionType)
        {
            // Замена именованных параметров второго выражения соответствующими параметрами первого
            var parameterExpressionMap = first.Parameters.Select((f, i) => new { f, s = second.Parameters[i] }).ToDictionary(p => p.s, p => p.f);
            var secondBody = ParameterExpressionRewriter.ReplaceParameters(parameterExpressionMap, second.Body);

            // Объединение выражений заданной операцией
            return Expression.Lambda(first.Type, expressionType(first.Body, secondBody), first.Parameters);
        }
    }
}