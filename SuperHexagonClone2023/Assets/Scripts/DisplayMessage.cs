using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DisplayMessage : MonoBehaviour
{

    public class Message
    {
        public string text;
        public float duration;
    }

    public Text text;
    public List<Message> messages = new List<Message>();

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (!text.enabled && messages.Count > 0)
        {
            Display(messages[0].text, messages[0].duration);
            messages.RemoveAt(0);
        }
    }

    public void Display(string message, float duration)
    {
        text.text = message;
        text.enabled = true;
        Invoke("ClearMessage", duration);
    }

    public void AddMessage(string text, float duration)
    {
        Message msg = new Message();
        msg.text = text;
        msg.duration = duration;

        messages.Add(msg);
    }

    public void AddMessageToTop(string text, float duration)
    {
        Message msg = new Message();
        msg.text = text;
        msg.duration = duration;

        messages.Insert(0, msg);
    }

    public void ClearMessage()
    {
        text.text = "";
        text.enabled = false;
    }
}
