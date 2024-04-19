using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Widget;
using Google.Android.Material.BottomNavigation;
using Xamarin.Essentials;
using System.Collections.Generic;
using Android.Content.PM;
using AndroidX.Fragment.App;
using Android.Media.Session;
using Android.Content;
using Android.Media;
using Google.Android.Material.Navigation;
using Android.Views;
using AndroidX.Core.App;
using Android;
using AndroidX.Activity;
using Android.Bluetooth;
using YoutubeReExplode.Videos;
using YoutubeReExplode.Playlists;
using System.IO;
using Android.Content.Res;
using System;

namespace AudioHub
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true,
        ScreenOrientation = ScreenOrientation.Portrait, LaunchMode = LaunchMode.SingleInstance)]
    [IntentFilter(new[] { "android.intent.action.SEND" }, Categories = new[] { "android.intent.category.DEFAULT" },
        DataMimeType = "text/*")]
    public class MainActivity : FragmentActivity
    {
        private readonly Dictionary<int, AndroidX.Fragment.App.Fragment> fragments = new Dictionary<int, AndroidX.Fragment.App.Fragment>
        {
            { Resource.Id.navigation_listen, new ListenFragment() },
            { Resource.Id.navigation_songs, new SongsFragment() },
            { Resource.Id.navigation_manage, new ManageFragment() },
        };

        private int currentPage;
        private BottomNavigationView bNavView;
        public Random shuffleSeed = new Random();

        public static MainActivity activity;

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            activity = this;

            base.OnCreate(savedInstanceState);
            Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            bNavView = FindViewById<BottomNavigationView>(Resource.Id.bnvNavigation);
            bNavView.ItemSelected += BNavView_ItemSelected;

            MessageListener.readMessages = true;
            SwitchPage(Resource.Id.navigation_listen);

            await Permissions.RequestAsync<Permissions.StorageRead>();
            await Permissions.RequestAsync<Permissions.StorageWrite>();

            VerifyDirectory(SongManager.SongDownloadDirectory);
            VerifyDirectory(SongManager.SongCacheDirectory);
            VerifyDirectory(SongManager.ThumbnailCacheDirectory);
            VerifyDirectory(PlaylistManager.PlaylistDirectory);
            VerifyDirectory($"storage/emulated/0/Documents/Debug");
            VerifyDirectory(SongManager.SongExportDirectory);

            AppDomain.CurrentDomain.UnhandledException += UnhandledException;

            SongManager.ClearCachedThumbnails();
            SongManager.ClearCachedSongs();

            NotificationChannel nc = new NotificationChannel("Controls", "Controls", NotificationImportance.None);
            nc.LockscreenVisibility = NotificationVisibility.Public;

            NotificationManager nm = GetSystemService(NotificationService) as NotificationManager;
            nm.CreateNotificationChannel(nc);

            SongPlayer.Init();
            OnNewIntent(Intent);

            ISharedPreferences prefs = GetSharedPreferences("AudioHub", FileCreationMode.Private);
            string prevSongId = prefs.GetString("Song", "");
            string prevPlaylistTitle = prefs.GetString("Playlist", "");

            if (!string.IsNullOrEmpty(prevSongId) && !string.IsNullOrEmpty(prevPlaylistTitle))
            {
                SongPlayer.Play(SongManager.GetSongById(prevSongId), PlaylistManager.GetPlaylistByTitle(prevPlaylistTitle));
                SongPlayer.Pause(true);
            }

            if (prefs.GetBoolean("Shuffle", false)) SongPlayer.ToggleShuffle();
        }
        public void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            File.WriteAllText($"storage/emulated/0/Documents/Debug/Debug{DateTime.Now.Ticks}.txt", ex.Message + ex.StackTrace);

            Toast.MakeText(BaseContext, ex.Message, ToastLength.Long).Show();
        }
        protected override void OnNewIntent(Intent intent)
        {
            string link = intent.GetStringExtra(Intent.ExtraText);
            if (string.IsNullOrEmpty(link)) return;

            int id = Resource.Id.navigation_songs;
            SongsFragment f = fragments[id] as SongsFragment;

            SwitchPage(id);
            f.SetLink(link);
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();

            GetSharedPreferences("AudioHub", FileCreationMode.Private).Edit()
                .PutString("Song", SongPlayer.currentSong.id)
                .PutString("Playlist", SongPlayer.currentPlaylist.title)
                .PutBoolean("Shuffle", SongPlayer.shuffle).Commit();

            AudioManager am = GetSystemService(AudioService) as AudioManager;
            am.AbandonAudioFocusRequest(SongPlayer.audioFocusRequest);

            StopService(new Intent(this, typeof(NextSongService)));

            SongManager.ClearCachedThumbnails();
            SongManager.ClearCachedSongs();

            NextSongService.initialized = false;
            SongPlayer.Cleanup();
        }
        private void BNavView_ItemSelected(object sender, NavigationBarView.ItemSelectedEventArgs e)
        {
            SwitchPage(e.Item.ItemId);
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
        public void SwitchPage(int newPage)
        {
            if (currentPage == newPage || string.IsNullOrEmpty(Resources.GetResourceName(newPage))) return;

            try
            {
                AndroidX.Fragment.App.FragmentTransaction ft = SupportFragmentManager.BeginTransaction();
                ft.Replace(Resource.Id.flFragment, fragments[newPage]);
                ft.SetTransition(AndroidX.Fragment.App.FragmentTransaction.TransitFragmentFade);
                ft.Commit();

                currentPage = newPage;
                bNavView.SelectedItemId = newPage;
            }
            catch (Exception e)
            {
                UnhandledException(this, new UnhandledExceptionEventArgs(e, false));
            }
        }
        public static void ShowDialog(int layoutResID, Stack<Dialog> stack, Action<Dialog, View> bindViewAction)
        {
            Dialog dialog = new Dialog(activity, Resource.Style.AppTheme);
            stack?.Push(dialog);

            dialog.SetCancelable(true);
            dialog.SetCanceledOnTouchOutside(true);

            View view = LayoutInflater.From(activity).Inflate(layoutResID, null);
            bindViewAction(dialog, view);

            dialog.Window.Attributes.WindowAnimations = Resource.Style.Base_Animation_AppCompat_Dialog;
            dialog.SetContentView(view);
            dialog.Create();
            dialog.Show();
        }
        public static void VerifyDirectory(string directory)
        {
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        }
        public static void UpdateService()
        {
            activity.StartForegroundService(new Intent(activity, typeof(NextSongService)));
        }
    }
}

