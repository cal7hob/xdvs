using System.Collections.Generic;

namespace XDevs.Offers
{
    public class GameOffer
    {
        public bool enabled            = false;
        public string id               = "";
        public string name             = "";
        public string bundleId         = "";
        public string url              = "";
        public ProfileInfo.Price award = new ProfileInfo.Price(0, ProfileInfo.PriceCurrency.Gold);

        public static GameOffer fromDictionary (string id, Dictionary<string, object> dict)
        {
            var g = new GameOffer();

            g.id = id;

            var data = new JsonPrefs(dict);
            g.enabled = data.ValueBool("enabled", false);
            g.name = data.ValueString("displayName", "");
            g.bundleId = data.ValueString("bundleId", "");
            g.url = data.ValueString("url", "");
            g.award = ProfileInfo.Price.FromDictionary(data.ValueObjectDict("price"));

            return g;
        }
    }
}