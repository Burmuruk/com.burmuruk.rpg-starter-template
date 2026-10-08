using System;
using System.Collections;
using UnityEngine;

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

        private bool ticking;
        private double elapsed;
        private double nextTick;
        private int generation;

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

        public float CurrentTime => currentTime;

        public CoolDownAction(float time) 
        { 
            this.time = time; canUse = true; 
        }

        public CoolDownAction(in float time) : this((float)time) { }

        public CoolDownAction(float time, bool invert) : this(time)
        {
            invertFunction = invert;
            CanUse = true;
        }

        public CoolDownAction(float time, Action<bool> OnFinished) : this(time)
        { 
            this.OnFinished = OnFinished; 
        }

        public CoolDownAction(float time, Action<bool> OnFinished, bool invert) : this(time, invert)
        { 
            this.OnFinished = OnFinished; 
        }

        public CoolDownAction(float time, float tickTime, Action tick, Action<bool> OnFinished) : this(time, OnFinished)
        { 
            this.tickTime = tickTime; OnTick = tick; 
        }

        public void ResetAttributes(float time, Action<bool> OnFinished = null, bool invert = false)
        {
            Cancel();
            this.time = time;
            this.OnFinished = OnFinished;
            invertFunction = invert;
            tickTime = 0;
            OnTick = null;
            currentTime = 0;
            elapsed = 0;
            nextTick = 0;
            CanUse = true;
        }

        public void ResetAttributes(float time, float tickTime, Action tick, Action<bool> OnFinished = null, bool invert = false)
        {
            ResetAttributes(time, OnFinished, invert);
            this.tickTime = tickTime;
            OnTick = tick;
        }

        public void Restart()
        {
            elapsed = 0;
            nextTick = tickTime;
            currentTime = inCoolDown && !ticking ? time : 0;
            
            if (!inCoolDown)
                CanUse = true;
        }

        public void Cancel()
        {
            generation++;
            inCoolDown = false;
            OnTick = null;
            OnFinished = null;
            currentTime = 0;
            CanUse = true;
        }

        private int Begin(bool isTick)
        {
            ticking = isTick;
            elapsed = 0;
            nextTick = tickTime;
            currentTime = isTick ? 0 : time;
            CanUse = false;
            inCoolDown = true;
            return ++generation;
        }

        private void Finish(int token)
        {
            if (token != generation)
                return;

            var callback = OnFinished;
            bool previousCanUse = canUse;
            inCoolDown = false;
            CanUse = true;
            
            callback?.Invoke(previousCanUse);
        }

        public IEnumerator CoolDown()
        {
            if (inCoolDown || time <= 0)
                yield break;

            int token = Begin(false);

            while (elapsed < time)
            {
                yield return null;

                if (token != generation)
                    yield break;

                elapsed += Time.deltaTime;
                currentTime = Mathf.Max(0, time - (float)elapsed);
            }

            Finish(token);
        }

        public IEnumerator Tick()
        {
            if (inCoolDown || OnTick == null || tickTime <= 0 || time <= 0)
                yield break;

            int token = Begin(true);

            while (elapsed < time)
            {
                yield return null;

                if (token != generation)
                    yield break;

                elapsed += Time.deltaTime;
                currentTime = (float)Math.Min(elapsed, time);
                
                while (nextTick <= elapsed && nextTick <= time)
                {
                    nextTick += tickTime;
                    OnTick?.Invoke();

                    if (token != generation)
                        yield break;
                }
            }
            Finish(token);
        }
    }
}
