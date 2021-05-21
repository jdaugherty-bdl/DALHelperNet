using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DALHelperNet.Queries.Joins
{
    public class DataJoinStatement<SourceT, DestT>
    {
        private QueryConstraint<SourceT, DestT> _joinConstraint;

        public DataFluentHelper<SourceT> _mainQuery { get; private set; }

        internal DataJoinStatement() { }

        public DataJoinStatement(DataFluentHelper<SourceT> MainQuery)
        {
            this._mainQuery = MainQuery;
        }

        public DataFluentHelper<SourceT> On(Expression<Func<SourceT, object>> SourceProperty, EqualityTypes Equality, Expression<Func<DestT, object>> DestinationProperty)
        {
            _joinConstraint = new QueryConstraint<SourceT, DestT>(SourceProperty, Equality, DestinationProperty);

            return _mainQuery;
        }
    }
}
