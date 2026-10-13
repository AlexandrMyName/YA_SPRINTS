using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SprintsASP_NetCore_API.Domain.Exceptions
{
    public class NoRightsException : Exception
    {
        public NoRightsException(string msg) : base(msg) { }
    }
}
