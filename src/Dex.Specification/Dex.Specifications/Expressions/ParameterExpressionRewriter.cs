using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Dex.Specifications.Expressions
{
    /// <summary>
    /// Класс для замены именованных параметров в выражении
    /// </summary>
    /// <remarks>
    /// Класс используется для замены именованных параметров одного лямбда-выражения
    /// параметрами другого с целью последующего объединения этих выражений заданным
    /// оператором. Естественно, что в оба выражения должны иметь одинаковое число и
    /// перечисление параметров.
    /// </remarks>
    public class ParameterExpressionRewriter : ExpressionVisitor
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="parameterExpressionMap">Список соответствий между параметрами лямбда-выражений</param>
        public ParameterExpressionRewriter(Dictionary<ParameterExpression, ParameterExpression> parameterExpressionMap)
        {
            _parameterExpressionMap = parameterExpressionMap ?? new Dictionary<ParameterExpression, ParameterExpression>();
        }


        /// <summary>
        /// Список соответствий для замены именованных параметров в выражении
        /// </summary>
        private readonly Dictionary<ParameterExpression, ParameterExpression> _parameterExpressionMap;


        /// <summary>
        /// Обработка именованного выражения параметра
        /// </summary>
        /// <param name="parameterExpression">Именованный параметр</param>
        /// <remarks>
        /// Именованные параметры присутствуют, например, в лябда-выражениях: param => ...
        /// </remarks>
        protected override Expression VisitParameter(ParameterExpression parameterExpression)
        {
            ParameterExpression replacement;

            if (_parameterExpressionMap.TryGetValue(parameterExpression, out replacement))
            {
                parameterExpression = replacement;
            }

            return base.VisitParameter(parameterExpression);
        }


        /// <summary>
        /// Заменить параметры в выражении параметрами из списка
        /// </summary>
        /// <param name="parameterExpressionMap">Список соответствий для замены</param>
        /// <param name="expression">Выражение</param>
        /// <returns>Модифицированное выражение</returns>
        public static Expression ReplaceParameters(Dictionary<ParameterExpression, ParameterExpression> parameterExpressionMap, Expression expression)
        {
            return new ParameterExpressionRewriter(parameterExpressionMap).Visit(expression);
        }
    }

    public class ParameterExpressionToPropertyRewriter : ExpressionVisitor
    {
        public ParameterExpressionToPropertyRewriter(Dictionary<string, Expression> parameterExpressionMap)
        {
            _parameterExpressionMap = parameterExpressionMap ?? new Dictionary<string, Expression>();
        }

        private readonly Dictionary<string, Expression> _parameterExpressionMap;

        protected override Expression VisitParameter(ParameterExpression parameterExpression)
        {
            if (parameterExpression == null) throw new ArgumentNullException(nameof(parameterExpression));
            if (_parameterExpressionMap.TryGetValue(parameterExpression.Name, out var replacement))
            {
                return base.VisitMember((MemberExpression)replacement);
            }

            return base.VisitParameter(parameterExpression);
        }

        public static Expression ReplaceParameters(Dictionary<string, Expression> parameterExpressionMap, Expression expression)
        {
            return new ParameterExpressionToPropertyRewriter(parameterExpressionMap).Visit(expression);
        }
    }

}