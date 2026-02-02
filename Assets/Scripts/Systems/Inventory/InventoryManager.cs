using UnityEngine;
using System.Collections.Generic;
using IdleRPG.Core; 
using System.Linq;
using IdleRPG.Systems;
using IdleRPG.Data.Equipment;
using IdleRPG.Data.Items;
using IdleRPG.Core.ResourceManagement;
using System.Threading.Tasks;

namespace IdleRPG.Systems.Inventory
{
    public enum EquipmentSlot
    {
        Weapon1,    // 主手/武器1
        Weapon2,    // 副手/武器2
        Helmet,     // 头盔
        Armor,      // 铠甲/胸甲
        Shoulder,   // 护肩
        Necklace,   // 项链
        Ring1,      // 戒指1
        Ring2,      // 戒指2
        Pants,      // 裤子/护腿
        Boots,      // 鞋子
        Accessory1, // 饰品1
        Accessory2  // 饰品2
    }

    /// <summary>
    /// 物品栏存档处理器接口，方便未来切换到数据库或云端
    /// </summary>
    public interface IInventorySaveHandler
    {
        void Save(InventorySaveData data);
        InventorySaveData Load();
    }

    /// <summary>
    /// 当前使用的 JSON + PlayerPrefs 实现
    /// </summary>
    public class PlayerPrefsJsonSaveHandler : IInventorySaveHandler
    {
        private const string SAVE_KEY = "Inventory_Data_JSON";

        public void Save(InventorySaveData data)
        {
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
            Debug.Log("[SaveHandler] Data saved to PlayerPrefs.");
        }

        public InventorySaveData Load()
        {
            if (PlayerPrefs.HasKey(SAVE_KEY))
            {
                string json = PlayerPrefs.GetString(SAVE_KEY);
                return JsonUtility.FromJson<InventorySaveData>(json);
            }
            return null;
        }
    }

    [System.Serializable]
    public class InventorySaveData
    {
        public int Gold;
        public int Diamond;
        public List<InventoryItem> InventoryItems;
        public List<EquippedItemSaveData> EquippedItems;

        public InventorySaveData()
        {
            InventoryItems = new List<InventoryItem>();
            EquippedItems = new List<EquippedItemSaveData>();
        }
    }

    [System.Serializable]
    public class EquippedItemSaveData
    {
        public EquipmentSlot Slot;
        public InventoryItem Item;
    }

    public class InventoryManager : Singleton<InventoryManager>
    {
        [Header("Databases (Auto-loaded via Addressables)")]
        public EquipmentDatabase equipmentDatabase;
        public SetDatabase setDatabase;
        public ItemDatabase itemDatabase;

        public bool IsInitialized { get; private set; }

        public SetTemplate GetSetDefinition(string setId)
        {
            if (setDatabase == null || string.IsNullOrEmpty(setId)) return null;
            return setDatabase.GetSet(setId);
        }

        public Sprite GetItemIcon(string itemId)
        {
            // 1. Check Equipment
            if (equipmentDatabase != null)
            {
                var template = equipmentDatabase.GetTemplate(itemId);
                if (template != null && template.icon != null) return template.icon;
            }

            // 2. Check General Items (Material/Consumable)
            if (itemDatabase != null)
            {
                var template = itemDatabase.GetTemplate(itemId);
                if (template != null && template.icon != null) return template.icon;
            }

            return null;
        }

        public string GetItemName(string itemId)
        {
            if (equipmentDatabase != null)
            {
                var template = equipmentDatabase.GetTemplate(itemId);
                if (template != null) return template.equipmentName;
            }

            if (itemDatabase != null)
            {
                var template = itemDatabase.GetTemplate(itemId);
                if (template != null) return template.itemName;
            }

            return itemId;
        }

        public string GetItemDescription(string itemId)
        {
            if (equipmentDatabase != null)
            {
                var template = equipmentDatabase.GetTemplate(itemId);
                if (template != null) return template.description;
            }

            if (itemDatabase != null)
            {
                var template = itemDatabase.GetTemplate(itemId);
                if (template != null) return template.description;
            }

            return "";
        }

        private List<InventoryItem> _inventory = new List<InventoryItem>();
        private Dictionary<EquipmentSlot, InventoryItem> _equipment = new Dictionary<EquipmentSlot, InventoryItem>();
        private IInventorySaveHandler _saveHandler;

        public int Gold { get; private set; }
        public int Diamond { get; private set; }
        public event System.Action<int> OnGoldChanged;
        public event System.Action<int> OnDiamondChanged;
        public event System.Action OnInventoryChanged;

        protected override void Awake()
        {
            base.Awake();
            _saveHandler = new PlayerPrefsJsonSaveHandler();
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            await LoadDatabasesAsync();
            LoadData(); 
            IsInitialized = true;
            Debug.Log("[InventoryManager] Addressables databases loaded and initialized.");
            
            // 通知 UI 刷新，因为数据可能是在 UI 打开后才加载完成的
            OnInventoryChanged?.Invoke();
            OnGoldChanged?.Invoke(Gold);
            OnDiamondChanged?.Invoke(Diamond);
        }

        private async Task LoadDatabasesAsync()
        {
            // 直接使用 await，不需要 Task.Run，因为 Addressables 本身就是异步的且必须在主线程发起请求
            equipmentDatabase = await AddressableManager.Instance.LoadAssetAsync<EquipmentDatabase>("EquipmentDatabase");
            itemDatabase = await AddressableManager.Instance.LoadAssetAsync<ItemDatabase>("ItemDatabase");
            setDatabase = await AddressableManager.Instance.LoadAssetAsync<SetDatabase>("SetDatabase");

            // 2. 编辑器备份加载
            if (equipmentDatabase == null || itemDatabase == null || setDatabase == null)
            {
#if UNITY_EDITOR
                Debug.Log("[InventoryManager] Addressables not ready, checking Assets/Data via Editor load...");
                if (equipmentDatabase == null)
                {
                    string[] guids = UnityEditor.AssetDatabase.FindAssets("t:EquipmentDatabase", new[] { "Assets/Data/Equipment" });
                    if (guids.Length > 0) equipmentDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<EquipmentDatabase>(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
                }
                if (itemDatabase == null)
                {
                    string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ItemDatabase", new[] { "Assets/Data/Items" });
                    if (guids.Length > 0) itemDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemDatabase>(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
                }
                if (setDatabase == null)
                {
                    string[] guids = UnityEditor.AssetDatabase.FindAssets("t:SetDatabase", new[] { "Assets/Data/Equipment" });
                    if (guids.Length > 0) setDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<SetDatabase>(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
                }
#endif
            }

            // 兜底检查
            if (equipmentDatabase == null) Debug.LogWarning("[InventoryManager] EquipmentDatabase failed to load!");
            if (itemDatabase == null) Debug.LogWarning("[InventoryManager] ItemDatabase failed to load!");
            if (setDatabase == null) Debug.LogWarning("[InventoryManager] SetDatabase failed to load!");
        }

        private void Start()
        {
            // Start 逻辑移到了 InitializeAsync 中调用 LoadData
            OnGoldChanged?.Invoke(Gold);
        }

        #region Persistence (Save/Load)

        public void SaveData()
        {
            InventorySaveData saveData = new InventorySaveData();
            saveData.Gold = Gold;
            saveData.Diamond = Diamond;
            saveData.InventoryItems = _inventory;
            
            foreach (var kvp in _equipment)
            {
                saveData.EquippedItems.Add(new EquippedItemSaveData { Slot = kvp.Key, Item = kvp.Value });
            }

            _saveHandler.Save(saveData);
        }

        public void LoadData()
        {
            InventorySaveData saveData = _saveHandler.Load();

            if (saveData != null)
            {
                Gold = saveData.Gold;
                Diamond = saveData.Diamond;
                // Note: Rest of loading logic...
            }
        }
        #endregion
    }
}
