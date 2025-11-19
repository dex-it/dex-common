using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Dex.Extensions;
using Neo4jClient;
using Neo4jClient.Transactions;

namespace Dex.Neo4J
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class Neo4JIndexExtension
    {
        public static Task CreateIndex<T, TU>(this ITransactionalGraphClient graphClient,
            Expression<Func<T, TU>> selector)
        {
            if (graphClient == null) throw new ArgumentNullException(nameof(graphClient));
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            return CreateIndexFromProps<T>(graphClient, selector.GetPropertyInfo().Name);
        }

        public static Task CreateIndex<T, TU, TU1>(this ITransactionalGraphClient graphClient,
            Expression<Func<T, TU>> selector, Expression<Func<T, TU1>> selector2)
        {
            if (graphClient == null) throw new ArgumentNullException(nameof(graphClient));
            if (selector == null) throw new ArgumentNullException(nameof(selector));
            if (selector2 == null) throw new ArgumentNullException(nameof(selector2));

            var propList = new[]
            {
                selector.GetPropertyInfo().Name,
                selector2.GetPropertyInfo().Name
            };
            return CreateIndexFromProps<T>(graphClient, propList);
        }

        public static Task CreateIndexFromProps<T>(this ICypherGraphClient graphClient, params string[] propList)
        {
            if (graphClient == null) throw new ArgumentNullException(nameof(graphClient));
            if (propList == null) throw new ArgumentNullException(nameof(propList));

            var text = "INDEX ON :" + typeof(T).Name + "(" + string.Join(",", propList) + ")";
            return graphClient.Cypher.Create(text).ExecuteWithoutResultsAsync();
        }
    }
}