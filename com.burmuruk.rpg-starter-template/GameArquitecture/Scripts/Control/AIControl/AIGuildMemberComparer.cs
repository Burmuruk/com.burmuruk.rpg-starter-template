using Burmuruk.RPGStarterTemplate.Control.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Burmuruk.RPGStarterTemplate.Control
{
    public class AIGuildMemberComparer : IEqualityComparer<AIGuildMember>
    {
        public bool Equals(AIGuildMember x, AIGuildMember y)
        {
            return (x.name == y.name) && (x.transform.position == y.transform.position);
        }

        public int GetHashCode(AIGuildMember obj)
        {
            return obj.name.GetHashCode() + obj.transform.position.GetHashCode();
        }
    }
}
