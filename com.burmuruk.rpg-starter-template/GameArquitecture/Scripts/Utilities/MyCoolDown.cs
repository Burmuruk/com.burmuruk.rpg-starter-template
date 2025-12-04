using System.Collections;
using UnityEngine;
using System;

namespace Burmuruk.Utilities
{
    public class MyCoolDown
    {
        private float time;
        private float currentTime;
        private bool canUse;
        private bool inCoolDown;
        private Action<bool> OnFinished;
        private Action<bool> OnTick;
        private bool invertFunction;

        public bool CanUse
        {
            get => canUse;
            set
            {
                if (inCoolDown)
                    return;

                canUse = invertFunction ? !value : value;
            }
        }

        public MyCoolDown(float time)
        {
            this.time = time;
            currentTime = 0;
            canUse = true;
            inCoolDown = false;
            OnFinished = null;
        }

        public MyCoolDown(in float time)
        {
            this.time = time;
            currentTime = 0;
            canUse = true;
            inCoolDown = false;
            OnFinished = null;
        }

        public MyCoolDown(float time, bool invert) : this(time)
        {
            invertFunction = invert;
            canUse = false;
        }

        public MyCoolDown(float time, Action<bool> OnFinished)
        {
            this.time = time;
            currentTime = 0;
            canUse = true;
            inCoolDown = false;
            this.OnFinished = OnFinished;
            invertFunction = false;
        }

        public MyCoolDown(float time, Action<bool> OnFinished, bool invert)
        {
            this.time = time;
            currentTime = 0;
            canUse = false;
            inCoolDown = false;
            this.OnFinished = OnFinished;
            invertFunction = invert;
        }

        public void ResetAttributes(float time = 0, Action<bool> OnFinished = null, bool invert = false)
        {
            currentTime = 0;
            canUse = false;
            inCoolDown = false;
            this.OnFinished = OnFinished;
            invertFunction = invert;

            if (time != 0)
                this.time = time;

            if (OnFinished != null)
                this.OnFinished = OnFinished;
        }

        public void Restart()
        {
            currentTime = 0;
            canUse = true;
            inCoolDown = false;
        }

        public IEnumerator CoolDown()
        {
            if (inCoolDown) yield break;

            var waiter = new WaitForEndOfFrame();
            CanUse = false;
            inCoolDown = true;
            currentTime = time - Time.deltaTime;

            while (currentTime > 0)
            {
                currentTime -= Time.deltaTime;

                yield return waiter;
            }

            if (OnFinished != null)
                OnFinished(canUse);

            inCoolDown = false;
            CanUse = true;
        }
    }
}