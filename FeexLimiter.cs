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

namespace Freenex.FeexLimiter
{
    public class FeexLimiter : RocketPlugin<FeexLimiterConfiguration>
    {
        public static FeexLimiter Instance;

        protected override void Load()
        {
            Instance = this;
            UnturnedPermissions.OnJoinRequested += UnturnedPermissions_OnJoinRequested;
            Logger.Log("Freenex's FeexLimiter has been loaded!");
        }

        protected override void Unload()
        {
            UnturnedPermissions.OnJoinRequested -= UnturnedPermissions_OnJoinRequested;
            Logger.Log("Freenex's FeexLimiter has been unloaded!");
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
                if (Configuration.Instance.Whitelist[i].WhitelistUser == player.ToString())
                {
                    if (Configuration.Instance.Logging) { Logger.LogWarning("Access granted: " + player + " // Reason: Whitelist."); }
                    return;
                }
            }

            string steamID = string.Empty;
            string privacyState = string.Empty;
            string vacBanned = string.Empty;
            string isLimitedAccount = string.Empty;
            string memberSince = string.Empty;

            try
            {
                var WR = WebRequest.Create(new Uri("http://steamcommunity.com/profiles/" + player + "?xml=1"));
                WR.Timeout = Configuration.Instance.Timeout;
                using (var WRresponse = WR.GetResponse())
                using (var WRstream = WRresponse.GetResponseStream())
                using (var sreader = new StreamReader(WRstream))
                using (XmlReader xreader = XmlReader.Create(new StringReader(sreader.ReadToEnd())))
                {
                    while (xreader.Read())
                    {
                        if (xreader.IsStartElement())
                        {
                            if (xreader.Name == "steamID")
                            {
                                if (xreader.Read()) { steamID = xreader.Value; }
                            }
                            else if (xreader.Name == "privacyState")
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
                                if (privacyState == "private" || privacyState == "friendsonly") { xreader.Close(); }
                            }
                            else if (xreader.Name == "memberSince")
                            {
                                if (xreader.Read()) { memberSince = xreader.Value; }
                                xreader.Close();
                            }
                        }
                    }
                }
            }
            catch
            {
                if (Configuration.Instance.KickOnTimeout)
                {
                    if (Configuration.Instance.Logging) { Logger.LogWarning("Access denied: " + player + " // Reason: Timeout."); }
                    rejectionReason = GetSteamRejection();
                }
                else
                {
                    if (Configuration.Instance.Logging) { Logger.LogWarning("Access granted: " + player + " // Reason: Timeout."); }
                }
                return;
            }

            if (isLimitedAccount == string.Empty)
            {
                if (Configuration.Instance.Logging) { Logger.LogWarning("Access denied: " + player + " // Reason: No profile."); }
                rejectionReason = GetSteamRejection();
                return;
            }

            if ((privacyState == "private" || privacyState == "friendsonly") && Configuration.Instance.accKickPrivateProfiles)
            {
                if (Configuration.Instance.accNonLimitedOverwrites)
                {
                    if (isLimitedAccount == "1")
                    {
                        if (Configuration.Instance.Logging) { Logger.LogWarning("Access denied: " + player + " (" + steamID + ") // Reason: Private profile."); }
                        rejectionReason = GetSteamRejection();
                        return;
                    }
                }
                else
                {
                    if (Configuration.Instance.Logging) { Logger.LogWarning("Access denied: " + player + " (" + steamID + ") // Reason: Private profile."); }
                    rejectionReason = GetSteamRejection();
                    return;
                }
            }

            if (vacBanned == "1" && Configuration.Instance.accKickVACBannedAccounts)
            {
                if (Configuration.Instance.Logging) { Logger.LogWarning("Access denied: " + player + " (" + steamID + ") // Reason: VAC banned."); }
                rejectionReason = GetSteamRejection();
                return;
            }

            if (isLimitedAccount == "1" && Configuration.Instance.accKickLimitedAccounts)
            {
                if (Configuration.Instance.Logging) { Logger.LogWarning("Access denied: " + player + " (" + steamID + ") // Reason: Limited account."); }
                rejectionReason = GetSteamRejection();
                return;
            }

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
                            if (isLimitedAccount == "1")
                            {
                                if (Configuration.Instance.Logging) { Logger.LogWarning("Access denied: " + player + " (" + steamID + ") // Reason: Account too new."); }
                                rejectionReason = GetSteamRejection();
                                return;
                            }
                        }
                        else
                        {
                            if (Configuration.Instance.Logging) { Logger.LogWarning("Access denied: " + player + " (" + steamID + ") // Reason: Account too new."); }
                            rejectionReason = GetSteamRejection();
                            return;
                        }
                    }
                }
                catch (Exception ex) { Logger.LogException(ex); }
            }

        }
    }
}
