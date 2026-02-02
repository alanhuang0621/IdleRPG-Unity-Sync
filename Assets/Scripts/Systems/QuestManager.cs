using UnityEngine;
using System.Collections.Generic;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Systems.Inventory;
using IdleRPG.UI.Notification;

namespace IdleRPG.Systems
{
    public class QuestManager : Singleton<QuestManager>
    {
        public event System.Action OnQuestUpdated;

        public class ActiveQuest
        {
            public QuestData data;
            public int currentAmount;
            public bool isCompleted;
        }

        private List<ActiveQuest> _activeQuests = new List<ActiveQuest>();
        private List<string> _completedQuestIds = new List<string>();

        public IReadOnlyList<ActiveQuest> ActiveQuests => _activeQuests;
        public IReadOnlyList<string> CompletedQuestIds => _completedQuestIds;

        public void AcceptQuest(string questId)
        {
            if (IsQuestCompleted(questId) || IsQuestActive(questId)) return;
            QuestData data = DataManager.Instance.GetQuest(questId);
            if (data == null) return;

            ActiveQuest newQuest = new ActiveQuest { data = data, currentAmount = 0, isCompleted = false };
            _activeQuests.Add(newQuest);
            
            if (NotificationManager.Instance != null)
                NotificationManager.Instance.ShowNotification(data.title, NotificationPopup.NotificationType.QuestAccept);

            GameStateManager.Instance.SetVariable($"quest_{questId}_accepted", "true");
            OnQuestUpdated?.Invoke();
        }

        public bool IsQuestActive(string questId) => _activeQuests.Exists(q => q.data.id == questId);
        public bool IsQuestCompleted(string questId) => _completedQuestIds.Contains(questId);
        public void OnTalkedToNpc(string npcId) { }
    }
}
