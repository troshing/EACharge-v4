using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EACharge
{
    interface IValue<T>
    {
        T Value { get; set; }
    }
}
