using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sprints.Users.Domain.Exceptions
{
    public class NoRightsException : Exception
    {
        public NoRightsException(string msg) : base(msg) { }
    }
}
