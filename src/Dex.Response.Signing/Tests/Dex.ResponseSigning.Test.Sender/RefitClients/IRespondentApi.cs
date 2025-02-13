using Refit;

namespace Dex.ResponseSigning.Test.Sender.RefitClients;

public interface IRespondentApi
{
    [Get("/Respondent")]
    Task<ResponseDto> Test();
}