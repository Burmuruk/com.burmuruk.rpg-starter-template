using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MyDearAnima.Control
{
    public class DisableInTime<T>
    {
        public readonly float time;
        private T item;

        public T Item { get => item; private set => item = value; }
        public bool IsDisabled { get; private set; }

        public DisableInTime(float time, T item)
        {
            this.time = time;
            this.item = item;
            IsDisabled = true;
        }

        public IEnumerator EnableInTime(bool shouldDisable = true)
        {
            Enable(shouldDisable);

            yield return new WaitForSeconds(time);

            Enable(!shouldDisable);
        }

        public void Enable(bool shouldEnable)
        {
            switch (item)
            {
                case GameObject g:
                    g.SetActive(shouldEnable);
                    break;

                case Image i:
                    i.enabled = shouldEnable;
                    break;
            }

            IsDisabled = shouldEnable;
        }

        public void Repleace_Item(T item) => Item = item;
    }
}
