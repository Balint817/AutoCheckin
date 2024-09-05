namespace AutoCheckin.Objects
{
    public class CodesByRegion : Dictionary<string, List<string>>
    {

    }
    public class CodeRegionsByGame : Dictionary<string, CodesByRegion>
    {

    }
}
