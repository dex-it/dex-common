using System.ComponentModel.DataAnnotations;

namespace Dex.SecurityTokenProvider.Options
{
    public class TokenProviderOptions 
    {
        [Required(AllowEmptyStrings = false)] 
        public string ApiResource { get; set; } = null!;

        [Required(AllowEmptyStrings = false)]
        public string ApplicationName { get; set; } = null!;
    }
}