using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using IdleRPG.Core;
using IdleRPG.Systems;
using IdleRPG.Data.Adventure;
using IdleRPG.Data.Shop;
using IdleRPG.Systems.Inventory;
using IdleRPG.UI.Shop;

using IdleRPG.UI.Core;
using IdleRPG.Core.ResourceManagement;
using System.Threading.Tasks;

namespace IdleRPG.Systems.Adventure
{
    public class AdventureManager : Singleton<AdventureManager>
    {
        [Header("Database (Auto-loaded via Addressables)")]
        [SerializeField] private AdventureDatabaseAsset database;

        public AdventureDatabaseAsset Database => database;

        public AdventureSceneData CurrentScene { get; private set; }
        public event Action<AdventureSceneData> OnSceneChanged;

        public bool IsInitialized { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            await LoadDatabaseAsync();
            IsInitialized = true;
            Debug.Log("[AdventureManager] Addressables database loaded.");
        }

        private async System.Threading.Tasks.Task LoadDatabaseAsync()
        {
            database = await AddressableManager.Instance.LoadAssetAsync<AdventureDatabaseAsset>("AdventureDatabase");
            
            if (database == null)
            {
#if UNITY_EDITOR
                Debug.Log("[AdventureManager] Addressables not ready, checking Assets/Data/Adventure via Editor load...");
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AdventureDatabaseAsset", new[] { "Assets/Data/Adventure" });
                if (guids.Length > 0)
                {
                    database = UnityEditor.AssetDatabase.LoadAssetAtPath<AdventureDatabaseAsset>(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
                }
#endif
            }

            if (database == null) Debug.LogWarning("[AdventureManager] AdventureDatabase failed to load!");
        }

        private void Start()
        {   
            Debug.Log("[AdventureManager] Start called.");
        }

        public string GetStartSceneId()
        {
            if (database != null && database.scenes.Count > 0)
            {
                return database.scenes[0].sceneId;
            }
            return null;
        }

        public void EnterScene(string sceneId)
        {   
            if (database == null) return;
            
            var scene = database.GetScene(sceneId);
            if (scene == null)
            {   
                Debug.LogError($"[Adventure] Scene not found in database: {sceneId}");
                return;
            }
            
            if (CurrentScene != null && CurrentScene.sceneId != sceneId)
            {
                StartCoroutine(EnterSceneWithTransition(scene));
            }
            else
            {
                ApplySceneChange(scene);
            }
        }

        private IEnumerator EnterSceneWithTransition(AdventureSceneData scene)
        {
            if (SceneLoader.Instance != null)
            {
                yield return SceneLoader.Instance.FadeToBlack();
            }
            else
            {
                yield return new WaitForSeconds(0.2f);
            }

            ApplySceneChange(scene);
            yield return new WaitForSeconds(0.1f);

            if (SceneLoader.Instance != null)
            {
                yield return SceneLoader.Instance.FadeIn();
            }
        }

        private void ApplySceneChange(AdventureSceneData scene)
        {
            CurrentScene = scene;
            Debug.Log($"[Adventure] Entering scene: {scene.sceneName} ({scene.sceneId})");
            OnSceneChanged?.Invoke(scene);

            if (!string.IsNullOrEmpty(scene.autoTriggerStoryId))
            {
                HandleTalk(scene.autoTriggerStoryId);
            }
        }

        public void ExecuteCommand(AdventureCommandData command)
        {   
            if (command == null) return;
            Debug.Log($"[Adventure] Executing command: {command.label} ({command.type})");

            switch (command.type)
            {
                case AdventureCommandType.Move: HandleMove(command.parameter); break;
                case AdventureCommandType.Talk: HandleTalk(command.parameter); break;
                case AdventureCommandType.Shop: HandleShop(command.parameter); break;
                case AdventureCommandType.Explore: HandleExplore(command.parameter); break;
                case AdventureCommandType.Battle: HandleBattle(command.parameter); break;
                case AdventureCommandType.System: HandleSystem(command.parameter); break;
            }
        }

        private void HandleMove(string targetSceneId) { EnterScene(targetSceneId); }

        private void HandleTalk(string storyId)
        {
            if (StoryManager.Instance != null)
            {
                StoryManager.Instance.StartStory(storyId);
                if (QuestManager.Instance != null) QuestManager.Instance.OnTalkedToNpc(storyId);
            }
            else Debug.LogError("[Adventure] StoryManager instance not found!");
        }

        private async void HandleShop(string shopAssetPath)
        {
            string[] parts = shopAssetPath.Split('|');
            string path = parts[0];
            string shopId = parts.Length > 1 ? parts[1] : "Default";

            string addressKey = path;
            if (path.Contains("/")) { string[] pathParts = path.Split('/'); addressKey = pathParts[pathParts.Length - 1]; }

            Debug.Log($"[Adventure] Attempting to load shop with key: {addressKey}");
            var shopAsset = await AddressableManager.Instance.LoadAssetAsync<ShopDatabaseAsset>(addressKey);
            
            if (shopAsset != null && UIManager.Instance != null)
            {
                UIManager.Instance.OpenPanel("ShopCanvas");
                var shopUI = UnityEngine.Object.FindAnyObjectByType<ShopUIController>(FindObjectsInactive.Include);
                if (shopUI != null) shopUI.OpenShop(shopAsset, shopId);
            }
        }

        private void HandleExplore(string parameter) { }
        private void HandleBattle(string parameter) { }
        private void HandleSystem(string parameter) { }
    }
}
