using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ColinChang.FFmpegHelper
{
    public class RtspHelper
    {
        public string Rtsp { get; set; }

        /// <summary>
        /// set time for rtsp connection,count by millisecond
        /// </summary>
        public int Timeout { get; set; } = -1;

        public RtspHelper(string rtsp)
        {
            if (!rtsp.Trim().ToLower().StartsWith("rtsp://"))
                throw new ArgumentException("invalid rtsp address");
            Rtsp = rtsp;
        }

        public RtspHelper(string rtsp, int timeout) : this(rtsp)
        {
            Timeout = timeout;
        }

        /// <summary>
        /// watermark on the video file
        /// </summary>
        public Watermark Watermark { get; set; }

        /// <summary>
        /// Record a video file from a specify rtsp address
        /// </summary>
        /// <param name="outputFile">output file</param>
        /// <param name="duration">record duration seconds of audio/video</param>
        /// <param name="userAgent">User-Agent header</param>
        /// <param name="transport">set RTSP transport protocols</param>
        /// <param name="allowedMediaTypes">set media types to accept from the server</param>
        /// <returns>whether record successfully</returns>
        public async Task<bool> Record2VideoFileAsync(
            string outputFile,
            TimeSpan duration,
            string userAgent = null,
            Transport transport = Transport.Udp, AllowedMediaTypes allowedMediaTypes = AllowedMediaTypes.All)
        {
            var beforeInput = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(userAgent))
                beforeInput["-user-agent"] = userAgent;
            if (transport != Transport.Udp)
                beforeInput["-rtsp_transport"] = transport.ToString().ToLower();
            if (allowedMediaTypes != AllowedMediaTypes.All)
                beforeInput["-allowed_media_types"] = allowedMediaTypes.ToString().ToLower();
            if (Timeout > 0)
                beforeInput["-stimeout"] = $"{Timeout * 1000}";

            var beforeOutput = new Dictionary<string, string> {["-t"] = duration.ToString()};
            if (Watermark != null)
                beforeOutput["-vf"] = Watermark.Color == Color.Empty
                    ? $"'movie={Watermark.Picture}[wm]; [in][wm]overlay={Watermark.X}:{Watermark.Y}[out]'"
                    : $"'movie={Watermark.Picture},colorkey=0x{Watermark.Color.ToArgb().ToString("X").Substring(2)}:{Watermark.Similarity}:{Watermark.Blend} [wm]; [in][wm]overlay={Watermark.X}:{Watermark.Y}[out]'";

            return await FFmpegHelper.ExecuteFfmpegAsync(Rtsp, outputFile, beforeInput, beforeOutput);
        }

        /// <summary>
        /// Screenshot once
        /// </summary>
        /// <param name="outputFile"></param>
        /// <returns></returns>
        public async Task<bool> ScreenshotAsync(string outputFile) =>
            await FFmpegHelper.ScreenshotAsync(Rtsp, outputFile, timeout: Timeout);

        /// <summary>
        /// Screenshot by a timer
        /// </summary>
        /// <param name="outputDirectory"></param>
        /// <param name="filenamePrefix">filename prefix of screenshot picture</param>
        /// <param name="interval">how often(seconds) to exec a screenshot.</param>
        /// <param name="duration">how long time will this run</param>
        /// <param name="format">screenshot picture format</param>
        /// <returns></returns>
        public async Task<bool> ScreenshotAsync(string outputDirectory, string filenamePrefix,
            int interval, TimeSpan duration,
            ImageFormat format = ImageFormat.JPG
        ) =>
            await FFmpegHelper.ScreenshotAsync(Rtsp, outputDirectory, filenamePrefix, interval, duration, Timeout,
                format);
    }

    public enum Transport
    {
        Udp,
        Tcp,
        Udp_Multicast,
        Http
    }

    public enum AllowedMediaTypes
    {
        All,
        Video,
        Audio,
        Data,
        Subtitle,
    }

    public class Watermark
    {
        /// <summary>
        /// JPG,PNG,BMP are supported
        /// </summary>
        public string Picture { get; set; }

        public float X { get; set; }

        public float Y { get; set; }

        public Color Color { get; set; }

        private float _similarity;

        /// <summary>
        /// Similarity 
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Similarity value out of range [0~1]</exception>
        public float Similarity
        {
            get => _similarity;
            set
            {
                if (_similarity < 0 || _similarity > 1)
                    throw new ArgumentOutOfRangeException($"Similarity should be between 0 and 1");
                _similarity = value;
            }
        }

        private float _blend;

        /// <summary>
        /// Blend
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Blend value out of range [0~1]</exception>
        public float Blend
        {
            get => _blend;
            set
            {
                if (_blend < 0 || _blend > 1)
                    throw new ArgumentOutOfRangeException($"Blend should be between 0 and 1");
                _blend = value;
            }
        }

        public Watermark(string picture, float x = 0, float y = 0) : this(picture, x, y, Color.Empty, 0, 0)
        {
        }

        public Watermark(string picture, float x, float y, Color color, float similarity, float blend)
        {
            Picture = picture;
            X = x;
            Y = y;
            Color = color;
            Similarity = similarity;
            Blend = blend;
        }
    }
}