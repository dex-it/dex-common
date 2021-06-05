using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using Dex.Specifications.Expressions;

namespace Dex.Specifications.Filters.BooleanOperators
{
    /// <summary>
    /// Оператор "ИЛИ"
    /// </summary>
    [DataContract]
    public class OrOperator : BooleanOperator
    {
        public OrOperator(string typeName, params IFilterOperator[] operators)
            : base(typeName, operators)
        {
        }

        public OrOperator(string typeName, IEnumerable<IFilterOperator> operators)
            : base(typeName, operators)
        {
        }

        public override LambdaExpression CreateFilter()
        {
            var result = CreateFalseLambdaExpression();
            return Operators.Aggregate(result, (current, filterOperator) => current.Compose(filterOperator.CreateFilter(), Expression.OrElse));
        }

        private LambdaExpression CreateFalseLambdaExpression()
        {
            return Expression.Lambda(CreateFilterDelegateType(), Expression.Constant(false), CreateFilterParameter());
        }
    }
}