using System.Collections;
using UnityEngine;
using System;

namespace Burmuruk.Utilities
{
    public class CoolDownAction
    {
        private float time;
        private float currentTime;
        private bool canUse;
        private bool inCoolDown;
        private Action<bool> OnFinished;
        private Action OnTick;
        private float tickTime;
        private bool invertFunction;

        public bool CanUse
        {
            get => canUse;
            set
            {
                if (inCoolDown)
                    return;

                canUse = invertFunction? !value : value;
            }
        }

        public float CurrentTime { get => currentTime; }

        public CoolDownAction(float time)
        {
            this.time = time;
            currentTime = 0;
            canUse = true;
            inCoolDown = false;
            OnFinished = null;
        }

        public CoolDownAction(in float time)
        {
            this.time = time;
            currentTime = 0;
            canUse = true;
            inCoolDown = false;
            OnFinished = null;
        }

        public CoolDownAction(float time, float tickTime, Action tick, Action<bool> OnFinished) : this(time, OnFinished)
        {
            this.tickTime = tickTime;
            OnTick = tick;
        }

        public CoolDownAction (float time, bool invert) : this (time)
        {
            invertFunction = invert;
            canUse = false;
        }

        public CoolDownAction(float time, Action<bool> OnFinished)
        {
            this.time = time;
            currentTime = 0;
            canUse = true;
            inCoolDown = false;
            this.OnFinished = OnFinished;
            invertFunction = false;
        }

        public CoolDownAction(float time, Action<bool> OnFinished, bool invert)
        {
            this.time = time;
            currentTime = 0;
            canUse = false;
            inCoolDown = false;
            this.OnFinished = OnFinished;
            invertFunction = invert;
        }

        public void ResetAttributes(float time, Action<bool> OnFinished = null, bool invert = false)
        {
            currentTime = 0;
            canUse = false;
            inCoolDown = false;
            this.OnFinished = OnFinished;
            invertFunction = invert;
            OnTick = null;
            this.time = time;
            this.OnFinished = OnFinished;
        }

        public void ResetAttributes(float time, float tickTime, Action tick, Action<bool> OnFinished = null, bool invert = false)
        {
            ResetAttributes(time, OnFinished, invert);

            this.tickTime = tickTime;
            OnTick = tick;
        }

        public void Restart()
        {
            currentTime = 0;
            canUse = true;
            inCoolDown = false;
        }

        public IEnumerator CoolDown()
        {
            if (inCoolDown || time == 0) yield break;

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

        public IEnumerator Tick()
        {
            if (inCoolDown || OnTick == null || tickTime == 0 || time == 0) yield break;

            var waiter = new WaitForSeconds(tickTime);
            CanUse = false;
            inCoolDown = true;
            currentTime = 0;
            //float tickLaps = 1;

            while (currentTime < time)
            {
                currentTime += tickTime;
                yield return waiter;

                OnTick?.Invoke();
                //currentTime += Time.fixedDeltaTime;
                //yield return new WaitForFixedUpdate();

                //if (currentTime / (tickLaps * tickTime) >= 1)
                //{
                //    OnTick?.Invoke();
                //    tickLaps++;
                //}
            }

            if (OnFinished != null)
                OnFinished(canUse);

            inCoolDown = false;
            CanUse = true;
        }
    }
}