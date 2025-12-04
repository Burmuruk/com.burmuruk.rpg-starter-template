using Burmuruk.RPGStarterTemplate.Control;
using System;

namespace Burmuruk.RPGStarterTemplate.Inventory
{
    public interface IUsable
    {
        void Use(Character character, object args, Action callback);
    }
}
