namespace Dex.TeamCity
{
    public class TeamCityRevisionDto
    {
        public int Build { get; set; }
        public string? Revision { get; set; }
        public string? TeamCityRevision { get; set; }

        public static TeamCityRevisionDto Create()
        {
            return new TeamCityRevisionDto
            {
                Build = 0,
                Revision = "",
                TeamCityRevision = "",
            };
        }
    }
}