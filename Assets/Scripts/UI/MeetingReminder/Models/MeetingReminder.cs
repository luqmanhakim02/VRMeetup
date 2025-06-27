using System;
using System.Collections.Generic;

[Serializable]
public class MeetingReminder
{
    public string id; // Unique 6-char code
    public string meetingName;
    public int maxParticipants;
    public string dateTime; // Use ISO 8601 format: "2025-06-26T14:00:00Z"
    public List<string> participants = new();
}
