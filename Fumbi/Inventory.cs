using Fumbi.Services;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

namespace Fumbi
{
    [Table("inventory")]
    public class Inventory
    {
        [Key]
        public ulong Uid { get; set; }

        public byte Alice { get; set; }

        public byte Alice2 { get; set; }

        public byte Glitch { get; set; }

        public byte IronEyes { get; set; }

        public byte Kitty { get; set; }

        public byte Lilith { get; set; }

        public byte Ophelia { get; set; }

        public byte Pug { get; set; }
        public byte GlitchAnim { get; set; }


        public async Task UpdateInventoryAsync()
        {
            await InventoryService.UpdateInventoryAsync(this);
        }

        public void AddItem(string item)
        {
            InventoryService.AddItem(item, this);
        }

        public bool CheckInventory(Shop.ProfileTheme theme)
        {
            return InventoryService.CheckInventory(theme, this);
        }
    }
}
