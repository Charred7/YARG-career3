using System.Collections.Generic;
using UnityEngine;
using YARG.Core.Song;
using YARG.Menu.MusicLibrary;
using YARG.Playlists;

namespace YARG.Menu.Career
{
    /// <summary>
    /// Opens the MusicLibraryMenu in playlist mode for a career gig.
    /// Uses the public API only — no reflection.
    /// </summary>
    public static class MusicLibraryWrapper
    {
        private static Playlist _activeGigPlaylist;
        private static bool _pendingGigOpen;

        public static bool IsInGigMode => _activeGigPlaylist != null;

        public static void OpenGigInMusicLibrary(string gigName, List<SongEntry> songs)
        {
            if (songs == null || songs.Count == 0)
            {
                Debug.LogError("MusicLibraryWrapper: Cannot open gig with no songs.");
                return;
            }

            // Build ephemeral playlist from resolved songs
            var playlist = new Playlist(true) { Name = gigName };
            foreach (var song in songs)
            {
                playlist.SongHashes.Add(song.Hash);
            }

            _activeGigPlaylist = playlist;
            _pendingGigOpen = true;

            // Tell MusicLibraryMenu to do a Partial reload on next OnEnable
            MusicLibraryMenu.SetReload(MusicLibraryReloadState.Partial);

            // Push normally — let Awake run in default Library mode.
            // OnEnable will fire right after Awake on first activation,
            // or immediately on subsequent activations.
            var menuObject = MenuManager.Instance.PushMenu(MenuManager.Menu.MusicLibrary);
            var musicLibrary = menuObject.GetComponent<MusicLibraryMenu>();

            if (musicLibrary == null)
            {
                Debug.LogError("MusicLibraryWrapper: MusicLibraryMenu component not found!");
                _activeGigPlaylist = null;
                _pendingGigOpen = false;
                return;
            }

            // If OnEnable already ran (which it did — SetActive(true) fires both
            // Awake and OnEnable synchronously), the menu is now showing Library mode
            // because Awake ran first with default state. We need to force it into
            // playlist mode now and refresh.
            musicLibrary.SelectedPlaylist = playlist;
            musicLibrary.MenuState = MenuState.Playlist;

            // Force a re-push of the nav scheme and view list by toggling the menu.
            // SetActive(false) triggers OnDisable which saves our SelectedPlaylist.
            // SetActive(true) triggers OnEnable which sees Partial + SelectedPlaylist.
            MusicLibraryMenu.SetReload(MusicLibraryReloadState.Partial);
            menuObject.gameObject.SetActive(false);
            menuObject.gameObject.SetActive(true);

            _pendingGigOpen = false;

            // Attach the back interceptor
            var go = menuObject.gameObject;
            var interceptor = go.GetComponent<GigBackInterceptor>();
            if (interceptor == null)
            {
                interceptor = go.AddComponent<GigBackInterceptor>();
            }
            interceptor.Initialize(musicLibrary);

            Debug.Log($"MusicLibraryWrapper: Opened gig '{gigName}' with {songs.Count} songs.");
        }

        internal static void ClearGigMode()
        {
            _activeGigPlaylist = null;
            _pendingGigOpen = false;
        }
    }

    /// <summary>
    /// Detects when Back() exits playlist mode and pops the whole menu
    /// to return to GigView.
    /// </summary>
    internal class GigBackInterceptor : MonoBehaviour
    {
        private MusicLibraryMenu _menu;
        private bool _active;

        public void Initialize(MusicLibraryMenu menu)
        {
            _menu = menu;
            _active = true;
        }

        private void Update()
        {
            if (!_active || _menu == null)
            {
                return;
            }

            if (_menu.MenuState != MenuState.Playlist)
            {
                _active = false;
                MusicLibraryWrapper.ClearGigMode();
                MenuManager.Instance.PopMenu();
            }
        }

        private void OnDisable()
        {
            _active = false;
            MusicLibraryWrapper.ClearGigMode();
        }
    }
}