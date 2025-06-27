using System;
using TMPro;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;

public class ReminderSlotUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] TMP_Text meetingNameText;
    [SerializeField] TMP_Text dateTimeText;
    [SerializeField] TMP_Text participantCountText;
    [SerializeField] TMP_Text daysRemainingText;
    [SerializeField] Button joinButton;
    [SerializeField] Button deleteButton;
    [SerializeField] GameObject hoverJoinVisual;
    [SerializeField] TMP_Text reminderIdText;

    [Header("Optional Settings")]
    [SerializeField] bool hideJoinButton = true;

    private MeetingReminder reminderData;
    private ReminderUIController reminderUIController;
    private bool isJoinable = false;

    public void Setup(MeetingReminder data)
    {
        reminderData = data;

        meetingNameText.text = data.meetingName;
        dateTimeText.text = FormatDateTime(data.dateTime);
        participantCountText.text = $"{data.participants.Count:D2}/{data.maxParticipants:D2}";
        daysRemainingText.text = GetTimeRemaining(data.dateTime);
        reminderIdText.text = $"ID: {data.id}";

        // Hide join button unless it's allowed and joinable
        joinButton.gameObject.SetActive(false);
        hoverJoinVisual.SetActive(false);

        if (!hideJoinButton)
        {
            joinButton.onClick.RemoveAllListeners();
            joinButton.onClick.AddListener(JoinReminder);
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(DeleteOrLeaveReminder);
        }
    }

    void Update()
    {
        if (reminderData == null || hideJoinButton) return;

        TimeSpan remaining = GetTimeUntil(reminderData.dateTime);
        bool nowJoinable = remaining.TotalMinutes <= 10 && remaining.TotalMinutes > -30;

        if (nowJoinable != isJoinable)
        {
            isJoinable = nowJoinable;
            hoverJoinVisual.SetActive(isJoinable);
            joinButton.gameObject.SetActive(isJoinable);
        }
    }

    private string FormatDateTime(string isoString)
    {
        if (DateTime.TryParse(isoString, out DateTime dt))
        {
            return dt.ToString("dd MMM yyyy  HH:mm");
        }
        return "Invalid Date";
    }

    private string GetTimeRemaining(string isoString)
    {
        TimeSpan span = GetTimeUntil(isoString);
        if (span.TotalDays >= 1)
            return $"{Mathf.FloorToInt((float)span.TotalDays)} Days";
        if (span.TotalHours >= 1)
            return $"{Mathf.FloorToInt((float)span.TotalHours)} Hours";
        if (span.TotalMinutes >= 1)
            return $"{Mathf.FloorToInt((float)span.TotalMinutes)} Min";
        return "Now";
    }

    private TimeSpan GetTimeUntil(string isoString)
    {
        if (DateTime.TryParse(isoString, out DateTime dt))
        {
            return dt - DateTime.Now;
        }
        return TimeSpan.MaxValue;
    }

    private void JoinReminder()
    {
        Debug.Log($"Joining reminder: {reminderData.meetingName}");
        // TODO: Launch VR view, session, or UI change
    }

    private void DeleteOrLeaveReminder()
    {
        bool isHost = reminderData.participants.Count > 0 &&
                      reminderData.participants[0] == AuthenticationService.Instance.PlayerId;

        bool success = isHost
            ? ReminderManager.Instance.DeleteReminder(reminderData.id)
            : ReminderManager.Instance.LeaveReminder(reminderData.id);

        if (success)
        {
            Debug.Log(isHost ? "Deleted reminder." : "Left reminder.");

            // Try to refresh UI
            reminderUIController = FindFirstObjectByType<ReminderUIController>();
            reminderUIController?.RefreshReminderList();
        }
        else
        {
            Debug.LogWarning("Failed to delete or leave reminder.");
        }
    }
}
