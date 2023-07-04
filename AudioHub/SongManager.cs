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
        public static string SongDownloadDirectory => $"{MainActivity.activity.GetExternalFilesDir(null).AbsolutePath}/Songs";

        public static Song GetSongFromVideo(Video video)
        {
            return new Song(video.Id.Value.GetHashCode(), video.Title, video.Author.ChannelName ?? video.Author.ChannelTitle,
                (int)Math.Ceiling(video.Duration.Value.TotalSeconds));
        }
        public static Song GetSongFromVideo(VideoSearchResult video)
        {
            return new Song(video.Id.Value.GetHashCode(), video.Title, video.Author.ChannelName ?? video.Author.ChannelTitle,
                (int)Math.Ceiling(video.Duration.Value.TotalSeconds));
        }
        public static async Task<List<Song>> SearchForSongs(string query, CancellationToken cancellationToken)
        {
            List<Song> songs = new List<Song>();

            await foreach (ISearchResult result in ytClient.Value.Search.GetResultsAsync(query, cancellationToken))
            {
                if (!(result is VideoSearchResult video)) continue;

                Thumbnail thumbnail = video.Thumbnails.OrderByDescending(t => t.Resolution.Area).First();
                songs.Add(GetSongFromVideo(video));
            }

            return songs;
        }
        public static async Task<Song> DownloadSong(VideoId videoId, Progress<double> progress, CancellationToken cancellationToken)
        {
            Video video = await ytClient.Value.Videos.GetAsync(videoId, cancellationToken);
            Song song = GetSongFromVideo(video);
            string directory = $"{SongDownloadDirectory}/{song.id}";

            DeleteSong(song);
            Directory.CreateDirectory(directory);

            StreamManifest manifest = await ytClient.Value.Videos.Streams.GetManifestAsync(videoId);
            IStreamInfo streamInfo = manifest.GetAudioOnlyStreams().GetWithHighestBitrate();

            await ytClient.Value.Videos.Streams.DownloadAsync(streamInfo, $"{directory}/Audio.mp3", progress, cancellationToken);

            Thumbnail thumbnail = video.Thumbnails.Where(t => t.Url.Contains("maxresdefault.jpg")).First();
            System.Net.WebClient client = new System.Net.WebClient();
            client.DownloadFile(new Uri(thumbnail.Url), $"{directory}/Thumbnail.jpg");

            XmlWriter writer = XmlWriter.Create($"{directory}/SongData.xml", new XmlWriterSettings() {
                Async = true, WriteEndDocumentOnClose = true, CloseOutput = true, Indent = true });

            XmlSerializer serializer = new XmlSerializer(typeof(Song));
            serializer.Serialize(writer, song);

            return song;
        }
        public static void DeleteSong(Song song)
        {
            string directory = $"{SongDownloadDirectory}/{song.id}";

            if (Directory.Exists(directory))
            {
                foreach (string file in Directory.EnumerateFiles(directory))
                {
                    File.Delete(file);
                }
                Directory.Delete(directory, true);
            }
        }
    }
}