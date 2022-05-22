using System.ComponentModel.DataAnnotations;

namespace Dex.SecurityTokenProvider.Options
{
    public class TokenProviderOptions 
    {
        [Required(AllowEmptyStrings = false)] 
        public string ApiResource { get; set; } = null!;
    }
}