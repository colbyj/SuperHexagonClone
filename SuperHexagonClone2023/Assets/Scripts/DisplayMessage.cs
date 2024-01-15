using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DisplayMessage : MonoBehaviour
{
    public class Message
    {
        public string Text;
        public float Duration;
    }

    public TMP_Text Text;
    public List<Message> Messages = new List<Message>();

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (!Text.enabled && Messages.Count > 0)
        {
            Display(Messages[0].Text, Messages[0].Duration);
            Messages.RemoveAt(0);
        }
    }

    public void Display(string message, float duration)
    {
        Text.text = message;
        Text.enabled = true;
        Invoke("ClearMessage", duration);
    }

    public void AddMessage(string text, float duration)
    {
        Message msg = new Message();
        msg.Text = text;
        msg.Duration = duration;

        Messages.Add(msg);
    }

    public void AddMessageToTop(string text, float duration)
    {
        Message msg = new Message();
        msg.Text = text;
        msg.Duration = duration;

        Messages.Insert(0, msg);
    }

    public void ClearMessage()
    {
        Text.text = "";
        Text.enabled = false;
    }
}
