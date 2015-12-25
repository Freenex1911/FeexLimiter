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
            var RejectionReason = ESteamRejection.AUTH_VERIFICATION;

            try
            {
                RejectionReason = (ESteamRejection)Enum.Parse(typeof(ESteamRejection), Configuration.Instance.accRejectionReason, true);
            }
            catch { }

            return RejectionReason;
        }

        private void UnturnedPermissions_OnJoinRequested(Steamworks.CSteamID player, ref ESteamRejection? rejectionReason)
        {
            for (int i = 0; i < Configuration.Instance.Whitelist.Length; i++)
            {
                if (Configuration.Instance.Whitelist[i].WhitelistUser == player.ToString()){ return; }
            }

            WebClient WC = new WebClient();

            string privacyState = string.Empty;
            string vacBanned = string.Empty;
            string isLimitedAccount = string.Empty;
            string memberSince = string.Empty;

            using (XmlReader xreader = XmlReader.Create(new StringReader(WC.DownloadString("http://steamcommunity.com/profiles/" + player + "?xml=1"))))
            {
                while (xreader.Read())
                {
                    if (xreader.IsStartElement())
                    {
                        if (xreader.Name == "privacyState")
                        {
                            if (xreader.Read()) { privacyState = xreader.Value; }
                        }
                        else if (xreader.Name == "vacBanned")
                        {
                            if (xreader.Read()) { vacBanned = xreader.Value; }
                        }
                        else if (xreader.Name == "isLimitedAccount")
                        {
                            if (xreader.Read()) { isLimitedAccount = xreader.Value; }
                        }
                        else if (xreader.Name == "memberSince")
                        {
                            if (xreader.Read()) { memberSince = xreader.Value; }
                        }
                    }
                }
            }

            if (isLimitedAccount == string.Empty) { rejectionReason = GetSteamRejection(); return; }

            if (privacyState == "private" && Configuration.Instance.accKickPrivateProfiles)
            {
                if (Configuration.Instance.accNonLimitedOverwrites)
                {
                    if (isLimitedAccount == "1") { rejectionReason = GetSteamRejection(); return; }
                }
                else { rejectionReason = GetSteamRejection(); return; }
            }
            if (vacBanned == "1" && Configuration.Instance.accKickVACBannedAccounts) { rejectionReason = GetSteamRejection(); return; }
            if (isLimitedAccount == "1" && Configuration.Instance.accKickLimitedAccounts) { rejectionReason = GetSteamRejection(); return; }
            if (memberSince != string.Empty)
            {
                try
                {
                    string[] MemberSince = memberSince.Split(' ');
                    MemberSince[1] = Regex.Match(MemberSince[1], @"\d+").Value;
                    DateTime dtSteamUser = DateTime.ParseExact(MemberSince[1] + MemberSince[0] + MemberSince[2], "dMMMMyyyy", CultureInfo.InvariantCulture, DateTimeStyles.None);
                    TimeSpan tsSteamUser = DateTime.Now - dtSteamUser;
                    if (tsSteamUser.Days <= Configuration.Instance.accMinimumDays)
                    {
                        if (Configuration.Instance.accNonLimitedOverwrites)
                        {
                            if (isLimitedAccount == "1") { rejectionReason = GetSteamRejection(); return; }
                        }
                        else { rejectionReason = GetSteamRejection(); return; }
                    }
                }
                catch
                {
                    Logger.LogWarning("DateTimeParseError: " + player + " // " + memberSince);
                }
            }
        }
    }
}
