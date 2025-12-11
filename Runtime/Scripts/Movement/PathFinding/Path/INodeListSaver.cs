using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Burmuruk.WorldG.Patrol
{
    public interface INodeListSaver
    {
        void SaveList(INodeListSupplier nodes);
        INodeListSupplier GetNodeList();
    }
}
