﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DALHelperNet.Queries.Constraints
{
    public class AndConstraint<SourceT, DestT> : QueryConstraint<SourceT, DestT>
    {
        public AndConstraint(Expression<Func<SourceT, object>> SourceProperty, EqualityTypes Equality, Expression<Func<DestT, object>> DestinationProperty) : base(SourceProperty, Equality, DestinationProperty)
        {
        }

        public AndConstraint(Expression<Func<SourceT, object>> SourceProperty, EqualityTypes Equality, object EqualityCompare) : base(SourceProperty, Equality, EqualityCompare)
        {
        }

        public new void ResolveConstraint()
        {
            base.ResolveConstraint();

            // return the SQL text of this AND
        }
    }
}
