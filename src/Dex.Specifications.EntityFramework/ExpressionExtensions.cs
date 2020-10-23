using System;
using System.Linq.Expressions;

namespace Dex.Specifications.EntityFramework
{
    public static class ExpressionExtensions
    {
        public static string GetMemberName(this Expression memberExpression)
        {
            if (memberExpression == null) throw new ArgumentNullException(nameof(memberExpression));

            var lambdaExpression = (LambdaExpression) memberExpression;
            
            if (!(lambdaExpression.Body is MemberExpression))
            {
                throw new InvalidOperationException("Expression must be a member expression");
            }

            var expression = (MemberExpression) lambdaExpression.Body;

            return expression.Member.Name;
        }
    }
}