using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DALHelperNet.Queries
{
    public enum EqualityTypes
    {
        EqualTo,
        NotEqualTo,
        GreaterThan,
        LessThan,
        IsInSet
    }

    public class QueryConstraint<SourceT, DestT>
    {
        protected internal Expression<Func<SourceT, object>> _sourcePropertyFunc;
        protected internal object _sourcePropertyObject;

        protected internal EqualityTypes _queryEquality;

        protected internal Expression<Func<DestT, object>> _destinationPropertyFunc;
        protected internal object _destinationPropertyObject;

        public QueryConstraint(Expression<Func<SourceT, object>> SourceProperty, EqualityTypes Equality, object EqualityCompare)
        {
            _sourcePropertyFunc = SourceProperty;
            _queryEquality = Equality;
            _destinationPropertyObject = EqualityCompare;
        }

        public QueryConstraint(Expression<Func<SourceT, object>> SourceProperty, EqualityTypes Equality, Expression<Func<DestT, object>> DestinationProperty)
        {
            _sourcePropertyFunc = SourceProperty;
            _queryEquality = Equality;
            _destinationPropertyFunc = DestinationProperty;
        }

        public void ResolveConstraint()
        {
            MemberExpression body = (MemberExpression)_sourcePropertyFunc.Body;
            var ddd = body.Member.Name;

            body = (MemberExpression)_destinationPropertyFunc.Body;
            var eee = body.Member.Name;
        }
    }
}
