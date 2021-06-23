using NonSucking.Framework.Extension.Rx.SumTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OctoAwesome.SumTypes
{
    public class Selection : Variant<BlockInfo, FunctionalBlock, Entity>
    {
        public Selection(BlockInfo value) : base(value)
        {

        }
        public Selection(FunctionalBlock value) : base(value)
        {

        }
        public Selection(Entity value) : base(value)
        {

        }

        public static implicit operator Selection(BlockInfo obj) 
            => new(obj);

        public static implicit operator Selection(FunctionalBlock obj)
            => new(obj);

        public static implicit operator Selection(Entity obj)
            => new(obj);

    }
}
