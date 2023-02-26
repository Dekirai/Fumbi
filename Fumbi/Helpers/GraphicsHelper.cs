using AnimatedGif;
using Fumbi.Services;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Fumbi.Helpers
{
    public static class GraphicsHelper
    {
        public static async Task<MemoryStream> DrawLevelUpImage(uint level, string name, byte theme)
        {
            bool isAnimated = false;

            if (theme == 9)
                isAnimated = true;

            string extension = !isAnimated ? ".png" : ".gif";

            var image = ImageCache.GetOrAdd(AppDomain.CurrentDomain.BaseDirectory + "resources\\LevelUp" + theme + extension);
            var icon = ImageCache.GetOrAdd(AppDomain.CurrentDomain.BaseDirectory + "resources\\Level" + level.ToString() + ".png");

            uint frameCount = 1;
            var frames = new Bitmap[1];
            if (isAnimated)
            {
                frameCount = (uint)image.GetFrameCount(FrameDimension.Time);
                frames = new Bitmap[frameCount];
            }

            using (var arialFont = new Font("Arial", 21, FontStyle.Bold))
            {
                for (uint i = 0; i < frameCount; i++)
                {
                    image.SelectActiveFrame(FrameDimension.Time, (int)(i));

                    frames[i] = new Bitmap(285, 96);

                    using (var g = Graphics.FromImage(frames[i]))
                    {
                        g.DrawImage(image, new Point(0, 0));
                        g.DrawImage(icon, 23, 23, 50, 50);

                        g.DrawString(level.ToString(), arialFont, Brushes.White, 87, 43);
                        g.DrawString(name, arialFont, Brushes.White, 133, 45);
                    }
                }
            }

            if (!isAnimated)
                return frames[0].ToStream();

            return await frames.ToStream();
        }

        public static async Task<MemoryStream> DrawProfileImageAsync(uint level, string name, uint exp, ulong pen, uint rank, byte theme, ExpBar expBar, string avatarUrl)
        {
            bool isAnimated = false;

            if (theme == 9)
                isAnimated = true;

            string extension = !isAnimated ? ".png" : ".gif";

            var image = ImageCache.GetOrAdd(AppDomain.CurrentDomain.BaseDirectory + "resources\\Profile" + theme + extension);
            var avatar = await GetAvatarAsync(avatarUrl);
            var icon = ImageCache.GetOrAdd(AppDomain.CurrentDomain.BaseDirectory + "resources\\Level" + level.ToString() + ".png");
            var expbar = ImageCache.GetOrAdd(AppDomain.CurrentDomain.BaseDirectory + "resources\\ExpBar" + theme + ".png");

            uint frameCount = 1;
            var frames = new Bitmap[1];
            if (isAnimated)
            {
                frameCount = (uint)image.GetFrameCount(FrameDimension.Time);
                frames = new Bitmap[frameCount];
            }

            using (var bottomFont = new Font("Arial", 9))
            {
                using (var nameFont = new Font("Arial", 18, FontStyle.Bold))
                {
                    for (uint i = 0; i < frameCount; i++)
                    {
                        image.SelectActiveFrame(FrameDimension.Time, (int)(i));

                        frames[i] = new Bitmap(285, 192);

                        using (var g = Graphics.FromImage(frames[i]))
                        {
                            g.DrawImage(image, new Point(0, 0));

                            g.DrawImage(avatar, 20, 20, 55, 55);
                            g.DrawImage(icon, 89, 35, 26, 26);
                            g.DrawImage(expbar, 42, 100, 200 * expBar.Percentage, 11);

                            g.DrawString(level.ToString(), bottomFont, Brushes.White, 135 - MeasureString(level.ToString(), bottomFont).Width, 164);
                            g.DrawString(exp.ToString(), bottomFont, Brushes.White, 135 - MeasureString(exp.ToString(), bottomFont).Width, 142);
                            g.DrawString(pen.ToString(), bottomFont, Brushes.White, 262 - MeasureString(pen.ToString(), bottomFont).Width, 142);
                            g.DrawString(rank.ToString(), bottomFont, Brushes.White, 262 - MeasureString(rank.ToString(), bottomFont).Width, 164);
                            g.DrawString(((uint)(expBar.Percentage * 100)).ToString() + "%", bottomFont, Brushes.White, 275 - MeasureString(((uint)(expBar.Percentage * 100)).ToString() + "%", bottomFont).Width, 97);
                            g.DrawString("Exp:", bottomFont, Brushes.White, 16, 97);
                            g.DrawString("/", bottomFont, Brushes.White, 142, 114);
                            g.DrawString(expBar.CurrentExp.ToString(), bottomFont, Brushes.White, 144 - MeasureString(expBar.CurrentExp.ToString(), bottomFont).Width, 114);
                            g.DrawString(expBar.NextLevelExp.ToString(), bottomFont, Brushes.White, 147, 114);
                            g.DrawString(name, nameFont, Brushes.White, 125, 33);
                        }
                    }
                }
            }

            avatar.Dispose();

            if (!isAnimated)
                return frames[0].ToStream();

            return await frames.ToStream();
        }

        public static async Task<MemoryStream> DrawRankImageAsync(uint level, string name, uint rank, byte theme, ExpBar expBar, string avatarUrl)
        {
            bool isAnimated = false;

            if (theme == 9)
                isAnimated = true;

            string extension = !isAnimated ? ".png" : ".gif";

            var image = ImageCache.GetOrAdd(AppDomain.CurrentDomain.BaseDirectory + "resources\\Rank" + theme + extension);
            var avatar = await GetAvatarAsync(avatarUrl);
            var icon = ImageCache.GetOrAdd(AppDomain.CurrentDomain.BaseDirectory + "resources\\Level" + level.ToString() + ".png");
            var expbar = ImageCache.GetOrAdd(AppDomain.CurrentDomain.BaseDirectory + "resources\\ExpBar" + theme + ".png");

            uint frameCount = 1;
            var frames = new Bitmap[1];
            if (isAnimated)
            {
                frameCount = (uint)image.GetFrameCount(FrameDimension.Time);
                frames = new Bitmap[frameCount];
            }

            using (var bottomFont = new Font("Arial", 9))
            using (var nameFont = new Font("Arial", 18, FontStyle.Bold))
            {
                for (uint i = 0; i < frameCount; i++)
                {
                    image.SelectActiveFrame(FrameDimension.Time, (int)(i));

                    frames[i] = new Bitmap(285, 96);

                    using (var g = Graphics.FromImage(frames[i]))
                    {
                        g.DrawImage(image, new Point(0, 0));
                        g.DrawImage(avatar, 19, 19, 40, 40);
                        g.DrawImage(icon, 73, 26, 26, 26);
                        g.DrawImage(expbar, 38, 64, 200 * expBar.Percentage, 11);

                        g.DrawString("Rank: #" + rank.ToString(), bottomFont, Brushes.White, 115, 47);
                        g.DrawString(((uint)(expBar.Percentage * 100)).ToString() + "%", bottomFont, Brushes.White, 270 - MeasureString(((uint)(expBar.Percentage * 100)).ToString() + "%", bottomFont).Width, 61);

                        if (theme != 0)
                            g.DrawString("Exp:", bottomFont, Brushes.White, 13, 61);

                        g.DrawString(name, nameFont, Brushes.White, 111, 20);
                    }
                }
            }

            avatar.Dispose();

            if (!isAnimated)
                return frames[0].ToStream();

            return await frames.ToStream();
        }

        public static MemoryStream DrawDailyImage(uint penGain)
        {
            var image = ImageCache.GetOrAdd(AppDomain.CurrentDomain.BaseDirectory + "resources\\Daily.png");
            using (var bmp = new Bitmap(image, 1054, 301))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    using (var penFont = new Font("Arial", 50, FontStyle.Bold))
                    {
                        g.DrawString(penGain.ToString() + " PEN", penFont, Brushes.White, 525, 140);
                    }
                }

                return bmp.ToStream();
            }
        }

        private static SizeF MeasureString(string text, Font font)
        {
            using (var g = Graphics.FromHwnd(IntPtr.Zero))
            {
                return g.MeasureString(text, font);
            }
        }
        private static async Task<Image> GetAvatarAsync(string url)
        {
            using (var webClient = new WebClient())
            {
                using (var stream = await webClient.OpenReadTaskAsync(new Uri(url)))
                {
                    return Image.FromStream(stream);
                }
            }
        }

        public static async Task<Stream> GetAvatarStreamAsync(string url)
        {
            using (var webClient = new WebClient())
            {
                return await webClient.OpenReadTaskAsync(new Uri(url));
            }
        }

        private static MemoryStream ToStream(this Image image)
        {
            var stream = new MemoryStream();
            image.Save(stream, ImageFormat.Png);
            image.Dispose();
            stream.Position = 0;

            return stream;
        }

        private static async Task<MemoryStream> ToStream(this Bitmap[] frames)
        {
            var ms = new MemoryStream();
            using (var gif = new AnimatedGifCreator(ms))
            {
                foreach (var image in frames)
                {
                    await gif.AddFrameAsync(image);
                }
            }

            ms.Position = 0;

            return ms;
        }
    }
}
