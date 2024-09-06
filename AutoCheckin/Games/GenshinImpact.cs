using HtmlAgilityPack;
using System.Net.Http.Json;
using System.Security.Policy;

namespace AutoCheckin.Games
{
    public class GenshinImpact: BaseGame
    {
        public override string HoyolabGameProfileUrl => "https://act.hoyolab.com/app/community-game-records-sea/#/ys";
        public override IEnumerable<SelectorAction> GetUidActions()
        {
            yield return new SelectorAction(".account-change");
            yield return new SelectorAction(".van-dialog__content .close", action: async e => await e.ClickAsync());
            yield return new SelectorAction(".account-change", action: async e => await e.ClickAsync());
            for (int i = 1; i <= 4; i++)
            {
                yield return new SelectorAction(".role-selector-sea-dropdown-menu .dropdown-menu__input", action: async e => await e.ClickAsync());
                yield return new SelectorAction($".role-selector-sea-dropdown-menu .dropdown-menu__list .dropdown-menu__list__item:nth-of-type({i})", action: async e => await e.ClickAsync());
                yield return new SelectorAction(".game-role-selector-sea__content__placeholder,.game-role-selector-sea__content__current__uid");
            }
        }
        public override string? GetUidFromCompleteActions(SelectorAction[] actions, string region)
        {
            return region switch
            {
                "usa" => actions[5].Result,
                "eu" => actions[8].Result,
                "asia" => actions[11].Result,
                "cht" => actions[14].Result,
                _ => throw new ArgumentOutOfRangeException(nameof(region)),
            };
        }
        public override string SettingRegionToUrlRegion(string region)
        {
            return region switch
            {
                "eu" => "os_euro",
                "usa" => "os_usa",
                "asia" => "os_asia",
                "cht" => "os_cht",
                _ => region,
            };
        }
        public override string DailyApiUrl => "https://sg-hk4e-api.hoyolab.com/event/sol/sign?lang=en-us&act_id=e202102251931481";
        public override string DailyManualUrl => "https://act.hoyolab.com/ys/event/signin-sea-v3/index.html?act_id=e202102251931481";
        public GenshinImpact(MainManager mainManager): base(mainManager) { }
        public override string CodeRedeemOrigin => "https://genshin.hoyoverse.com";
        public override string CodeRedeemReferer => "https://genshin.hoyoverse.com/";
        public override string CodeUrl => "https://sg-hk4e-api.hoyoverse.com/common/apicdkey/api/webExchangeCdkey?uid={0}&region={1}&lang=en&cdkey={2}&game_biz=hk4e_global&sLangKey=en-us";
        public override async Task<string[]> GetRedeemCodes()
        {
            IEnumerable<string> result = Enumerable.Empty<string>();
            // fandom
            {
                var url = "https://antifandom.com/genshin-impact/wiki/Promotional_Code";
                var htmlDocument = await Utils.GetHtml(url);
                // CSS equivalent: ".table-scroller tr td:first-child code"
                var validCodeXPath = ".//*[contains(concat(\" \",normalize-space(@class),\" \"),\" table-scroller \")]//tr//td[not(preceding-sibling::*)]//code";
                var tableRows = htmlDocument.DocumentNode.SelectNodes(validCodeXPath);
                var codes = tableRows.Select(x => x.InnerText.Trim()).ToArray();
                result = result.Concat(codes);
            }

            // github
            {

                var githubResponse = await Program.Client.GetAsync("https://raw.githubusercontent.com/themojache/ScrapeAction/main/valid.json");
                var codes = await githubResponse.Content.ReadFromJsonAsync<string[]>(Program.JsonOptions) ?? Array.Empty<string>();
                result = result.Concat(codes);
            }

            return result.ToArray();
        }
    }

}
