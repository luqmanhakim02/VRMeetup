using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Services.Authentication;

public class ReminderManager : MonoBehaviour
{
    public static ReminderManager Instance;

    private const string GLOBAL_KEY = "GlobalReminders";
    private const string USER_KEY_PREFIX = "UserReminders_";
    private const string ALL_USERS_KEY = "AllUsers";

    private Dictionary<string, MeetingReminder> globalReminders = new();
    private List<string> joinedReminders = new();
    private string userId => AuthenticationService.Instance.PlayerId;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private IEnumerator Start()
    {
        while (!AuthenticationService.Instance.IsSignedIn)
            yield return null;

        LoadGlobalReminders();
        LoadUserReminders();
    }

    public MeetingReminder CreateReminder(string name, int max, DateTime dt)
    {
        string id = GenerateUniqueID();
        var reminder = new MeetingReminder
        {
            id = id,
            meetingName = name,
            maxParticipants = max,
            dateTime = dt.ToString("o"),
            participants = new List<string> { userId }
        };

        globalReminders[id] = reminder;
        joinedReminders.Add(id);

        AddUserToMasterList(userId);
        SaveGlobalReminders();
        SaveUserReminders();

        return reminder;
    }

    public bool JoinReminder(string id)
    {
        if (!globalReminders.ContainsKey(id)) return false;

        var reminder = globalReminders[id];
        if (reminder.participants.Contains(userId)) return true;
        if (reminder.participants.Count >= reminder.maxParticipants) return false;

        reminder.participants.Add(userId);
        joinedReminders.Add(id);

        AddUserToMasterList(userId);
        SaveGlobalReminders();
        SaveUserReminders();
        return true;
    }

    public bool LeaveReminder(string id)
    {
        if (joinedReminders.Contains(id))
        {
            joinedReminders.Remove(id);

            if (globalReminders.TryGetValue(id, out var reminder))
            {
                reminder.participants.Remove(userId);
            }

            SaveGlobalReminders();
            SaveUserReminders();
            return true;
        }
        return false;
    }

    public bool DeleteReminder(string id)
    {
        if (globalReminders.TryGetValue(id, out var reminder))
        {
            if (reminder.participants.Count > 0 && reminder.participants[0] == userId)
            {
                globalReminders.Remove(id);

                var allUsers = GetAllUserIds();
                foreach (var uid in allUsers)
                {
                    string key = USER_KEY_PREFIX + uid;
                    if (PlayerPrefs.HasKey(key))
                    {
                        var json = PlayerPrefs.GetString(key);
                        var wrapper = JsonUtility.FromJson<IdListWrapper>(json);
                        if (wrapper?.ids != null && wrapper.ids.Contains(id))
                        {
                            wrapper.ids.Remove(id);
                            PlayerPrefs.SetString(key, JsonUtility.ToJson(wrapper));
                        }
                    }
                }

                SaveGlobalReminders();
                PlayerPrefs.Save();
                return true;
            }
        }
        return false;
    }

    public List<MeetingReminder> GetMyReminders()
    {
        return joinedReminders
            .Where(globalReminders.ContainsKey)
            .Select(id => globalReminders[id])
            .ToList();
    }

    private void LoadGlobalReminders()
    {
        globalReminders.Clear();
        if (PlayerPrefs.HasKey(GLOBAL_KEY))
        {
            var json = PlayerPrefs.GetString(GLOBAL_KEY);
            var wrapper = JsonUtility.FromJson<ReminderListWrapper>(json);
            if (wrapper?.reminders != null)
                foreach (var r in wrapper.reminders)
                    globalReminders[r.id] = r;
        }
    }

    private void SaveGlobalReminders()
    {
        var list = globalReminders.Values.ToList();
        var json = JsonUtility.ToJson(new ReminderListWrapper { reminders = list });
        PlayerPrefs.SetString(GLOBAL_KEY, json);
        PlayerPrefs.Save();
    }

    private void LoadUserReminders()
    {
        joinedReminders.Clear();
        string key = USER_KEY_PREFIX + userId;
        if (PlayerPrefs.HasKey(key))
        {
            var json = PlayerPrefs.GetString(key);
            var wrapper = JsonUtility.FromJson<IdListWrapper>(json);
            if (wrapper?.ids != null)
                joinedReminders = wrapper.ids;
        }
    }

    private void SaveUserReminders()
    {
        string key = USER_KEY_PREFIX + userId;
        var json = JsonUtility.ToJson(new IdListWrapper { ids = joinedReminders });
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save();
    }

    private void AddUserToMasterList(string uid)
    {
        var list = GetAllUserIds();
        if (!list.Contains(uid))
        {
            list.Add(uid);
            SaveAllUserIds(list);
        }
    }

    private List<string> GetAllUserIds()
    {
        if (PlayerPrefs.HasKey(ALL_USERS_KEY))
        {
            var json = PlayerPrefs.GetString(ALL_USERS_KEY);
            var wrapper = JsonUtility.FromJson<IdListWrapper>(json);
            return wrapper?.ids ?? new List<string>();
        }
        return new List<string>();
    }

    private void SaveAllUserIds(List<string> ids)
    {
        var json = JsonUtility.ToJson(new IdListWrapper { ids = ids });
        PlayerPrefs.SetString(ALL_USERS_KEY, json);
        PlayerPrefs.Save();
    }

    private string GenerateUniqueID()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        System.Random rand = new();
        string code;
        do
        {
            code = new string(Enumerable.Repeat(chars, 6).Select(s => s[rand.Next(s.Length)]).ToArray());
        } while (globalReminders.ContainsKey(code));
        return code;
    }

    [Serializable] private class ReminderListWrapper { public List<MeetingReminder> reminders; }
    [Serializable] private class IdListWrapper { public List<string> ids; }
}