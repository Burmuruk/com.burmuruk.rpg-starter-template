using UnityEditor;
using UnityEditor.Callbacks;

namespace Burmuruk.RPGStarterTemplate.Editor.Saving
{
    public static class ApplyChangesContinuation
    {
        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            EditorApplication.delayCall += ContinueApply;
        }

        private static void ContinueApply()
        {
            var registry = SavingSystem.LoadEnumRegistry();

            if (!registry.waitingForCompilation)
                return;

            registry.waitingForCompilation = false;

            foreach (var definition in registry.definitions)
                definition.hasChanges = false;

            SavingSystem.SaveEnumRegistry(registry);
            CreationManager.CreateEverything();
        }
    }
}