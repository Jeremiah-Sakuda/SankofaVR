using System;
using System.Collections.Generic;
using Sankofa.Runtime.UI;
using UnityEngine;

namespace Sankofa.Runtime.Personal
{
    /// <summary>A private, hands-accessible editor. Text is ephemeral until an explicit online request or Keep.</summary>
    public sealed class StoryTableKeyboard : MonoBehaviour
    {
        public PokePanel Panel { get; private set; }
        public string Text { get; private set; } = "";
        public bool IsOpen { get; private set; }
        public Action<string> Edited, Committed;
        public Action Cancelled;
        public const int MaximumLength = 500;

        public void Configure(StoryTableView view, PokePanel style)
        {
            Panel = style.CreateSibling(view.TableRoot, "Your question keyboard", new Vector2(.066f, .045f), 8);
            Panel.transform.localPosition = new Vector3(0, .18f, -.19f);
            Panel.transform.localRotation = Quaternion.Euler(25, 0, 0);
            var keys = new List<(string, string)>();
            foreach (var c in "abcdefghijklmnopqrstuvwxyz0123456789.,?'") keys.Add(("key:" + c, c.ToString().ToUpperInvariant()));
            keys.Add(("space", "Space")); keys.Add(("back", "Delete")); keys.Add(("clear", "Clear")); keys.Add(("done", "Done")); keys.Add(("cancel", "Cancel"));
            Panel.Show(keys); Panel.Pressed += Press;
            Panel.gameObject.SetActive(false);
        }

        public void Open(string text)
        {
            Text = (text ?? "").Substring(0, Mathf.Min(MaximumLength, (text ?? "").Length));
            IsOpen = true; Panel.gameObject.SetActive(true); Panel.Hidden = false;
            Edited?.Invoke(Text);
        }

        public void Close() { IsOpen = false; if (Panel != null) Panel.gameObject.SetActive(false); }
        public void Suspend(bool value) { if (Panel != null) Panel.Hidden = value || !IsOpen; }
        public void Press(string id)
        {
            if (!IsOpen || Panel.Hidden) return;
            if (id == "done") { var value = Text; Close(); Committed?.Invoke(value); return; }
            if (id == "cancel") { Close(); Cancelled?.Invoke(); return; }
            if (id == "back" && Text.Length > 0) Text = Text.Substring(0, Text.Length - 1);
            else if (id == "clear") Text = "";
            else if (Text.Length < MaximumLength && id == "space") Text += " ";
            else if (Text.Length < MaximumLength && id.StartsWith("key:") && id.Length == 5) Text += id.Substring(4);
            Edited?.Invoke(Text);
        }
    }
}
