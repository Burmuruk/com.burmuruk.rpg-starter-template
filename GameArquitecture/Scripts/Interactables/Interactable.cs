using Burmuruk.RPGStarterTemplate.Control;
using Burmuruk.RPGStarterTemplate.Control.AI;
using Burmuruk.RPGStarterTemplate.Saving;
using Burmuruk.RPGStarterTemplate.Utilities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Burmuruk.RPGStarterTemplate.Interaction
{
    public class Interactable : MonoBehaviour, IJsonSaveable, IInteractable
    {
        [SerializeField] bool isPersistent;
        [SerializeField] bool triggerOnCollition = false;
        [SerializeField] GameObject itemToDisable;
        [SerializeField] bool shouldDisable;

        [SerializeField] public UnityEvent OnInteract;
        [SerializeField] public List<DelayedAction> delayedActions;

        private bool disabled;
        DisableInTime<GameObject> disabler;
        List<ActionScheduler> actionSchedulers = new List<ActionScheduler>();

        [Serializable]
        public struct DelayedAction
        {
            [SerializeField] public UnityEvent Action;
            [SerializeField] public float Delay;
            public DelayedAction(UnityEvent action, float delay)
            {
                Action = action;
                Delay = delay;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!triggerOnCollition || disabled) return;

            var caller = other.GetComponent<AIGuildMember>();

            if (caller == FindAnyObjectByType<PlayerManager>().CurPlayer)
            {
                Interact();
            }
        }

        public JToken CaptureAsJToken(out SavingExecution execution)
        {
            execution = SavingExecution.General;

            if (!isPersistent) return null;

            JObject state = new JObject();

            state["Disabled"] = true;

            return state;
        }

        public virtual void Interact()
        {
            if (disabled) return;

            OnInteract?.Invoke();
            delayedActions.ForEach(action => StartCoroutine(DelayedActionsCoroutine(action)));

            SetDisabled(true);
        }

        public void Restart()
        {
            SetDisabled(false);
        }

        public void LoadAsJToken(JToken state)
        {
            if (state == null) return;

            SetDisabled(state["Disabled"].ToObject<bool>());
        }

        public void StartCinematic(bool start)
        {
            FindObjectOfType<GameManager>().StartCinematic(start);
        }

        private void SetDisabled(bool value)
        {
            disabled = value;
        }

        public void DisableByTime(float time)
        {
            disabler ??= new(time, itemToDisable);

            if (disabler.IsDisabled != shouldDisable)
                StartCoroutine(disabler.EnableInTime(shouldDisable));
        }

        IEnumerator DelayedActionsCoroutine(DelayedAction action)
        {
            yield return new WaitForSeconds(action.Delay);
            action.Action?.Invoke();
        }
    }
}
