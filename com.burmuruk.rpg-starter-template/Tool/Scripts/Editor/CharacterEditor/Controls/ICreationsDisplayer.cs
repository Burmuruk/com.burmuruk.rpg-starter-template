using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Burmuruk.RPGStarterTemplate.UI
{
    internal interface ICreationsDisplayer
    {
        public void BindBuffsOptions(Func<List<string>> GetNames);
    }
}
