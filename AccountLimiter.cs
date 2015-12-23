using Rocket.Core.Logging;
using Rocket.Core.Plugins;
using Rocket.Unturned.Permissions;
using SDG.Unturned;
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;

namespace Freenex.AccountLimiter
{
    public class AccountLimiter : RocketPlugin<AccountLimiterConfiguration>
    {
        public static AccountLimiter Instance;

        protected override void Load()
        {
            Instance = this;
            UnturnedPermissions.OnJoinRequested += UnturnedPermissions_OnJoinRequested;
            Logger.Log("Freenex's AccountLimiter has been loaded!");
        }

        protected override void Unload()
        {
            UnturnedPermissions.OnJoinRequested -= UnturnedPermissions_OnJoinRequested;
            Logger.Log("Freenex's AccountLimiter has been unloaded!");
        }

        public ESteamRejection GetSteamRejection()
        {
            if (Configuration.Instance.accRejectionReason == "ALREADY_CONNECTED") { return ESteamRejection.ALREADY_CONNECTED; }
            else if (Configuration.Instance.accRejectionReason == "ALREADY_PENDING") { return ESteamRejection.ALREADY_PENDING; }
            else if (Configuration.Instance.accRejectionReason == "AUTH_ECON") { return ESteamRejection.AUTH_ECON; }
            else if (Configuration.Instance.accRejectionReason == "AUTH_ELSEWHERE") { return ESteamRejection.AUTH_ELSEWHERE; }
            else if (Configuration.Instance.accRejectionReason == "AUTH_LICENSE_EXPIRED") { return ESteamRejection.AUTH_LICENSE_EXPIRED; }
            else if (Configuration.Instance.accRejectionReason == "AUTH_NO_STEAM") { return ESteamRejection.AUTH_NO_STEAM; }
            else if (Configuration.Instance.accRejectionReason == "AUTH_NO_USER") { return ESteamRejection.AUTH_NO_USER; }
            else if (Configuration.Instance.accRejectionReason == "AUTH_PUB_BAN") { return ESteamRejection.AUTH_PUB_BAN; }
            else if (Configuration.Instance.accRejectionReason == "AUTH_TIMED_OUT") { return ESteamRejection.AUTH_TIMED_OUT; }
            else if (Configuration.Instance.accRejectionReason == "AUTH_USED") { return ESteamRejection.AUTH_USED; }
            else if (Configuration.Instance.accRejectionReason == "AUTH_VAC_BAN") { return ESteamRejection.AUTH_VAC_BAN; }
            else if (Configuration.Instance.accRejectionReason == "AUTH_VERIFICATION") { return ESteamRejection.AUTH_VERIFICATION; }
            else if (Configuration.Instance.accRejectionReason == "LATE_PENDING") { return ESteamRejection.LATE_PENDING; }
            else if (Configuration.Instance.accRejectionReason == "NAME_CHARACTER_INVALID") { return ESteamRejection.NAME_CHARACTER_INVALID; }
            else if (Configuration.Instance.accRejectionReason == "NAME_CHARACTER_LONG") { return ESteamRejection.NAME_CHARACTER_LONG; }
            else if (Configuration.Instance.accRejectionReason == "NAME_CHARACTER_NUMBER") { return ESteamRejection.NAME_CHARACTER_NUMBER; }
            else if (Configuration.Instance.accRejectionReason == "NAME_CHARACTER_SHORT") { return ESteamRejection.NAME_CHARACTER_SHORT; }
            else if (Configuration.Instance.accRejectionReason == "NAME_PLAYER_INVALID") { return ESteamRejection.NAME_PLAYER_INVALID; }
            else if (Configuration.Instance.accRejectionReason == "NAME_PLAYER_LONG") { return ESteamRejection.NAME_PLAYER_LONG; }
            else if (Configuration.Instance.accRejectionReason == "NAME_PLAYER_NUMBER") { return ESteamRejection.NAME_PLAYER_NUMBER; }
            else if (Configuration.Instance.accRejectionReason == "NAME_PLAYER_SHORT") { return ESteamRejection.NAME_PLAYER_SHORT; }
            else if (Configuration.Instance.accRejectionReason == "NOT_PENDING") { return ESteamRejection.NOT_PENDING; }
            else if (Configuration.Instance.accRejectionReason == "PING") { return ESteamRejection.PING; }
            else if (Configuration.Instance.accRejectionReason == "PRO") { return ESteamRejection.PRO; }
            else if (Configuration.Instance.accRejectionReason == "SERVER_FULL") { return ESteamRejection.SERVER_FULL; }
            else if (Configuration.Instance.accRejectionReason == "WHITELISTED") { return ESteamRejection.WHITELISTED; }
            else if (Configuration.Instance.accRejectionReason == "WRONG_HASH") { return ESteamRejection.WRONG_HASH; }
            else if (Configuration.Instance.accRejectionReason == "WRONG_PASSWORD") { return ESteamRejection.WRONG_PASSWORD; }
            else if (Configuration.Instance.accRejectionReason == "WRONG_VERSION") { return ESteamRejection.WRONG_VERSION; }
            else { return ESteamRejection.AUTH_VERIFICATION; }
        }

        private void UnturnedPermissions_OnJoinRequested(Steamworks.CSteamID player, ref ESteamRejection? rejectionReason)
        {
            for (int i = 0; i < Configuration.Instance.Whitelist.Length; i++)
            {
                if (Configuration.Instance.Whitelist[i].WhitelistUser == player.ToString()){ return; }
            }

            WebClient WC = new WebClient();

            bool UserHasSetUpProfile = false;
            bool NonLimitedOverwrites = false;

            using (XmlReader xreader = XmlReader.Create(new StringReader(WC.DownloadString("http://steamcommunity.com/profiles/" + player + "?xml=1"))))
            {
                while (xreader.Read())
                {
                    if (xreader.IsStartElement())
                    {
                        if (xreader.Name == "vacBanned")
                        {
                            if (xreader.Read())
                            {
                                if (xreader.Value == "1")
                                {
                                    if (Configuration.Instance.accKickVACBannedAccounts)
                                    {
                                        rejectionReason = GetSteamRejection();
                                    }
                                }
                            }
                        }
                        else if (xreader.Name == "isLimitedAccount")
                        {
                            if (xreader.Read())
                            {
                                if (xreader.Value == "1")
                                {
                                    if (Configuration.Instance.accKickLimitedAccounts)
                                    {
                                        rejectionReason = GetSteamRejection();
                                    }
                                }
                                else if (xreader.Value == "0")
                                {
                                    if (Configuration.Instance.accNonLimitedOverwrites)
                                    {
                                        NonLimitedOverwrites = true;
                                    }
                                }
                                UserHasSetUpProfile = true;
                            }
                        }
                        else if (xreader.Name == "memberSince")
                        {
                            if (xreader.Read())
                            {
                                try
                                {
                                    string[] MemberSince = xreader.Value.Split(' ');
                                    MemberSince[1] = Regex.Match(MemberSince[1], @"\d+").Value;
                                    DateTime dtSteamUser = DateTime.ParseExact(MemberSince[1] + MemberSince[0] + MemberSince[2], "dMMMMyyyy", CultureInfo.InvariantCulture, DateTimeStyles.None);
                                    TimeSpan tsSteamUser = DateTime.Now - dtSteamUser;
                                    if (tsSteamUser.Days <= Configuration.Instance.accMinimumDays && !NonLimitedOverwrites)
                                    {
                                        rejectionReason = GetSteamRejection();
                                    }
                                }
                                catch
                                {
                                    Logger.LogWarning("DateTimeParseError: " + player + " // " + xreader.Value);
                                }
                                UserHasSetUpProfile = true;
                                xreader.Close();
                            }
                        }
                    }
                }
            }

            if (!UserHasSetUpProfile)
            {
                rejectionReason = GetSteamRejection();
            }

        }
    }
}
