using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Dialogue Content", menuName = "Dialogue System/Dialogue Content")]
public class DialogueContentSO : ScriptableObject
{
    [Serializable]
    public class DialogueLine
    {
        public string text;
        public AudioClip voiceClip;
        public string textHighlight;

        [Header("Highlight Settings")]
        public Color highlightColor = Color.yellow;
    }

    [Header("Dialogue Content")]
    public List<DialogueLine> dialogueLines = new List<DialogueLine>();


}