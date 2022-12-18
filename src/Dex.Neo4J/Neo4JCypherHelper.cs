using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Dex.Neo4J
{
    public static class Neo4JCypherHelper
    {
        public static string BuildQueryCase<T>(string targetName, Expression<Func<T, object>> selector) where T : class
        {
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            if (selector.Body is UnaryExpression { Operand: ConditionalExpression conditionalExpression })
            {
                var ifTrueValue = GetValueExpression(conditionalExpression.IfTrue, targetName);
                var ifFalseValue = GetValueExpression(conditionalExpression.IfFalse, targetName);
                var binaryExpressionValue = GetValueExpression(conditionalExpression.Test, targetName);

                return $"CASE WHEN {binaryExpressionValue} THEN {ifTrueValue} ELSE {ifFalseValue} END";
            }

            throw new NotSupportedException($"Not supported ({selector.Body}) expression. Supported only ConditionalExpression");
        }

        private static string GetValueExpression(Expression expression, string targetName)
        {
            if (expression is BinaryExpression binaryExpression)
            {
                string? operand = null;

                if (expression.NodeType == ExpressionType.OrElse)
                {
                    operand = "OR";
                }

                if (expression.NodeType == ExpressionType.AndAlso)
                {
                    operand = "AND";
                }

                if (expression.NodeType == ExpressionType.Equal)
                {
                    operand = "=";
                }

                if (expression.NodeType == ExpressionType.NotEqual)
                {
                    operand = "<>";
                }

                return $"{GetValueExpression(binaryExpression.Left, targetName)} {operand} {GetValueExpression(binaryExpression.Right, targetName)}";
            }

            if (expression is ConstantExpression constantExpression)
            {
                return constantExpression.Value.ToString();
            }

            if (expression is MemberExpression memberExpression)
            {
                if (memberExpression.Member is FieldInfo fieldInfo)
                {
                    return $"\"{fieldInfo.GetValue((memberExpression.Expression as ConstantExpression)?.Value)}\"";
                }

                return $"{targetName}.{memberExpression.Member.Name}";
            }

            throw new NotSupportedException($"Expression ({expression}) not supported");
        }
    }
}