using HtmlAgilityPack;

namespace AutoCheckin.Games
{
    public class StarRail: BaseGame
    {
        public override string HoyolabGameProfileUrl => "https://act.hoyolab.com/app/community-game-records-sea/rpg/#/hsr";
        public override IEnumerable<SelectorAction> GetUidActions()
        {
            yield return new SelectorAction(".account-change");
            yield return new SelectorAction(".van-dialog__content .close", action: async e => await e.ClickAsync());
            yield return new SelectorAction(".driver-popover button span", action: async e => await e.ClickAsync());
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
                "usa" => actions[6].Result,
                "eu" => actions[9].Result,
                "asia" => actions[12].Result,
                "cht" => actions[15].Result,
                _ => throw new ArgumentOutOfRangeException(nameof(region)),
            };
        }
        public override string DailyApiUrl => "https://sg-public-api.hoyolab.com/event/luna/os/sign?lang=en-us&act_id=e202303301540311";
        public override string DailyManualUrl => "https://act.hoyolab.com/bbs/event/signin/hkrpg/index.html?act_id=e202303301540311";
        public StarRail(MainManager mainManager): base(mainManager) { }
        public override string CodeRedeemOrigin => "https://hsr.hoyoverse.com";
        public override string CodeRedeemReferer => "https://hsr.hoyoverse.com/";
        public override string SettingRegionToUrlRegion(string region)
        {
            return region switch
            {
                "eu" => "prod_official_eur",
                "usa" => "prod_official_usa",
                "asia" => "prod_official_asia",
                "cht" => "prod_official_cht",
                _ => region,
            };
        }
        public override string CodeUrl => $"https://sg-hkrpg-api.hoyoverse.com/common/apicdkey/api/webExchangeCdkey?t={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}&lang=en&game_biz=hkrpg_global&uid={{0}}&region={{1}}&cdkey={{2}}";
        public override async Task<string[]> GetRedeemCodes()
        {
            var url = "https://www.pcgamer.com/honkai-star-rail-codes/";
            var htmlDocument = await Utils.GetHtml(url);
            // CSS equivalent: "ul:has(strong):first-of-type strong"
            var validCodeXPath = ".//ul[count(.//strong) > 0][1]//strong";
            var tableRows = htmlDocument.DocumentNode.SelectNodes(validCodeXPath);
            return tableRows.Select(x => x.InnerText.Trim()).ToArray();
        }
    }
}
