using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DALHelperNet.Queries.Constraints
{
    public class Constraint
    {
        public Type ConstraintPropertyType { get; set; }
        public object ConstraintValue { get; set; }

        public Constraint(Type ConstraintPropertyType, object ConstraintValue)
        {
            this.ConstraintPropertyType = ConstraintPropertyType;
            this.ConstraintValue = ConstraintValue;
        }
    }
}
