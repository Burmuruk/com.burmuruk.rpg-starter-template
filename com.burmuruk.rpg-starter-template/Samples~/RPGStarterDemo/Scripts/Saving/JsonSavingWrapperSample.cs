using Burmuruk.RPGStarterTemplate.UI.Samples;

namespace Burmuruk.RPGStarterTemplate.Saving.Samples
{
    public class JsonSavingWrapperSample : JsonSavingWrapper
    {
        protected override void LoadStage(int stage)
        {
            base.LoadStage(stage);

            switch ((SavingExecution)stage)
            {
                case SavingExecution.General:
                    FindObjectOfType<HUDManager>().Init();
                    break;
            }
        }
    }
}
