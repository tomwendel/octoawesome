using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OctoAwesome.Components
{
    public interface IContainsComponents
    {
        bool ContainsComponent<T>();

        T GetComponent<T>();
    }
}
