using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Nest;

namespace Forum.Domain.Contract.Extensions
{
    public static class QueryableExtention
    {
        public static IQueryable<T> OrderBy<T>(this IQueryable<T> source, string sortColumn, SortOrder sortOrder)
        {
            if (source == null)
                throw new ArgumentNullException("source", "source is null.");

            if (string.IsNullOrEmpty(sortColumn))
                throw new ArgumentException("sortExpression is null or empty.", "sortExpression");

            var isDescending = false;
            var propertyName = "";
            var tType = typeof(T);

            if (!string.IsNullOrWhiteSpace(sortColumn))
            {
                propertyName = sortColumn;

                isDescending = sortOrder == SortOrder.Descending;

                PropertyInfo prop = tType.GetProperty(propertyName);

                if (prop == null)
                {
                    throw new ArgumentException($"No property '{propertyName}' on type '{tType.Name}'");
                }

                var funcType = typeof(Func<,>)
                    .MakeGenericType(tType, prop.PropertyType);

                var lambdaBuilder = typeof(Expression)
                    .GetMethods()
                    .First(x => x.Name == "Lambda" && x.ContainsGenericParameters && x.GetParameters().Length == 2)
                    .MakeGenericMethod(funcType);

                var parameter = Expression.Parameter(tType);
                var propExpress = Expression.Property(parameter, prop);

                var sortLambda = lambdaBuilder
                    .Invoke(null, new object[] { propExpress, new ParameterExpression[] { parameter } });

                var sorter = typeof(Queryable)
                    .GetMethods()
                    .FirstOrDefault(x => x.Name == (isDescending ? "OrderByDescending" : "OrderBy") && x.GetParameters().Length == 2)
                    .MakeGenericMethod(new[] { tType, prop.PropertyType });

                return (IQueryable<T>)sorter
                    .Invoke(null, new object[] { source, sortLambda });
            }

            return source;
        }
    }
}
