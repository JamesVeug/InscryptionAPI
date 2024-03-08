using InscryptionAPI.Dialogue;
using UnityEngine;

#nullable enable

namespace InscryptionAPI.TalkingCards.Create;

[Serializable]
public class DialogueEventStrings
{
    public string eventName
    {
        get => m_eventName;
        set => m_eventName = value;
    }
    
    public string[] mainLines
    {
        get => m_mainLines;
        set => m_mainLines = value;
    }
    
    public string[][] repeatLines
    {
        get => m_repeatLines;
        set => m_repeatLines = value;
    }

    [SerializeField]
    private string m_eventName;
    
    [SerializeField]
    private string[] m_mainLines;
    
    [SerializeField]
    private string[][] m_repeatLines;
    
    public DialogueEventStrings(string eventName, string[] mainLines, string[][] repeatLines)
    {
        m_eventName = eventName;
        m_mainLines = mainLines;
        m_repeatLines = repeatLines;
    }

    public DialogueEvent CreateEvent(string cardName)
    {
        List<CustomLine> lines = mainLines.Select(x => (CustomLine)x).ToList();
        List<List<CustomLine>> repeatedLines = repeatLines.Select(x => x.Select(y => (CustomLine)y).ToList()).ToList();
        return DialogueManager.GenerateEvent(
            InscryptionAPIPlugin.ModGUID,
            $"{cardName}_{eventName}",
            lines,
            repeatedLines
        );
    }
}