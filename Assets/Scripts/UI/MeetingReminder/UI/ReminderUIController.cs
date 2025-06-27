using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ReminderUIController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject panelMeetingReminder;    // 0_MeetingReminder
    public GameObject panelScheduleMeeting;    // 1_ScheduleMeeting

    [Header("Reminder Warning Toast")]
    public GameObject warningToastObject;      // Set this to the toast GameObject in the reminder panel

    [Header("Schedule Inputs")]
    public TMP_InputField inputMeetingName;
    public TMP_Text inputMaxParticipants;
    public TMP_Text inputDateTime;             // ISO format input

    [Header("Join Reminder")]
    public TMP_InputField inputJoinReminderCode;

    [Header("Reminder List Container")]
    public Transform reminderListContainer;    // Scroll View Content
    public GameObject reminderItemPrefab;      // Prefab that includes ReminderSlotUI

    [Header("Generated ID Display")]
    public TMP_Text generatedReminderIDText;

    private void Start()
    {
        ShowRelevantPanel();
    }

    public void ShowRelevantPanel()
    {
        panelMeetingReminder.SetActive(true);
        panelScheduleMeeting.SetActive(false);

        RefreshReminderList();
    }

    public void RefreshReminderList()
    {
        var userReminders = ReminderManager.Instance.GetMyReminders();
        bool hasReminders = userReminders != null && userReminders.Count > 0;

        if (warningToastObject != null)
            warningToastObject.SetActive(!hasReminders);

        if (hasReminders)
            PopulateReminderList(userReminders);
        else
            ClearReminderList();
    }

    private void PopulateReminderList(List<MeetingReminder> reminders)
    {
        ClearReminderList();

        foreach (var r in reminders)
        {
            var item = Instantiate(reminderItemPrefab, reminderListContainer);
            item.GetComponent<ReminderSlotUI>().Setup(r);
        }
    }

    private void ClearReminderList()
    {
        foreach (Transform child in reminderListContainer)
            Destroy(child.gameObject);
    }

    public void OnClickCreateReminderPanel()
    {
        panelScheduleMeeting.SetActive(true);
        panelMeetingReminder.SetActive(false);
    }

    public void OnClickCancelSchedule()
    {
        ShowRelevantPanel();
    }

    public void OnClickCreateReminder()
    {
        string name = inputMeetingName.text.Trim();
        string maxStr = inputMaxParticipants.text.Trim();
        string dtStr = inputDateTime.text.Trim();

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(maxStr) || string.IsNullOrEmpty(dtStr))
        {
            Debug.LogWarning("All fields are required.");
            return;
        }

        if (!int.TryParse(maxStr, out int max) || !DateTime.TryParse(dtStr, out DateTime dateTime))
        {
            Debug.LogWarning("Invalid max participants or date format.");
            return;
        }

        var reminder = ReminderManager.Instance.CreateReminder(name, max, dateTime);

        if (generatedReminderIDText != null)
            generatedReminderIDText.text = $"Reminder ID: {reminder.id}";

        ShowRelevantPanel();
    }

    public void OnClickJoinReminder()
    {
        string code = inputJoinReminderCode.text.Trim().ToUpper();

        if (code.Length != 6)
        {
            Debug.LogWarning("Reminder code must be 6 characters.");
            return;
        }

        bool success = ReminderManager.Instance.JoinReminder(code);
        if (success)
        {
            Debug.Log("Joined reminder successfully.");
            ShowRelevantPanel();
        }
        else
        {
            Debug.LogWarning("Reminder not found or is full.");
        }
    }
}
