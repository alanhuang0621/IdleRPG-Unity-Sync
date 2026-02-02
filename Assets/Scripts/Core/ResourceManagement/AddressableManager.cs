using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading.Tasks;
using System.Collections.Generic;
using IdleRPG.Core;

namespace IdleRPG.Core.ResourceManagement
{
    /// <summary>
    /// Addressable 资源加载管理器
    /// 提供异步加载资源的基础框架
    /// </summary>
    public class AddressableManager : Singleton<AddressableManager>
    {
        private Dictionary<string, AsyncOperationHandle> _loadedHandles = new Dictionary<string, AsyncOperationHandle>();

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="address">Addressable 地址</param>
        /// <returns>资源实例</returns>
        public async Task<T> LoadAssetAsync<T>(string address) where T : UnityEngine.Object
        {
            if (_loadedHandles.TryGetValue(address, out var existingHandle))
            {
                if (existingHandle.IsDone)
                {
                    return existingHandle.Result as T;
                }
            }

            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(address);
            _loadedHandles[address] = handle;

            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                return handle.Result;
            }
            else
            {
                Debug.LogError($"[AddressableManager] Failed to load asset at address: {address}");
                return null;
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="address">Addressable 地址</param>
        public void ReleaseAsset(string address)
        {
            if (_loadedHandles.TryGetValue(address, out var handle))
            {
                Addressables.Release(handle);
                _loadedHandles.Remove(address);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            // 清理所有加载的资源
            foreach (var handle in _loadedHandles.Values)
            {
                Addressables.Release(handle);
            }
            _loadedHandles.Clear();
        }
    }
}
