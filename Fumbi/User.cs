using Fumbi.Helpers;
using Fumbi.Services;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Threading.Tasks;

namespace Fumbi
{
    [Table("users")]
    public class User
    {
        [Key]
        public ulong Uid { get; set; }

        public string Name { get; set; }

        public byte Level { get; set; }

        public uint Exp { get; set; }

        public ulong Pen { get; set; }

        public byte ProfileTheme { get; set; }

        public string LastDaily { get; set; }

        public string DailyExp { get; set; }

        public async Task<MemoryStream> DrawLevelUpImage()
        {
            return await GraphicsHelper.DrawLevelUpImage(Level, Name, ProfileTheme);
        }

        public async Task<MemoryStream> DrawProfileImageAsync(uint rank, string avatarUrl)
        {
            return await GraphicsHelper.DrawProfileImageAsync(Level, Name, Exp, Pen, rank, ProfileTheme, UserService.CalculateExpBar(Level, Exp), avatarUrl);
        }

        public async Task<MemoryStream> DrawRankImageAsync(uint rank, string avatarUrl)
        {
            return await GraphicsHelper.DrawRankImageAsync(Level, Name, rank, ProfileTheme, UserService.CalculateExpBar(Level, Exp), avatarUrl);
        }

        public MemoryStream DrawDailyImage(uint penGain)
        {
            return GraphicsHelper.DrawDailyImage(penGain);
        }

        public async Task<bool> MessageRecievedAsync(uint length, string name)
        {
            return await UserService.MessageRecievedAsync(length, name, this);
        }

        public async Task UpdateUserAsync()
        {
            await UserService.UpdateUserAsync(this);
        }
    }
}
