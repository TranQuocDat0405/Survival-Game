#if USE_FIREBASE && USE_FIREBASE_REMOTECONFIG 
using Firebase.Extensions;
using Firebase.RemoteConfig;
#endif
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace NFramework.FirebaseService
{
    public static class FirebaseRemoteConfigService
    {
        // Sự kiện này chỉ được kích hoạt trong nhánh code bọc trong #if USE_FIREBASE. Project
        // hiện không bật Firebase, nên trình biên dịch thấy nó "khai báo mà không ai gọi" và
        // cảnh báo CS0067 ở mỗi lần build. Tắt riêng đúng cảnh báo đó tại đây thay vì xoá sự
        // kiện đi, vì xoá là làm hỏng luôn phần Firebase cho những project khác dùng framework này.
#pragma warning disable 67
        public static event Action OnFetchSuccess;
#pragma warning restore 67

        public static bool IsInitialized { get; private set; }
        public static bool IsFetchSuccess { get; private set; }
        public static bool IsFetching { get; private set; }

        public static void Init(Dictionary<string, object> defaults)
        {
#if USE_FIREBASE && USE_FIREBASE_REMOTECONFIG
            FirebaseServiceManager.CheckAndTryInit(() =>
            {
                FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(defaults).ContinueWithOnMainThread(task =>
                {
                    IsInitialized = true;
                    FetchDataAsync();
                });
            });
#endif
        }

        public static Task FetchDataAsync()
        {
#if USE_FIREBASE && USE_FIREBASE_REMOTECONFIG
            if (IsFetching)
                return null;

            Debug.Log("[FirebaseRemoteConfigService] Fetching data...");
            IsFetching = true;
            var fetchTask = FirebaseRemoteConfig.DefaultInstance.FetchAsync(TimeSpan.Zero);
            return fetchTask.ContinueWithOnMainThread(FetchComplete);
#else
            return null;
#endif
        }

        private static void FetchComplete(Task fetchTask)
        {
#if USE_FIREBASE && USE_FIREBASE_REMOTECONFIG
            if (!fetchTask.IsCompleted)
            {
                Debug.LogError("[FirebaseRemoteConfigService] Retrieval hasn't finished.");
                return;
            }

            IsFetching = false;
            var remoteConfig = FirebaseRemoteConfig.DefaultInstance;
            var info = remoteConfig.Info;
            if (info.LastFetchStatus != LastFetchStatus.Success)
            {
                Debug.LogError($"[FirebaseRemoteConfigService] Fetch was unsuccessful\n{nameof(info.LastFetchStatus)}: {info.LastFetchStatus}");
                IsFetchSuccess = false;
                return;
            }

            // Fetch successful. Parameter values must be activated to use.
            remoteConfig.ActivateAsync().ContinueWithOnMainThread(task =>
            {
                Debug.Log($"[FirebaseRemoteConfigService] Remote data loaded and ready for use. Last fetch time {info.FetchTime}.");
                IsFetchSuccess = true;
                OnFetchSuccess?.Invoke();
            });
#endif
        }
    }

    // GameRemoteConfig should override this class
    public class RemoteConfig : MonoBehaviour
    {
        [SerializeField] protected bool _autoInitAtStart = true;

        protected virtual void Start()
        {
            if (_autoInitAtStart)
            {
                FirebaseRemoteConfigService.Init(GetDefaultsDic());
                FirebaseRemoteConfigService.OnFetchSuccess += FirebaseRemoteConfigService_OnFetchSuccess;
                InternetChecker.OnStatusChanged += InternetChecker_OnStatusChanged;
            }
        }

        protected virtual void OnDestroy()
        {
            FirebaseRemoteConfigService.OnFetchSuccess -= FirebaseRemoteConfigService_OnFetchSuccess;
            InternetChecker.OnStatusChanged -= InternetChecker_OnStatusChanged;
        }

        private void InternetChecker_OnStatusChanged(bool status)
        {
            if (status && !FirebaseRemoteConfigService.IsFetchSuccess)
                FirebaseRemoteConfigService.FetchDataAsync();
        }

        /// <summary>
        /// Get default local values 
        /// ex: { "inter_start_level", InterStartLevel }
        /// </summary>
        protected virtual Dictionary<string, object> GetDefaultsDic() => new Dictionary<string, object>();

        /// <summary>
        /// Set local values after fetch success
        /// ex: InterStartLevel = (int)FirebaseRemoteConfig.DefaultInstance.GetValue("inter_start_level").DoubleValue;
        /// </summary>
        protected virtual void FirebaseRemoteConfigService_OnFetchSuccess() { }

    }
}