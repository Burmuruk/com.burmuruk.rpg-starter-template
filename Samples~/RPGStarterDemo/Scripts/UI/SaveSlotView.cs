using System;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Burmuruk.RPGStarterTemplate.UI.Samples
{
    public class SaveSlotView : MonoBehaviour
    {
        [SerializeField] MyItemButton button;
        [SerializeField] TextMeshProUGUI title;
        [SerializeField] TextMeshProUGUI timePlayed;
        [SerializeField] TextMeshProUGUI membersCount;
        [SerializeField] Image picture;
        ColorBlock originalColors;
        bool initialized;
        public int Id => button.GetId();

        public void Bind(int id, JObject data, Sprite sprite, Action<int> clicked)
        {
            if (button == null)
                button = GetComponent<MyItemButton>();

            if (!initialized)
            { 
                originalColors = button.colors; initialized = true; 
            }

            button.SetId(id);
            button.onClick = new Button.ButtonClickedEvent();
            button.SetCallback(null);
            button.onClick.AddListener(() => clicked(Id));

            if (title != null)
                title.text = id > 0 ? "Manual save " + id : "Auto save " + -id;

            if (timePlayed != null)
                timePlayed.text = data?["TimePlayed"]?.ToString() ?? "0";

            if (membersCount != null)
                membersCount.text = data?["MembersCount"]?.ToString() ?? "0";

            if (picture != null)
            {
                picture.sprite = sprite;
                picture.enabled = sprite != null;
            }
        }

        public void SetState(bool interactable, bool selected, Color tint)
        {
            button.interactable = interactable;
            var colors = originalColors;
            if (selected)
            {
                colors.normalColor = tint;
                colors.highlightedColor = tint;
                colors.selectedColor = tint;
                colors.pressedColor = tint;
            }
            button.colors = colors;
        }
    }
}
