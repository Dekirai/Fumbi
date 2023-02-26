using Dapper.FastCrud;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Fumbi.Services
{
    public struct ExpBar
    {
        public float Percentage { get; set; }
        public uint CurrentExp { get; set; }
        public uint NextLevelExp { get; set; }
    }

    public static class UserService
    {
        public static async Task<User> FindUserAsync(ulong uid, string name)
        {
            User user;
            using (var db = Database.Open())
            {
                user = (await db.FindAsync<User>(statement => statement
                    .Where($"{nameof(User.Uid):C} = @Uid")
                    .WithParameters(new { Uid = uid }))).FirstOrDefault();
            }

            if (user == null)
                user = await CreateAndInsertNewUserAsync(uid, name);

            return user;
        }

        private static async Task<User> CreateAndInsertNewUserAsync(ulong uid, string name)
        {
            var newUser = new User
            {
                Uid = uid,
                Name = name,
                Level = 0,
                Exp = 0,
                Pen = 0,
                ProfileTheme = 0,
                LastDaily = null
            };

            //if (!name.All(char.IsLetterOrDigit))
            //    newUser.Name = "invalid name";

            using (var db = Database.Open())
                await db.InsertAsync(newUser);
            return newUser;
        }

        public static async Task UpdateUserAsync(User user)
        {
            using (var db = Database.Open())
                await db.UpdateAsync(user);
        }

        public static async Task<uint> CalculateRankAsync(ulong uid)
        {
            List<User> users;
            using (var db = Database.Open())
            {
                users = (await db.FindAsync<User>()).ToList();
            }
            var rankList = users.OrderByDescending(i => i.Exp).ToList();
            return (uint)rankList.FindIndex(i => i.Uid == uid) + 1;
        }

        public static async Task<List<User>> GetTopListAsync()
        {
            List<User> users;
            using (var db = Database.Open())
            {
                users = (await db.FindAsync<User>()).ToList();
            }
            var rankList = users.OrderByDescending(i => i.Exp).ToList();
            return rankList;
        }

        public static bool CheckDaily(DateTime last)
        {
            if ((DateTime.Now - last).Days >= 1)
                return true;

            return false;
        }

        public static uint CalculateDaily()
        {
            var rand = new Random();

            return (uint)rand.Next(10, 50 + 1);
        }

        public static bool GambleIsWon()
        {
            var rand = new Random();

            return rand.Next(0, 1 + 1) == 1 ? true : false;
        }

        public static uint GambleCalculateMultiplier()
        {
            var rand = new Random();

            double magic = rand.NextDouble();

            double multiplier = Math.Floor(2 + (5 + 1 - 2) * (Math.Pow(magic, 4)));

            return (uint)multiplier;
        }

        private enum Levels : uint
        {
            Level0 = 0,
            Level1 = 300,
            Level2 = 600,
            Level3 = 900,
            Level4 = 1200,
            Level5 = 1500,
            Level6 = 2200,
            Level7 = 2800,
            Level8 = 3500,
            Level9 = 4200,
            Level10 = 4900,
            Level11 = 6100,
            Level12 = 7300,
            Level13 = 8500,
            Level14 = 9700,
            Level15 = 10900,
            Level16 = 12600,
            Level17 = 14300,
            Level18 = 16000,
            Level19 = 17700,
            Level20 = 20000,
            Level21 = 22300,
            Level22 = 24600,
            Level23 = 26900,
            Level24 = 29200,
            Level25 = 33500,
            Level26 = 37800,
            Level27 = 42100,
            Level28 = 46400,
            Level29 = 50700,
            Level30 = 60500,
            Level31 = 70300,
            Level32 = 80100,
            Level33 = 89900,
            Level34 = 99700,
            Level35 = 126500,
            Level36 = 153300,
            Level37 = 180100,
            Level38 = 206900,
            Level39 = 233700,
            Level40 = 264500,
            Level41 = 295300,
            Level42 = 326100,
            Level43 = 356900,
            Level44 = 387700,
            Level45 = 428500,
            Level46 = 469300,
            Level47 = 510100,
            Level48 = 550900,
            Level49 = 591700,
            Level50 = 658500,
            Level51 = 725300,
            Level52 = 792100,
            Level53 = 858900,
            Level54 = 925700,
            Level55 = 1064500,
            Level56 = 1203300,
            Level57 = 1342100,
            Level58 = 1480900,
            Level59 = 1619700,
            Level60 = 1762500,
            Level61 = 1905300,
            Level62 = 2048100,
            Level63 = 2190900,
            Level64 = 2333700,
            Level65 = 2491500,
            Level66 = 2649300,
            Level67 = 2807100,
            Level68 = 2964900,
            Level69 = 3122700,
            Level70 = 3314500,
            Level71 = 3506300,
            Level72 = 3698100,
            Level73 = 3889900,
            Level74 = 4081700,
            Level75 = 4345500,
            Level76 = 4609300,
            Level77 = 4873100,
            Level78 = 5136900,
            Level79 = 5400700,
            Level80 = 5664500
        }

        public static ExpBar CalculateExpBar(byte level, uint exp)
        {
            if (level == 80)
            {
                var specialCase = new ExpBar
                {
                    Percentage = 1.00f,
                    CurrentExp = exp - (uint)Levels.Level80,
                    NextLevelExp = exp - (uint)Levels.Level80
                };

                return specialCase;
            }

            var expList = Enum.GetValues(typeof(Levels)).Cast<Levels>().ToList();
            float percentage = (((exp - (float)expList[level])) / ((float)expList[level + 1] - (float)expList[level]));

            var expBar = new ExpBar
            {
                Percentage = percentage,
                CurrentExp = exp - (uint)expList[level],
                NextLevelExp = (uint)expList[level + 1] - (uint)expList[level]
            };

            return expBar;
        }

        public static byte CalculateLevel(uint exp)
        {
            int level = -1;

            foreach (uint i in Enum.GetValues(typeof(Levels)).Cast<Levels>())
            {
                if (exp < i)
                    break;

                level++;
            }

            return (byte)level;
        }

        public static uint CalculatePenGain(uint level)
        {
            if (level < 4)
                return 2000;
            else if (level >= 5 && level < 10)
                return 3000;
            else if (level >= 10 && level < 20)
                return 4000;
            else if (level >= 20 && level < 27)
                return 7000;
            else if (level >= 27 && level < 35)
                return 10000;
            else if (level >= 35 && level < 42)
                return 13000;
            else if (level >= 42 && level < 47)
                return 15000;
            else if (level >= 47 && level < 55)
                return 17000;
            else if (level >= 55 && level < 62)
                return 20000;
            else if (level >= 62 && level < 67)
                return 24000;
            else if (level >= 67 && level < 73)
                return 27000;
            else if (level >= 73 && level < 80)
                return 30000;
            else
                return 100000;
        }

        public static async Task<bool> MessageRecievedAsync(uint length, string name, User user)
        {
            UpdateUsername(name, user);

            if (CheckDailyExp(user, length))
            {
                byte initalLevel = user.Level;
                user.Exp += (length * 7 > 300) ? 300 : (length * 7);
                byte newLevel = CalculateLevel(user.Exp);

                if (newLevel != initalLevel)
                {
                    user.Level = newLevel;
                    user.Pen += CalculatePenGain(user.Level);

                    return true;
                }
            }

            await UpdateUserAsync(user);

            return false;
        }

        public static void UpdateUsername(string name, User user)
        {
            //if (!name.All(char.IsLetterOrDigit))
            //{
            //    user.Name = "invalid name";
            //    return;
            //}

            user.Name = name;
        }

        private static bool CheckDailyExp(User user, uint length)
        {
            if (user.DailyExp == null)
            {
                uint exp = (length * 7 > 300) ? 300 : (length * 7);
                user.DailyExp = exp.ToString().PadLeft(5, '0') + " || " + DateTime.Now.ToString();

                return true;
            }

            var lastdaily = DateTime.Parse(user.DailyExp.Remove(0, 8));
            uint totalexp = uint.Parse(user.DailyExp.Remove(5));

            if (totalexp >= 75000 && (DateTime.Now - lastdaily).Days < 1)
                return false;

            if ((DateTime.Now - lastdaily).Days >= 1)
            {
                uint exp = (length * 7 > 300) ? 300 : (length * 7);
                user.DailyExp = exp.ToString().PadLeft(5, '0') + " || " + DateTime.Now.ToString();

                return true;
            }

            if (totalexp < 75000 && (DateTime.Now - lastdaily).Days < 1)
            {
                totalexp += (length * 7 > 300) ? 300 : (length * 7);
                user.DailyExp = totalexp.ToString().PadLeft(5, '0') + " || " + lastdaily.ToString();

                return true;
            }

            return false;
        }
    }
}
