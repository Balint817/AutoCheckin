using HtmlAgilityPack;

namespace AutoCheckin.Games
{
    public class ZZZ: BaseGame
    {
        public override string HoyolabGameProfileUrl => "https://act.hoyolab.com/app/mihoyo-zzz-game-record/#/zzz";
        public override IEnumerable<SelectorAction> GetUidActions()
        {
            yield return new SelectorAction(".account-change");
            yield return new SelectorAction(".van-dialog__content *[class^='close_']", action: async e => await e.ClickAsync());
            yield return new SelectorAction("div[class^='change_']", action: async e => await e.ClickAsync());
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
        public override string DailyApiUrl => "https://sg-act-nap-api.hoyolab.com/event/luna/zzz/os/sign?lang=en-us&act_id=e202406031448091";
        public override string DailyManualUrl => "https://act.hoyolab.com/bbs/event/signin/zzz/e202406031448091.html?act_id=e202406031448091";
        public ZZZ(MainManager mainManager): base(mainManager) { }
        public override string CodeRedeemOrigin => "https://zenless.hoyoverse.com";
        public override string CodeRedeemReferer => "https://zenless.hoyoverse.com/";
        public override string SettingRegionToUrlRegion(string region)
        {
            return region switch
            {
                "eu" => "prod_gf_eu",
                "usa" => "prod_gf_us",
                "asia" => "prod_gf_jp",
                "cht" => "prod_gf_sg",
                _ => region,
            };
        }
        public override string CodeUrl => $"https://public-operation-nap.hoyoverse.com/common/apicdkey/api/webExchangeCdkey?t={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}&lang=en&game_biz=nap_global&uid={{0}}&region={{1}}&cdkey={{2}}";
        public override async Task<string[]> GetRedeemCodes()
        {
            var url = "https://game8.co/games/Zenless-Zone-Zero/archives/435683";
            var htmlDocument = await Utils.GetHtml(url);
            // CSS equivalent: "ul.a-list:first-of-type .a-listItem .a-link:first-child"
            var validCodeXPath = ".//ul[contains(concat(\" \",normalize-space(@class),\" \"),\" a-list \")][1]//*[contains(concat(\" \",normalize-space(@class),\" \"),\" a-listItem \")]//*[contains(concat(\" \",normalize-space(@class),\" \"),\" a-link \")][not(preceding-sibling::*)]";
            var listItems = htmlDocument.DocumentNode.SelectNodes(validCodeXPath);
            return listItems.Select(x => x.InnerText.Trim()).ToArray();
        }
    }
}
