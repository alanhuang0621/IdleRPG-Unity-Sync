using UnityEngine;
using System.Collections.Generic;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Data.Characters;
using IdleRPG.Core.ResourceManagement;
using System.Threading.Tasks;

namespace IdleRPG.Systems.Characters
{
    public class CharacterManager : Singleton<CharacterManager>
    {
        [Header("Databases (Auto-loaded via Addressables)")]
        [SerializeField] private CharacterDatabase characterDatabase;

        public bool IsInitialized { get; private set; }
        private List<CharacterInstance> ownedCharacters = new List<CharacterInstance>();
        public List<CharacterInstance> OwnedCharacters => ownedCharacters;

        protected override void Awake()
        {
            base.Awake();
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            await LoadDatabaseAsync();
            IsInitialized = true;
            SyncFromDataManager();
        }

        private async Task LoadDatabaseAsync()
        {
            Debug.Log("[CharacterManager] Loading CharacterDatabase via Addressables...");
            characterDatabase = await AddressableManager.Instance.LoadAssetAsync<CharacterDatabase>("CharacterDatabase");
            if (characterDatabase == null) Debug.LogError("[CharacterManager] Failed to load CharacterDatabase!");
        }

        private void Start()
        {
            if (IsInitialized) SyncFromDataManager();
        }

        private void SyncFromDataManager()
        {
            if (DataManager.Instance.CurrentSave != null) ownedCharacters = DataManager.Instance.CurrentSave.ownedCharacters;
        }

        public void AddCharacter(string templateId)
        {
            var template = characterDatabase.GetTemplate(templateId);
            if (template == null) return;
            var newInstance = new CharacterInstance(templateId);
            ownedCharacters.Add(newInstance);
            DataManager.Instance.SaveGame();
        }

        public CharacterInstance GetCharacter(string instanceId) => ownedCharacters.Find(c => c.instanceId == instanceId);
        public CharacterTemplate GetTemplate(string templateId) => characterDatabase.GetTemplate(templateId);
    }
}
