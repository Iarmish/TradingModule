using ShortPeakRobot.Market.Models.ApiModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ShortPeakRobot.Market
{
    public static class ApiServices
    {
        public async static Task<LoginResponse> Login(HttpClient httpClient, string login, string password)
        {
            var loginResponse = new LoginResponse();            

            var formContent = new MultipartFormDataContent();

            formContent.Add(new StringContent(login), "login");
            formContent.Add(new StringContent(password), "password");

            try
            {
                var response = await httpClient.PostAsync("auth/login.php", formContent);            
                var responceString = (response.Content.ReadAsStringAsync().Result).Trim('"');
                loginResponse = JsonSerializer.Deserialize<LoginResponse>(responceString);                
            }
            catch (Exception)
            {

            }

            return loginResponse;
        }

        public async static Task<TestResponse> Test(HttpClient httpClient)
        {
            var testResponse = new TestResponse();

            try
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", MarketData.Tokens.access_token + "1");

                var response = await httpClient.GetAsync("test.php");
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    var refreshToken = await RefreshToken(httpClient);
                    if (refreshToken.success)
                    {
                        MarketData.Tokens.access_token = refreshToken.data.access_token;
                        MarketData.Tokens.refresh_token = refreshToken.data.refresh_token;

                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", MarketData.Tokens.access_token);
                        response = await httpClient.GetAsync("test.php");
                    }
                    else
                    {
                        testResponse.message = refreshToken.message;
                        return testResponse;
                    }
                }

                var responceString = (response.Content.ReadAsStringAsync().Result).Trim('"');
                testResponse = JsonSerializer.Deserialize<TestResponse>(responceString);

            }
            catch (Exception)
            {


            }

            return testResponse;
        }


        public async static Task<LoginResponse> RefreshToken(HttpClient httpClient)
        {
            var loginResponse = new LoginResponse();

            var formContent = new MultipartFormDataContent();

            formContent.Add(new StringContent(MarketData.Tokens.refresh_token), "refresh_token");

            try
            {

                //httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await httpClient.PostAsync("auth/refresh_token.php", formContent);
                var responceString = (response.Content.ReadAsStringAsync().Result).Trim('"');
                loginResponse = JsonSerializer.Deserialize<LoginResponse>(responceString);
            }
            catch (Exception)
            {

            }

            return loginResponse;
        }
    }
}
