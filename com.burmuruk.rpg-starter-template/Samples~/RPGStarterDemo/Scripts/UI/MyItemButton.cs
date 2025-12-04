using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Burmuruk.RPGStarterTemplate.UI.Samples
{
    public class MyItemButton : Button
    {
        [SerializeField] int id;
        Action callback;
        public event Action<int> OnPointerEnterEvent;
        public event Action OnRightClick;

        public void SetId(int id)
        {
            this.id = id;
        }

        public int GetId() => id;

        public void SetCallback(Action callback)
        {
            this.callback = callback;
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            base.OnPointerClick(eventData);
            
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                OnRightPointerClick();
            }

            callback?.Invoke();
            callback = null;
        }

        public void OnRightPointerClick()
        {
            OnRightClick?.Invoke();
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);

            OnPointerEnterEvent?.Invoke(id);
        }
    }
}