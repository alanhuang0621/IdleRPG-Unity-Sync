using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using IdleRPG.Systems.Characters;
using IdleRPG.Data.Characters;

namespace IdleRPG.Core
{
    /// <summary>
    /// 战斗管理器，负责控制战斗流程、场景切换和结果结算
    /// 位于 CorePersistentScene (CPS) 中
    /// </summary>
    public class BattleManager : Singleton<BattleManager>
    {
        [Header("Battle Settings")]
        public string BattleSceneName = "BattleScene";
        public string MainGameSceneName = "GameScene";

        private bool _isBattleActive = false;
        public bool IsBattleActive => _isBattleActive;

        /// <summary>
        /// 进入战斗
        /// </summary>
        /// <param name="enemyTemplates">要生成的敌人模板列表</param>
        public void EnterBattle(List<CharacterTemplate> enemyTemplates)
        {
            if (_isBattleActive) return;
            
            Debug.Log("BattleManager: Entering Battle...");
            _isBattleActive = true;
            
            // 存储敌人信息（可以用于在战斗场景初始化敌人）
            // TODO: 这里可以先存到一个静态列表或者传参给战斗场景初始化逻辑
            
            // 使用 SceneLoader 切换到战斗场景
            SceneLoader.Instance.LoadScene(BattleSceneName, true);
        }

        /// <summary>
        /// 退出战斗，返回主场景
        /// </summary>
        public void ExitBattle()
        {
            if (!_isBattleActive) return;

            Debug.Log("BattleManager: Exiting Battle...");
            _isBattleActive = false;

            // 使用 SceneLoader 返回主场景
            SceneLoader.Instance.LoadScene(MainGameSceneName, true);
        }

        /// <summary>
        /// 战斗结算
        /// </summary>
        public void FinishBattle(bool playerWon)
        {
            Debug.Log($"Battle Finished. Player Won: {playerWon}");
            // TODO: 处理奖励、经验、掉落等逻辑
            
            ExitBattle();
        }
    }
}
