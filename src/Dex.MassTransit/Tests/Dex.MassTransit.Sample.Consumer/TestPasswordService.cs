using System.Threading.Tasks;

namespace Dex.MassTransit.Sample.Consumer
{
    public class TestPasswordService : ITestPasswordService
    {
        public async Task<string> GetAccessToken()
        {
            // var password =
            //     "eyJhbGciOiJSUzI1NiIsInR5cCI6ImF0K2p3dCJ9.eyJuYmYiOjE2NDM0NjM2MjAsImV4cCI6MTY0NjA1NTYyMCwiaXNzIjoiaHR0cHM6Ly9sb2NhbGhvc3Q6NTAwMSIsImF1ZCI6WyJzLmNsdWJzeW5jIiwicy5yYWJiaXRtcSJdLCJjbGllbnRfaWQiOiJjbHViLmNsaWVudCIsInN1YiI6ImQ5YjQ0MTI3LTg5MGUtNDJhMC05N2VjLTk3ODA2MGNjZGE5OCIsImF1dGhfdGltZSI6MTY0MzQ2MzYyMCwiaWRwIjoiZGVmYXVsdC1uYW1lIiwicmFiYml0bXEtc2NvcGUiOlsicy5yYWJiaXRtcS5jb25maWd1cmU6V2luS3Jhc25vZGFyQ2x1Yi8qIiwicy5yYWJiaXRtcS53cml0ZToqV2luS3Jhc25vZGFyQ2x1Yi8qIiwicy5yYWJiaXRtcS5yZWFkOipXaW5LcmFzbm9kYXJDbHViLyoiXSwianRpIjoiODRDNkZEQkQwM0Y4RTM1RDgzNjU2QjBBODNFNEQzNkEiLCJpYXQiOjE2NDM0NjM2MjAsInNjb3BlIjpbImNsdWJzeW5jLWFwaSIsInMucmFiYml0bXEudGFnOmFkbWluaXN0cmF0b3IiLCJvZmZsaW5lX2FjY2VzcyJdLCJhbXIiOlsicHdkIl19.CboBuKOXWMa-1f2PJe7aBRUdopdT8fRCcccFCpw9mePZ6j7bmHsEz0BHYMjQpb7fTsAjjay38AL6v3obH5AFk6LwRQoKIiVN6JZE_AMONOymBR6KBrmTqvYOf7aw6RgdVLZmNmzEzMwgDELGba6BA4CuhUKvrM06UU6Qt6_jzapIrFqXJLuiLjlR0-epu4nlam9YOxOcS0aL-ZtT7k0fz-fRn63hQGDqs3qbuz8CkHoqpCj87lkaSE_UsKq7jetfANw9PpJYtY9QoAMAHlk_SOpBEcqszXVawVtOdxLB1MmDB40Cho9mXmTdoFqaLcT41BYWrBmlzSUhodh1aicbiw";

            // var password = "eyJhbGciOiJSUzI1NiIsInR5cCI6ImF0K2p3dCJ9.eyJuYmYiOjE2NDM0NjQxNDQsImV4cCI6MTY0NjA1NjE0NCwiaXNzIjoiaHR0cHM6Ly9sb2NhbGhvc3Q6NTAwMSIsImF1ZCI6WyJzLmNsdWJzeW5jIiwicy5yYWJiaXRtcSJdLCJjbGllbnRfaWQiOiJjbHViLmNsaWVudCIsInN1YiI6ImQ5YjQ0MTI3LTg5MGUtNDJhMC05N2VjLTk3ODA2MGNjZGE5OCIsImF1dGhfdGltZSI6MTY0MzQ2NDE0NCwiaWRwIjoiZGVmYXVsdC1uYW1lIiwicmFiYml0bXEtc2NvcGUiOlsicy5yYWJiaXRtcS5jb25maWd1cmU6V2luS3Jhc25vZGFyQ2x1Yi8qIiwicy5yYWJiaXRtcS53cml0ZToqV2luS3Jhc25vZGFyQ2x1Yi8qIiwicy5yYWJiaXRtcS5yZWFkOipXaW5LcmFzbm9kYXJDbHViLyoiXSwianRpIjoiMEZDNDc4RjUzNkUzRkYxRUYwOTg0QUQxOEE1RDA0NTciLCJpYXQiOjE2NDM0NjQxNDQsInNjb3BlIjpbImNsdWJzeW5jLWFwaSIsInMucmFiYml0bXEudGFnOmFkbWluaXN0cmF0b3IiLCJvZmZsaW5lX2FjY2VzcyJdLCJhbXIiOlsicHdkIl19.NignKSPJ-FNv5t9Ck2KeSGBvJvTPcEIc8_UVEy2f0X1j4OlzFAD-qQNWLK5_MghkzsuGs3GgROIbJkaFy9Qr6T8-ojjiplYd6pe2z6M5JIBcF3v-cg2V01m-DCnY_PTql0S4rau6GYBm8Eq441TupdSpyC6T45rI2rwjh1lzqTe_iBoAT2j6DdYq3hfKlfYPaRzIip-IkGGAch3L2Uhktp3GC_qxqaNAAgkTZdaNgQE7YJkQgmBoDYHudRVhvhPpTBB0IjdubBN_jA3VyXn-pzssonlV6XHWxVJ-0z6hW7gcE-vAGnMCxHAnFtovKMJwLq1UgorjtOxWiPgz8rsWFA";

            //var password = "eyJhbGciOiJSUzI1NiIsInR5cCI6ImF0K2p3dCJ9.eyJuYmYiOjE2NDM0NzI1MzAsImV4cCI6MTY0NjA2NDUzMCwiaXNzIjoiaHR0cHM6Ly9sb2NhbGhvc3Q6NTAwMS9pZGVudGl0eSIsImF1ZCI6WyJzLmNsdWJzeW5jIiwicy5yYWJiaXRtcSJdLCJjbGllbnRfaWQiOiJjbHViLmNsaWVudCIsInN1YiI6ImQ5YjQ0MTI3LTg5MGUtNDJhMC05N2VjLTk3ODA2MGNjZGE5OCIsImF1dGhfdGltZSI6MTY0MzQ3MjUyMywiaWRwIjoiZGVmYXVsdC1uYW1lIiwicmFiYml0bXEtc2NvcGUiOlsicy5yYWJiaXRtcS5jb25maWd1cmU6d2lubGluZUNsdWIvKiIsInMucmFiYml0bXEud3JpdGU6KndpbmxpbmVDbHViLyoiLCJzLnJhYmJpdG1xLnJlYWQ6KndpbmxpbmVDbHViLyoiXSwianRpIjoiMzI4QzdFQzRGMkY0NzA3MzU2QTk3QzVEMjkwMEIzNzciLCJpYXQiOjE2NDM0NzI1MzAsInNjb3BlIjpbImNsdWJzeW5jLWFwaSIsInMucmFiYml0bXEudGFnOmFkbWluaXN0cmF0b3IiLCJvZmZsaW5lX2FjY2VzcyJdLCJhbXIiOlsicHdkIl19.W92ML_KxaEauvfFhj5jTrXvm1TxwH7uEgJJ26jyhlVgZSj_Kq6tlpz7Dbvyj5Cbi2V3_JE1q7x5TAjSf1cEwEBcMuFNVNT1phOQFCwIrCSD5wBm7qeJQjaBtFmbCApya3U-D4rnpWJ_pdVFzu23TjTIX372KPqF9wNTS7h9qNPXMLI0zROqVX_UARlGie0IHCI8inxmKnD5_z5THJApgP2HBXz7qPmZI1TPAwdVHCHma-79uh_EG-2zVSnUXUbFx1CFHoII1DuHjB2FzeVLHhxv8aJU3JGXlPbBNdnBl6wfcMdP3Z1BSaWqXeCyQvnGVT-TLMnNHWaeZA1bqGFjbdg";

            var password = "guest";
            
            return await Task.FromResult(password);
        }
    }
}