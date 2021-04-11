using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DALHelperNet.Interfaces.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Struct)]
    public sealed class DALTransferProperty : Attribute
    {
    }
}
