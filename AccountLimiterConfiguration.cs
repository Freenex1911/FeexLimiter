using Rocket.API;
using System.Xml.Serialization;

namespace Freenex.AccountLimiter
{
    public sealed class WhitelistSteamID
    {
        [XmlAttribute("Steam64ID")]
        public string WhitelistUser;

        public WhitelistSteamID(string steamid)
        {
            WhitelistUser = steamid;
        }
        public WhitelistSteamID()
        {
            WhitelistUser = string.Empty;
        }
    }

    public class AccountLimiterConfiguration : IRocketPluginConfiguration
    {
        public int accMinimumDays;
        public bool accLimitOverwrite;
        public bool accKickLimited;

        [XmlArrayItem("WhitelistUser")]
        [XmlArray(ElementName = "Whitelist")]
        public WhitelistSteamID[] Whitelist;

        public void LoadDefaults()
        {
            accMinimumDays = 30;
            accLimitOverwrite = true;
            accKickLimited = false;

            Whitelist = new WhitelistSteamID[]{
                new WhitelistSteamID("76561198187138313")
            };
        }
    }
}