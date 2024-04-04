using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using YoutubeReExplode;
using YoutubeReExplode.Common;
using YoutubeReExplode.Search;
using YoutubeReExplode.Videos;
using Java.Net;
using System.IO;
using System.Threading.Tasks;
using YoutubeReExplode.Videos.Streams;
using YoutubeReExplode.Channels;
using System.Xml.Serialization;
using System.Xml;

namespace AudioHub
{
    public static class SongManager
    {
        private static readonly Lazy<YoutubeClient> ytClient = new Lazy<YoutubeClient>();
        private static readonly Lazy<System.Net.WebClient> webClient = new Lazy<System.Net.WebClient>();
        public static string SongDownloadDirectory => $"{MainActivity.activity.GetExternalFilesDir(null).AbsolutePath}/Songs";
        public static string ThumbnailCacheDirectory => $"{MainActivity.activity.GetExternalCacheDirs().First().AbsolutePath}/Thumbnails";
        public static string SongCacheDirectory => $"{MainActivity.activity.GetExternalCacheDirs().First().AbsolutePath}/Songs";
        public static string SongExportDirectory => "/storage/emulated/0/Music/AudioHub";
        public static string lastSearchQuery;

        public static Song GetSongFromVideo(Video video)
        {
            return new Song(video.Id, video.Title, video.Author.ChannelName ?? video.Author.ChannelTitle,
                (int)Math.Ceiling(video.Duration.Value.TotalSeconds));
        }
        public static Song GetSongFromVideo(VideoSearchResult video)
        {
            return new Song(video.Id, video.Title, video.Author.ChannelName ?? video.Author.ChannelTitle,
                (int)Math.Ceiling(video.Duration.Value.TotalSeconds));
        }
        public static Song GetUpdatedSong(Song song)
        {
            return new Song
            {
                id = song.id,
                durationSecs = song.durationSecs,
                artist = Song.GetSimpleArtist(song.fullTitle, song.fullArtist),
                title = Song.GetSimpleTitle(song.fullTitle),
                fullArtist = song.fullArtist,
                fullTitle = song.fullTitle,
            };
        }
        public static async Task<Song> GetSongFromVideo(VideoId videoId)
        {
            return GetSongFromVideo(await ytClient.Value.Videos.GetAsync(videoId));
        }
        public static async Task<List<Song>> SearchForSongs(string query, CancellationToken cancellationToken)
        {
            if (query != lastSearchQuery)
            {
                ClearCachedThumbnails();
                lastSearchQuery = query;
            }

            List<Song> songs = new List<Song>();

            IAsyncEnumerator<Batch<ISearchResult>> results = TryGetResult(() => ytClient.Value.Search.
                GetResultBatchesAsync(query, SearchFilter.Video, cancellationToken).GetAsyncEnumerator(cancellationToken),
                () => null, out bool fail);

            if (fail) return songs;
            await results.MoveNextAsync();

            foreach (ISearchResult result in results.Current.Items)
            {
                if (!(result is VideoSearchResult video)) continue;
                Song song = GetSongFromVideo(video);

                if (CacheThumbnail(video, song)) songs.Add(song);
            }

            return songs;
        }
        public static void ClearCachedThumbnails()
        {
            foreach (string file in Directory.EnumerateFiles(ThumbnailCacheDirectory))
            {
                File.Delete(file);
            }
        }
        public static void ClearCachedSongs()
        {
            lastSearchQuery = null;

            foreach (string dir in Directory.EnumerateDirectories(SongCacheDirectory))
            {
                Directory.Delete(dir, true);
            }
        }
        public static async Task<Song> DownloadSong(string videoId, Progress<double> progress, CancellationToken cancellationToken)
        {
            Video video = await ytClient.Value.Videos.GetAsync(VideoId.Parse(videoId), cancellationToken);
            Song song = GetSongFromVideo(video);
            string directory = GetSongDirectory(song.id);

            if (Directory.Exists(directory)) return song;
            Directory.CreateDirectory(directory);

            StreamManifest manifest = await ytClient.Value.Videos.Streams.GetManifestAsync(videoId);
            IStreamInfo streamInfo = manifest.GetAudioOnlyStreams().GetWithHighestBitrate();

            await ytClient.Value.Videos.Streams.DownloadAsync(streamInfo, $"{directory}/Audio.mp3", progress, cancellationToken);
            DownloadThumbnail(video, directory);

            WriteSongData(directory, song);
            PlaylistManager.AddSongToPlaylist(PlaylistManager.downloadedPlaylistName, song.id);

            Toast.MakeText(MainActivity.activity.BaseContext,
                $"{song.title} by {song.artist} has been downloaded", ToastLength.Long).Show();

            return song;
        }
        public static async Task<Song> DownloadCachedSong(Song song, Progress<double> progress, CancellationToken cancellationToken)
        {
            string directory = GetSongDirectory(song.id);

            if (Directory.Exists(directory)) return song;
            Directory.CreateDirectory(directory);

            StreamManifest manifest = await ytClient.Value.Videos.Streams.GetManifestAsync(song.id);
            IStreamInfo streamInfo = manifest.GetAudioOnlyStreams().GetWithHighestBitrate();

            await ytClient.Value.Videos.Streams.DownloadAsync(streamInfo, $"{directory}/Audio.mp3", progress, cancellationToken);
            File.Copy($"{ThumbnailCacheDirectory}/{song.id}.jpg", $"{directory}/Thumbnail.jpg");

            WriteSongData(directory, song);
            PlaylistManager.AddSongToPlaylist(PlaylistManager.downloadedPlaylistName, song.id);

            Toast.MakeText(MainActivity.activity.BaseContext,
                $"{song.title} by {song.artist} has been downloaded", ToastLength.Long).Show();

            return song;
        }
        public static void WriteSongData(string directory, Song song)
        {
            File.Delete($"{directory}/SongData.xml");

            XmlWriter writer = XmlWriter.Create($"{directory}/SongData.xml", new XmlWriterSettings() {
                WriteEndDocumentOnClose = true, CloseOutput = true, Indent = true });

            XmlSerializer serializer = new XmlSerializer(typeof(Song));
            serializer.Serialize(writer, song);
        }
        public static string GetSongDirectory(string id)
        {
            return $"{SongDownloadDirectory}/{id}";
        }
        public static void DownloadThumbnail(Video video, string directory)
        {
            Thumbnail thumbnail = video.Thumbnails.Where(t => t.Url.Contains("maxresdefault.jpg")).FirstOrDefault()
                ?? video.Thumbnails.GetWithHighestResolution();
            System.Net.WebClient client = new System.Net.WebClient();
            client.DownloadFile(new Uri(thumbnail.Url), $"{directory}/Thumbnail.jpg");
        }
        public static bool CacheThumbnail(VideoSearchResult video, Song song)
        {
            Thumbnail thumbnail = video.Thumbnails.Where(t => t.Url.Contains("maxresdefault.jpg")).FirstOrDefault()
                ?? video.Thumbnails.GetWithHighestResolution();

            try
            {
                webClient.Value.DownloadFile(new Uri(thumbnail.Url.Split('?').First()), $"{ThumbnailCacheDirectory}/{song.id}.jpg");
            }
            catch
            {
                return false;
            }

            return true;
        }
        public static bool CacheThumbnail(Video video, Song song)
        {
            Thumbnail thumbnail = video.Thumbnails.Where(t => t.Url.Contains("maxresdefault.jpg")).FirstOrDefault()
                ?? video.Thumbnails.GetWithHighestResolution();

            try
            {
                webClient.Value.DownloadFile(new Uri(thumbnail.Url.Split('?').First()), $"{ThumbnailCacheDirectory}/{song.id}.jpg");
            }
            catch
            {
                return false;
            }

            return true;
        }
        public static async Task<bool> CacheThumbnail(Song song)
        {
            return CacheThumbnail(await ytClient.Value.Videos.GetAsync(VideoId.Parse(song.id)), song);
        }
        public static async Task<Song> CacheSong(string videoId, Progress<double> progress, CancellationToken cancellationToken)
        {
            Video video = await ytClient.Value.Videos.GetAsync(VideoId.Parse(videoId), cancellationToken);
            Song song = GetSongFromVideo(video);
            string directory = $"{SongCacheDirectory}/{song.id}";

            if (Directory.Exists(directory)) return song;
            Directory.CreateDirectory(directory);

            StreamManifest manifest = await ytClient.Value.Videos.Streams.GetManifestAsync(videoId);
            IStreamInfo streamInfo = manifest.GetAudioOnlyStreams().GetWithHighestBitrate();

            await ytClient.Value.Videos.Streams.DownloadAsync(streamInfo, $"{directory}/Audio.mp3", progress, cancellationToken);
            DownloadThumbnail(video, directory);

            XmlWriter writer = XmlWriter.Create($"{directory}/SongData.xml", new XmlWriterSettings()
            {
                WriteEndDocumentOnClose = true,
                CloseOutput = true,
                Indent = true
            });

            XmlSerializer serializer = new XmlSerializer(typeof(Song));
            serializer.Serialize(writer, song);

            PlaylistManager.AddSongToPlaylist(PlaylistManager.tempPlaylistName, song.id);

            Toast.MakeText(MainActivity.activity.BaseContext,
                $"{song.title} by {song.artist} has been cached", ToastLength.Long).Show();

            return song;
        }
        public static void DeleteSong(string id)
        {
            if (SongPlayer.currentSong.id == id)
            {
                if (SongPlayer.currentSongs.Count > 1) SongPlayer.PlayNextSong(false);
                else return;
            }

            foreach (string playlist in PlaylistManager.GetPlaylistNames())
            {
                PlaylistManager.RemoveSongFromPlaylist(playlist, id);
            }

            PlaylistManager.RemoveSongFromPlaylist(PlaylistManager.downloadedPlaylistName, id);

            Song song = GetSongById(id);

            string directory = $"{SongDownloadDirectory}/{id}";
            if (Directory.Exists(directory)) Directory.Delete(directory, true);

            Toast.MakeText(MainActivity.activity.BaseContext,
                $"{song.title} by {song.artist} has been deleted", ToastLength.Long).Show();
        }
        public static Song GetSongById(string id)
        {
            XmlReader reader = XmlReader.Create($"{(IsSongDownloaded(id) ? SongDownloadDirectory : SongCacheDirectory)}/{id}/SongData.xml", new XmlReaderSettings() { CloseInput = true });

            XmlSerializer serializer = new XmlSerializer(typeof(Song));
            return (Song)serializer.Deserialize(reader);
        }
        public static bool IsSongDownloaded(string id)
        {
            return Directory.Exists(GetSongDirectory(id));
        }
        public static Song[] GetSongsFromIDs(string[] ids)
        {
            Song[] songs = new Song[ids.Length];
            for (int i = 0; i < songs.Length; i++)
            {
                songs[i] = GetSongById(ids[i]);
            }
            return songs;
        }
        public static T TryGetResult<T>(Func<T> f, Func<T> d, out bool fail)
        {
            try
            {
                fail = false;
                return f();
            }
            catch (Exception e)
            {
                MainActivity.activity.UnhandledException(null, new UnhandledExceptionEventArgs(e, false));
                fail = true;

                return d();
            }
        }
        public static async Task ExportSong(string videoId, Progress<double> progress, CancellationToken cancellationToken)
        {
            Video video = await ytClient.Value.Videos.GetAsync(VideoId.Parse(videoId), cancellationToken);
            Song song = GetSongFromVideo(video);
            string path = $"{SongExportDirectory}/{song.artist} - {song.title}.mp3";

            if (File.Exists(path)) return;

            StreamManifest manifest = await ytClient.Value.Videos.Streams.GetManifestAsync(videoId);
            IStreamInfo streamInfo = manifest.GetAudioOnlyStreams().GetWithHighestBitrate();

            await ytClient.Value.Videos.Streams.DownloadAsync(streamInfo, path, progress, cancellationToken);

            Toast.MakeText(MainActivity.activity.BaseContext,
                $"{song.title} by {song.artist} has been exported", ToastLength.Long).Show();
        }
    }
}