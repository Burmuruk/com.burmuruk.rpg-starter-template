using Burmuruk.RPGStarterTemplate.Control;
using Burmuruk.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Stats
{
    public class BuffsManager : MonoBehaviour
    {
        private sealed class ActiveEffect
        {
            public Guid id;
            public Guid modifierId;
            public Character character;
            public BuffData data;
            public Action tick;
            public CoolDownAction timer;
        }
        
        private readonly Dictionary<Guid, ActiveEffect> active = new();
        const int timersCount = 30;
        private readonly Queue<CoolDownAction> timers = new();
        private readonly Dictionary<CoolDownAction, (Character character, Coroutine coroutine, BuffData buff)> runningTimers = new();
        public static BuffsManager Instance { get; private set; }

        public event Action<Character, BuffData> OnEffectExecuted;
        public event Action<Guid, Character, BuffData> OnEffectApplied;
        public event Action<Guid, Character, BuffData> OnEffectRemoved;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Initilize();
        }

        private void OnDisable()
        {
            if (Instance == this)
                RemoveAllBuffs();
        }

        private void OnDestroy()
        {
            if (Instance != this)
                return;

            RemoveAllBuffs();
            Instance = null;
        }

        private void Initilize()
        {
            Instance = this;
            for (int i = 0; i < timersCount; i++)
                timers.Enqueue(new CoolDownAction(0));
        }

        public int ActiveEffectCount => active.Count;

        public void AddBuff(Character character, in BuffData buff, Action tickAction = null)
        {
            ApplyEffect(character, buff, tickAction);
        }

        public Guid ApplyEffectIfNotActive(Character character, BuffData buff, Action action = null, bool rollProbability = false)
        {
            if (HasActiveEffect(character, buff))
                return Guid.Empty;

            return ApplyEffect(
                character,
                buff,
                action,
                rollProbability);
        }

        public Guid ApplyEffect(Character character, BuffData buff, Action action = null, bool rollProbability = false)
        {
            Guid? guid = VerifyRequest(character, buff, action, rollProbability, out bool modifier, out bool timed);

            if (guid.HasValue)
                return guid.Value;

            if (buff.effectType == EffectType.Instant)
            {
                ExecuteChange(character, buff, action);
                return Guid.Empty;
            }

            var effect = new ActiveEffect
            {
                id = Guid.NewGuid(),
                character = character,
                data = buff,
                tick = action
            };

            if (modifier)
            {
                float delta = buff.value;
                if (buff.percentage)
                {
                    ModsList.TryGetBaseValue(character, buff.stat, out float basis);
                    delta = basis * buff.value / 100f;
                }

                if (!ModsList.AddModification(character, buff.stat, delta, out effect.modifierId))
                    return Reject("Could not register modifier: " + buff.stat);
            }

            active.Add(effect.id, effect);

            OnEffectApplied?.Invoke(effect.id, effect.character, buff);

            if (timed)
                SetTimer(effect);

            return effect.id;
        }

        private Guid? VerifyRequest(Character character, BuffData buff, Action action, bool rollProbability, out bool modifier, out bool timed)
        {
            modifier = false;
            timed = false;

            if (character == null || !character.gameObject.activeInHierarchy || !isActiveAndEnabled)
                return Guid.Empty;

            if (!ValidNumber(buff.value) || !ValidNumber(buff.duration) || !ValidNumber(buff.rate))
                return Reject("Effect contains a non-finite value.");

            if (!Enum.IsDefined(typeof(EffectType), buff.effectType))
                return Reject("Unknown effect type.");

            modifier = buff.effectType == EffectType.Temporary || buff.effectType == EffectType.UntilRemoved;
            timed = buff.effectType == EffectType.Temporary || buff.effectType == EffectType.Periodic;

            if (timed && buff.duration <= 0)
                return Reject("Temporary/Periodic effects require duration > 0.");

            if (buff.effectType == EffectType.Periodic && buff.rate <= 0)
                return Reject("Periodic effects require rate > 0.");

            if (modifier && action != null)
                return Reject("Reversible modifiers cannot use a one-way callback.");

            if (action == null && !ModsList.IsRegistered(character, buff.stat))
                return Reject("Stat is not registered: " + buff.stat);

            if (action == null && buff.stat == ModifiableStat.HP && character.Health == null)
                return Reject("Character has no Health component.");

            if (action == null && buff.stat == ModifiableStat.HP && buff.percentage)
                return Reject("HP percentages need an explicit callback defining current/max HP as the reference.");

            if (rollProbability)
            {
                if (!ValidNumber(buff.probability) || buff.probability < 0 || buff.probability > 1)
                    return Reject("Probability must be in [0, 1].");

                if (buff.probability <= 0 || (buff.probability < 1 && UnityEngine.Random.value >= buff.probability))
                    return Guid.Empty;
            }

            return null;
        }

        private static bool ValidNumber(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private Guid Reject(string message) { Debug.LogWarning(message, this); return Guid.Empty; }

        private void ExecuteChange(Character character, BuffData data, Action action)
        {
            if (action != null)
            { 
                action(); return; 
            }

            if (data.stat == ModifiableStat.HP)
            {
                int amount = Mathf.RoundToInt(Mathf.Abs(data.value));

                if (amount == 0)
                    return;

                if (data.value < 0)
                    character.Health.ApplyDamage(amount);
                else
                    character.Health.Heal(amount);
                return;
            }

            float delta = data.value;

            if (data.percentage)
            {
                if (!ModsList.TryGetBaseValue(character, data.stat, out float basis))
                    return;

                delta = basis * data.value / 100f;
            }

            ModsList.ApplyChange(character, data.stat, delta);

            OnEffectExecuted?.Invoke(character, data);
        }

        private void SetTimer(ActiveEffect effect)
        {
            CoolDownAction coolDown = timers.Count > 0 ? timers.Dequeue() : new CoolDownAction(0);
            effect.timer = coolDown;

            if (effect.data.effectType == EffectType.Periodic)
            {
                coolDown.ResetAttributes(effect.data.duration, effect.data.rate,
                    () =>
                    {
                        ExecuteChange(effect.character, effect.data, effect.tick);

                        if (effect.character == null || !effect.character.gameObject.activeInHierarchy)
                            RemoveEffectInternal(effect.id, false);
                    },
                    _ => RemoveEffectInternal(effect.id, false));
            }
            else
                coolDown.ResetAttributes(effect.data.duration, _ => RemoveEffectInternal(effect.id, false));

            runningTimers.Add(coolDown, (effect.character, null, effect.data));
            Coroutine coroutine = StartCoroutine(RunTimer(effect));

            if (runningTimers.ContainsKey(coolDown))
                runningTimers[coolDown] = (effect.character, coroutine, effect.data);
        }

        private IEnumerator RunTimer(ActiveEffect effect)
        {
            IEnumerator routine = effect.data.effectType == EffectType.Periodic
                ? effect.timer.Tick() : effect.timer.CoolDown();

            while (active.ContainsKey(effect.id))
            {
                if (effect.character == null || !effect.character.gameObject.activeInHierarchy)
                { 
                    RemoveEffectInternal(effect.id, false); 
                    yield break; 
                }
                bool hasNext;

                try
                { 
                    hasNext = routine.MoveNext(); 
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                    RemoveEffectInternal(effect.id, false);
                    hasNext = false;
                }

                if (!hasNext)
                { 
                    RemoveEffectInternal(effect.id, false); 
                    yield break; 
                }

                yield return routine.Current;
            }
        }

        private void RemoveTimer(CoolDownAction coolDown, bool stopCoroutine = true)
        {
            if (!runningTimers.TryGetValue(coolDown, out (Character character, Coroutine coroutine, BuffData buff) timer))
                return;

            runningTimers.Remove(coolDown);
            coolDown.Cancel();

            if (stopCoroutine && timer.coroutine != null)
                StopCoroutine(timer.coroutine);

            timers.Enqueue(coolDown);
        }

        public bool RemoveEffect(Guid id)
        {
            return RemoveEffectInternal(id, true);
        }

        private bool RemoveEffectInternal(Guid id, bool stopCoroutine)
        {
            if (!active.TryGetValue(id, out ActiveEffect effect))
                return false;

            active.Remove(id);

            if (effect.timer != null)
                RemoveTimer(effect.timer, stopCoroutine);

            if (effect.modifierId != Guid.Empty && effect.character != null)
                ModsList.RemoveModification(effect.character, effect.data.stat, effect.modifierId);

            OnEffectRemoved?.Invoke(effect.id, effect.character, effect.data);

            return true;
        }

        public bool RefreshEffect(Guid id)
        {
            if (!active.TryGetValue(id, out ActiveEffect effect) || effect.timer == null)
                return false;

            effect.timer.Restart();
            return true;
        }

        public void RemoveBuff(CoolDownAction coolDown, Character character, ModifiableStat type, float modification)
        {
            ActiveEffect effect = active.Values.FirstOrDefault(item => item.timer == coolDown &&
                item.character == character && item.data.stat == type);

            if (effect != null)
                RemoveEffect(effect.id);
        }

        public KeyValuePair<CoolDownAction, (Character character, Coroutine coroutine, BuffData buff)>[] GetCharacterTimers(Character character)
        {
            return runningTimers.Where(timer => timer.Value.character == character).ToArray();
        }

        public void RemoveAllBuffs(Character character)
        {
            var ids = new List<Guid>();
            foreach (KeyValuePair<Guid, ActiveEffect> pair in active)
            {
                if (pair.Value.character == character)
                    ids.Add(pair.Key);
            }

            foreach (Guid id in ids)
                RemoveEffect(id);
            
            if (character != null)
                ModsList.RemoveAllModifications(character);
        }

        public void RemoveAllBuffs()
        {
            foreach (Guid id in new List<Guid>(active.Keys))
                RemoveEffect(id);
        }

        public bool HasActiveEffect(Character character, BuffData buff)
        {
            if (character == null)
                return false;

            return active.Values.Any(effect =>
                effect.character == character &&
                IsSameEffect(effect.data, buff));
        }

        private static bool IsSameEffect(in BuffData a, in BuffData b)
        {
            return a.effectType == b.effectType &&
                   a.stat == b.stat &&
                   a.value == b.value &&
                   a.percentage == b.percentage &&
                   a.duration == b.duration &&
                   a.rate == b.rate;
        }
    }
}
