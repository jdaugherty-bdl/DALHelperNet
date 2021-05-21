using DALHelperNet.Models;
using DALHelperNet.Queries;
using DALHelperNet.Queries.Constraints;
using DALHelperNet.Queries.Joins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DALHelperNet
{
    public class DataFluentHelper<SourceT>
    {
        //TODO: implement: https://docs.microsoft.com/en-us/previous-versions/bb546158(v=vs.140)?redirectedfrom=MSDN

        private List<object> _executionCommands;

        internal DataFluentHelper() 
        {
            _executionCommands = new List<object>();
        }

        /*
        public DataFluentHelper LeftJoin<T>()
        {
            var type = typeof(T);
            var property = type.GetProperty("ProductMetalProductType");
            var parameter = Expression.Parameter(typeof(T), "x");

            Expression<Func<T, bool>> predicate =
                 (Expression<Func<T, bool>>)Expression.Lambda(
                    Expression.Equal(
                        Expression.MakeMemberAccess(parameter, property),
                       Expression.Constant(metalProdType)),
                            parameter);
            
            return this;
        }
        */
        public DataJoinStatement<SourceT, DestT> LeftJoin<DestT>()
        {
            var joinStatement = new DataJoinStatement<SourceT, DestT>();

            _executionCommands.Add(joinStatement);

            return joinStatement;
        }

        public DataFluentHelper<SourceT> Where(Expression<Func<SourceT, object>> SourceProperty, EqualityTypes Equality, object EqualityCompare)
        {
            _executionCommands.Add(new WhereConstraint<SourceT, object>(SourceProperty, Equality, EqualityCompare));

            return this;
        }

        public DataFluentHelper<SourceT> And(Expression<Func<SourceT, object>> SourceProperty, EqualityTypes Equality, object EqualityCompare)
        {
            _executionCommands.Add(new AndConstraint<SourceT, object>(SourceProperty, Equality, EqualityCompare));

            return this;
        }

        public DataFluentHelper<SourceT> Or(Expression<Func<SourceT, object>> SourceProperty, EqualityTypes Equality, object EqualityCompare)
        {
            _executionCommands.Add(new OrConstraint<SourceT, object>(SourceProperty, Equality, EqualityCompare));

            return this;
        }

        public DALBaseModel ExecuteQuery()
        {
            return new DALBaseModel();
        }

        internal Expression<Func<T, bool>> GenerateExpression<T>(Dictionary<string, object> properties)
        {
            var type = typeof(T);
            List<Expression> expressions = new List<Expression>();
            var parameter = Expression.Parameter(typeof(T), "x");
            foreach (var key in properties.Keys)
            {
                var val = properties[key];
                var property = type.GetProperty(key);

                var eqExpr = Expression.Equal(Expression.MakeMemberAccess(parameter, property), Expression.Constant(val));

                expressions.Add(eqExpr);
            }

            Expression final = expressions.First();
            foreach (var expression in expressions.Skip(1))
            {
                final = Expression.And(final, expression);
            }

            Expression<Func<T, bool>> predicate =
                (Expression<Func<T, bool>>)Expression.Lambda(final, parameter);

            return predicate;
        }
    }
}
