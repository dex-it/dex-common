using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using Dex.Specifications.Expressions;

namespace Dex.Specifications.Filters.BooleanOperators
{
    /// <summary>
    /// Оператор "И"
    /// </summary>
    [DataContract]
    public class AndOperator : BooleanOperator
    {
        public AndOperator(string typeName, params IFilterOperator[] operators)
            : base(typeName, operators)
        {
        }

        public AndOperator(string typeName, IEnumerable<IFilterOperator> operators)
            : base(typeName, operators)
        {
        }

        public override LambdaExpression CreateFilter()
        {
            var result = CreateTrueLambdaExpression();
            return Operators.Aggregate(result, (current, filterOperator) => current.Compose(filterOperator.CreateFilter(), Expression.AndAlso));
        }

        private LambdaExpression CreateTrueLambdaExpression()
        {
            return Expression.Lambda(CreateFilterDelegateType(), Expression.Constant(true), CreateFilterParameter());
        }
    }
}