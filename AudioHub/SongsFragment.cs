using AndroidX.Fragment.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.FloatingActionButton;
using Android.Graphics.Drawables;
using System.Threading.Tasks;
using AndroidX.ConstraintLayout.Widget;
using YoutubeReExplode.Videos;

namespace AudioHub
{
    public class SongsFragment : Fragment
    {
        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.fragment_songs, container, false);
        }
        public override async void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            RecyclerView rv = view.FindViewById<RecyclerView>(Resource.Id.rvList);
            List<Song> songs = new List<Song>() {
                await SongManager.DownloadSong(VideoId.Parse("https://www.youtube.com/watch?v=7aS7KStPgNA"), null, default) };

            rv.SetAdapter(new ViewAdapter<Song>(songs, Resource.Layout.item_song, BindSongViewAdapter));
            rv.SetLayoutManager(new LinearLayoutManager(view.Context));
        }
        private static async void BindSongViewAdapter(RecyclerView.ViewHolder holder, Song song)
        {
            holder.ItemView.FindViewById<TextView>(Resource.Id.tvTitle).Text = song.title;
            holder.ItemView.FindViewById<TextView>(Resource.Id.tvArtist).Text = song.artist;
            holder.ItemView.FindViewById<TextView>(Resource.Id.tvDuration).Text = song.GetDurationString();

            Drawable thumbnail = await Drawable.CreateFromPathAsync($"{SongManager.SongDownloadDirectory}/{song.id}/Thumbnail.jpg");

            holder.ItemView.FindViewById<ImageView>(Resource.Id.imgThumbnail)
                .SetImageDrawable(thumbnail);

            holder.ItemView.FindViewById<FloatingActionButton>(Resource.Id.fabActions)
                .Click += (s, e) => ShowSongDialog(song, thumbnail);
        }
        private static void ShowSongDialog(Song song, Drawable thumbnail)
        {
            Android.App.Dialog dialog = new Android.App.Dialog(MainActivity.activity, Resource.Style.AppTheme);
            dialog.SetCancelable(true);
            dialog.SetCanceledOnTouchOutside(true);

            View view = LayoutInflater.From(MainActivity.activity).Inflate(Resource.Layout.dialog_songs_song, null);

            view.FindViewById<ImageView>(Resource.Id.imgThumbnail).SetImageDrawable(thumbnail);
            view.FindViewById<TextView>(Resource.Id.tvTitle).Text = song.title;
            view.FindViewById<TextView>(Resource.Id.tvArtist).Text = song.artist;
            view.FindViewById<TextView>(Resource.Id.tvDuration).Text = song.GetDurationString();

            view.FindViewById<Button>(Resource.Id.btnCancel).Click += (s, e) => { dialog.Dismiss(); };

            dialog.Window.Attributes.WindowAnimations = Resource.Style.Base_Animation_AppCompat_Dialog;
            dialog.SetContentView(view);
            dialog.Create();
            dialog.Show();
        }
    }
}