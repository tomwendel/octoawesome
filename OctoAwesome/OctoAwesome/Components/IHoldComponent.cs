using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OctoAwesome.Components
{
    public interface IHoldComponent<T>
    {
        void Add(T value);
        void Remove(T value);
    }
}
