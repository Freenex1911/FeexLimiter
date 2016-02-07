using Rocket.API;
using System.Xml.Serialization;

namespace Freenex.FeexLimiter
{
    public sealed class Whitelist
    {
        [XmlAttribute("Steam64ID")]
        public string WhitelistUser;

        public Whitelist(string steamid)
        {
            WhitelistUser = steamid;
        }
        public Whitelist()
        {
            WhitelistUser = string.Empty;
        }
    }

    public class FeexLimiterConfiguration : IRocketPluginConfiguration
    {
        public int accMinimumDays;
        public bool accKickPrivateProfiles;
        public bool accKickVACBannedAccounts;
        public bool accKickLimitedAccounts;
        public bool accNonLimitedOverwrites;
        public string accRejectionReason;

        [XmlArrayItem("WhitelistUser")]
        [XmlArray(ElementName = "Whitelist")]
        public Whitelist[] Whitelist;

        public int Timeout;
        public bool KickOnTimeout;
        public bool Logging;

        public void LoadDefaults()
        {
            accMinimumDays = 30;
            accKickPrivateProfiles = true;
            accKickVACBannedAccounts = false;
            accKickLimitedAccounts = false;
            accNonLimitedOverwrites = true;
            accRejectionReason = "AUTH_VERIFICATION";

            Whitelist = new Whitelist[]{
                new Whitelist("76561198187138313")
            };

            Timeout = 3000;
            KickOnTimeout = false;
            Logging = true;
        }
    }
}