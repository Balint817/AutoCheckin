using HtmlAgilityPack;
using System.Globalization;

namespace AutoCheckin.Games
{
    public class TearsOfThemis: BaseGame
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
        public override string DailyApiUrl => "https://sg-public-api.hoyolab.com/event/luna/os/sign?lang=en-us&act_id=e202308141137581";
        public override string DailyManualUrl => "https://act.hoyolab.com/bbs/event/signin/nxx/index.html?act_id=e202202281857121";
        public TearsOfThemis(MainManager mainManager): base(mainManager) { }
        public override string SettingRegionToUrlRegion(string region)
        {
            //glb_prod_wd01
            switch (region)
            {
                case "eu":
                case "usa":
                case "asia":
                case "cht":
                    return "glb_prod_wd01";
                default:
                    return region;
            }
        }
        public override string CodeRedeemOrigin => "https://tot.hoyoverse.com";
        public override string CodeRedeemReferer => "https://tot.hoyoverse.com/";
        public override string CodeUrl => $"https://sg-public-api.hoyoverse.com/common/apicdkey/api/webExchangeCdkey?t={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}&lang=en&game_biz=nxx_global&uid={{0}}&region={{1}}&cdkey={{2}}";
        public override async Task<string[]> GetRedeemCodes()
        {
            return (await GetRedeemCodesInternal()).ToArray();
        }
        private async Task<List<string>> GetRedeemCodesInternal()
        {
            var result = new List<string>();
            var url = "https://tot.wiki/wiki/Redeem_Code";
            var responseMsg = await Program.Client.GetAsync(url);
            var responseBody = await responseMsg.Content.ReadAsStringAsync();
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(responseBody);
            // CSS equivalent: ".wikitable:first-of-type tr"
            var tableRowsXPath = ".//*[contains(concat(\" \",normalize-space(@class),\" \"),\" wikitable \")][1]//tr";
            var tableRows = htmlDocument.DocumentNode.SelectNodes(tableRowsXPath);
            foreach (var tableRow in tableRows.Skip(1))
            {
                var codeNode = tableRow.SelectSingleNode(".//td[2]");
                var code = codeNode.InnerText.Trim();

                var startDateNode = tableRow.SelectSingleNode(".//td[4]");
                var startDateText = startDateNode.InnerText.Trim();

                var endDateNode = tableRow.SelectSingleNode(".//td[5]");
                var endDateText = endDateNode.InnerText.Trim();

                startDateText = startDateText.Replace("UTC", "", StringComparison.Ordinal);
                endDateText = endDateText.Replace("UTC", "", StringComparison.Ordinal);

                var dateFormat = "yyyy-MM-d HH:mm:ss z";
                var startDate = DateTime.ParseExact(startDateText, dateFormat, CultureInfo.InvariantCulture);
                var endDate = DateTime.ParseExact(endDateText, dateFormat, CultureInfo.InvariantCulture);

                var timeNow = DateTime.Now;
                if (startDate <= timeNow && timeNow <= endDate)
                {
                    result.Add(code);
                }
            }
            return result;
        }
    }
}
