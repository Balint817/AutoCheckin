namespace AutoCheckin.Games
{
    public class HonkaiImpact3rd : BaseGame
    {
        public override IEnumerable<SelectorAction> GetUidActions()
        {
            throw new NotImplementedException();
        }
        public override string? GetUidFromCompleteActions(SelectorAction[] actions, string regionKey)
        {
            throw new NotImplementedException();
        }
        public override string HoyolabGameProfileUrl => throw new NotImplementedException();
        public override string DailyApiUrl => "https://sg-public-api.hoyolab.com/event/mani/sign?lang=en-us&act_id=e202110291205111";
        public override string DailyManualUrl => "https://act.hoyolab.com/bbs/event/signin-bh3/index.html?act_id=e202110291205111";
        public HonkaiImpact3rd(MainManager mainManager) : base(mainManager) { }
        public override string SettingRegionToUrlRegion(string region)
        {
            throw new NotImplementedException();
        }
        //public override void TransformCodeRedeemMessage(HttpRequestMessage message)
        //{
        //    message.Headers.AcceptLanguage.Clear();
        //    message.Headers.Accept.Clear();
        //    message.Headers.Remove("Cookies");
        //    message.Headers.Remove("Origin");
        //    message.Headers.Remove("x-rpc-app_version");
        //    message.Headers.Remove("x-rpc-client_type");

        //    message.Headers.AcceptEncoding.Clear();
        //    message.Headers.AcceptEncoding.ParseAdd("gzip");

        //    message.Headers.UserAgent.Clear();
        //    message.Headers.UserAgent.ParseAdd("Dalvik/2.1.0 (Linux; U; Android 9; SM-N976N Build/PQ3B.190801.03011045)");
        //    message.Headers.Add("X-Unity-Version", "2017.4.18f1");

        //    var authKey = MainManager.Honkai3rdAuthKey;
        //    if (string.IsNullOrEmpty(authKey))
        //    {
        //        throw new ArgumentException("invalid auth key", nameof(authKey));
        //    }

        //    authKey = Uri.EscapeDataString(authKey);
        //    var version = QueryGameVersion();

        //    var url = "https://sg-public-api.hoyoverse.com/common/apicdkey/api/queryItems?sign_type=2&authkey_ver=1&auth_appid=apicdkey&game_biz=bh3_global&cdkey={0}&lang=en&game_version={1}&device_model=samsung+SM-N976N&os_system=Android+OS+9+%2f+API-28+(PQ3B.190801.03011045%2fG9650ZHU2ARC6)&plat_type=android&web_type=android&authkey={2}";

        //    var originalStr = message.RequestUri.OriginalString;
        //    originalStr = originalStr[CodeUrlStart.Length..];
        //    var options = originalStr.Split('&', 3);
        //    var uid = options[0][2..];
        //    var region = options[1][2..];
        //    var code = options[2][2..];

        //    var formattedUrl = string.Format(url, code, version, authKey);

        //    message.RequestUri = new Uri(formattedUrl);
        //}

        //string QueryGameVersion()
        //{
        //    var url = "https://antifandom.com/honkaiimpact3/wiki/Update_Log";
        //    var responseMsg = Program.Client.GetAsync(url).Result;
        //    var responseBody = responseMsg.Content.ReadAsStringAsync().Result;
        //    var document = new HtmlDocument();
        //    document.LoadHtml(responseBody);

        //    // equivalent CSS: ".mw-parser-output div:nth-of-type(2) big b a"
        //    var versionNodeSelector = ".//*[contains(concat(\" \",normalize-space(@class),\" \"),\" mw-parser-output \")]//div[2]//big//b//a";
        //    var versionNode = document.DocumentNode.SelectSingleNode(versionNodeSelector);
        //    var versionText = versionNode.InnerText;
        //    return versionText.Split(' ', 2)[1]+".0";
        //}
        //public override string CodeRedeemOrigin => "https://sg-public-api.hoyoverse.com";
        //public override string CodeRedeemReferer => "https://sg-public-api.hoyoverse.com/";

        //static readonly string CodeUrlStart = "https://dummy123asd.com/asd.html?";
        //public override string CodeUrl => CodeUrlStart+ "a={0}&b={1}&c={2}";

        public override string CodeRedeemOrigin => throw new NotImplementedException();
        public override string CodeRedeemReferer => throw new NotImplementedException();
        public override string CodeUrl => throw new NotImplementedException();
    }
}
