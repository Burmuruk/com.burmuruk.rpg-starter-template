using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Burmuruk.RPGStarterTemplate.Stats
{
    public interface IBuffUser
    {
        public BuffData[] Buffs { get; }
        public void UpdateBuffData(BuffData[] buffData);
    }
}
