using UnityEngine;
using UnityEngine.UI;

namespace Burmuruk.RPGStarterTemplate.UI.Samples
{
    [RequireComponent(typeof(Animator), typeof(Image))]
    public class Fader : MonoBehaviour
    {
        [SerializeField] bool autoFadeoutOnStart = false;

        public void FadeOut()
        {
            //GetComponent<Animator>().enabled = true;
            GetComponent<Animator>().SetTrigger("FadeOut");
        }

        public void FadeIn()
        {
            GetComponent<Animator>().SetTrigger("FadeIn");
        }

        private void OnLevelWasLoaded(int level)
        {
            FadeOut();
        }
    }
}
