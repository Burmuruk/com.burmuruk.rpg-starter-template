using UnityEditor;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Editor.Saving
{
    public interface ISavingOperation<TResult>
    {
        TResult Execute(SavingContext context);
    }
}