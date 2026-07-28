using UnityEngine;
using YARG.Menu.Persistent;

namespace YARG.Menu.Career.Motivation
{
    /// <summary>
    /// Bootstraps the motivation system. Creates a persistent MomentumTracker
    /// that survives scene loads. The individual UI components (CampaignStatsDisplay,
    /// GigProgressEnhancer, TeaserPanel) are self-attached by the hooks in
    /// CareerView.cs and GigView.cs when they first activate.
    ///
    /// Attach this to a GameObject in the PersistentScene or any scene that
    /// outlives menu navigation.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class MotivationSystemInjector : MonoBehaviour
    {
        private static MotivationSystemInjector _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            // Create the persistent MomentumTracker
            var trackerObj = new GameObject("MomentumTracker");
            trackerObj.transform.SetParent(transform);
            trackerObj.AddComponent<MomentumTracker>();
            DontDestroyOnLoad(trackerObj);

            Debug.Log("MotivationSystemInjector: Initialized momentum tracker");
        }
    }
}