using Dapper.FastCrud;
using System;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Fumbi.Services
{
    public static class InventoryService
    {
        public static async Task<Inventory> FindInventory(ulong uid)
        {
            Inventory inventory;
            using (var db = Database.Open())
            {
                inventory = (await db.FindAsync<Inventory>(statement => statement
                    .Where($"{nameof(Inventory.Uid):C} = @Uid")
                    .WithParameters(new { Uid = uid }))).FirstOrDefault();
            }

            if (inventory == null)
                inventory = await CreateAndInsertNewInventoryAsync(uid);

            return inventory;
        }

        private static async Task<Inventory> CreateAndInsertNewInventoryAsync(ulong uid)
        {
            var newInventory = new Inventory
            {
                Uid = uid,
                Alice = 0,
                Alice2 = 0,
                Glitch = 0,
                IronEyes = 0,
                Kitty = 0,
                Lilith = 0,
                Ophelia = 0,
                Pug = 0,
                GlitchAnim = 0
            };

            using (var db = Database.Open())
                await db.InsertAsync(newInventory);
            return newInventory;
        }

        public static async Task UpdateInventoryAsync(Inventory inventory)
        {
            using (var db = Database.Open())
                await db.UpdateAsync(inventory);
        }

        public static bool CheckInventory(Shop.ProfileTheme theme, Inventory inventory)
        {
            if (theme.GetHashCode() == 0)
                return true;

            byte check = (byte)(inventory.GetType().GetProperty(theme.ToString(), BindingFlags.Instance |
                        BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                        .GetValue(inventory));

            if (check == 1)
                return true;

            return false;
        }

        public static void AddItem(string item, Inventory inventory)
        {
            inventory.GetType().GetProperty(item, BindingFlags.Instance |
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                .SetValue(inventory, Convert.ChangeType(1, inventory.GetType().GetProperty(item).PropertyType), null);
        }

        public static Shop.ProfileTheme FindTheme(byte theme)
        {
            foreach (var i in Enum.GetValues(typeof(Shop.ProfileTheme)).Cast<Shop.ProfileTheme>())
            {
                if (i.GetHashCode() == theme)
                    return i;
            }

            return Shop.ProfileTheme.Default;
        }
    }
}
