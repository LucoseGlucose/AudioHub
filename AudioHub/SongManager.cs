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
        public static string ThumbnailCacheDirectory => $"{MainActivity.activity.GetExternalFilesDir(null).AbsolutePath}/Cached Thumbnails";
        private static string lastSearchQuery;

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
        public static async Task<List<Song>> SearchForSongs(string query, CancellationToken cancellationToken)
        {
            if (query != lastSearchQuery)
            {
                ClearCachedThumbnails();
                lastSearchQuery = query;
            }

            List<Song> songs = new List<Song>();

            IAsyncEnumerator<Batch<ISearchResult>> results = ytClient.Value.Search.
                GetResultBatchesAsync(query, SearchFilter.Video, cancellationToken).GetAsyncEnumerator(cancellationToken);

            while (await results.MoveNextAsync())
            {
                foreach (ISearchResult result in results.Current.Items)
                {
                    if (!(result is VideoSearchResult video)) continue;
                    Song song = GetSongFromVideo(video);

                    if (CacheThumbnail(video, song)) songs.Add(song);
                }
                break;
            }

            return songs;
        }
        public static void ClearCachedThumbnails()
        {
            if (!Directory.Exists(ThumbnailCacheDirectory)) Directory.CreateDirectory(ThumbnailCacheDirectory);

            foreach (string file in Directory.EnumerateFiles(ThumbnailCacheDirectory))
            {
                File.Delete(file);
            }
        }
        public static async Task<Song> DownloadSong(string videoId, Progress<double> progress, CancellationToken cancellationToken)
        {
            Video video = await ytClient.Value.Videos.GetAsync(VideoId.Parse(videoId), cancellationToken);
            Song song = GetSongFromVideo(video);
            string directory = GetSongDirectory(song.id);

            if (Directory.Exists(directory)) return song;

            DeleteSong(song.id);
            Directory.CreateDirectory(directory);

            StreamManifest manifest = await ytClient.Value.Videos.Streams.GetManifestAsync(videoId);
            IStreamInfo streamInfo = manifest.GetAudioOnlyStreams().GetWithHighestBitrate();

            await ytClient.Value.Videos.Streams.DownloadAsync(streamInfo, $"{directory}/Audio.mp3", progress, cancellationToken);
            DownloadThumbnail(video, song);

            XmlWriter writer = XmlWriter.Create($"{directory}/SongData.xml", new XmlWriterSettings() {
                WriteEndDocumentOnClose = true, CloseOutput = true, Indent = true });

            XmlSerializer serializer = new XmlSerializer(typeof(Song));
            serializer.Serialize(writer, song);

            return song;
        }
        public static string GetSongDirectory(string id)
        {
            return $"{SongDownloadDirectory}/{id}";
        }
        public static void DownloadThumbnail(Video video, Song song)
        {
            Thumbnail thumbnail = video.Thumbnails.Where(t => t.Url.Contains("maxresdefault.jpg")).FirstOrDefault()
                ?? video.Thumbnails.GetWithHighestResolution();
            System.Net.WebClient client = new System.Net.WebClient();
            client.DownloadFile(new Uri(thumbnail.Url), $"{GetSongDirectory(song.id)}/Thumbnail.jpg");
        }
        public static bool CacheThumbnail(VideoSearchResult video, Song song)
        {
            if (!Directory.Exists(ThumbnailCacheDirectory)) Directory.CreateDirectory(ThumbnailCacheDirectory);

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
        public static void DeleteSong(string id)
        {
            string directory = $"{SongDownloadDirectory}/{id}";
            if (Directory.Exists(directory)) Directory.Delete(directory, true);

            foreach (string playlist in PlaylistManager.GetPlaylistNames())
            {
                PlaylistManager.RemoveSongFromPlaylist(playlist, id);
            }
        }
        public static Song GetSongById(string id)
        {
            XmlReader reader = XmlReader.Create($"{SongDownloadDirectory}/{id}/SongData.xml", new XmlReaderSettings() { CloseInput = true });

            XmlSerializer serializer = new XmlSerializer(typeof(Song));
            return (Song)serializer.Deserialize(reader);
        }
        public static bool IsSongDownloaded(string id)
        {
            return Directory.Exists(GetSongDirectory(id));
        }
    }
}