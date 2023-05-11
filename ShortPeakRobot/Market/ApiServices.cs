using ShortPeakRobot.Data;
using ShortPeakRobot.Market.Models.ApiModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ShortPeakRobot.Market
{
    public static class ApiServices
    {
        public static readonly HttpClient httpClient = new HttpClient();

        public static Tokens Tokens { get; set; } = new Tokens();
        //-------------------------------
        public async static Task<LoginResponse> Login(string login, string password)
        {
            var responseData = new LoginResponse();

            try
            {
                var response = await httpClient.PostAsJsonAsync("auth/login.php", new LoginRequest { login = login, password = password });
                var responceString = (response.Content.ReadAsStringAsync().Result).Trim('"');
                responseData = JsonSerializer.Deserialize<LoginResponse>(responceString);
            }
            catch (Exception)
            {

            }

            return responseData;
        }
        //----------------------------
        public async static Task<TestResponse> Test()
        {
            var responseData = new TestResponse();

            try
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens.access_token);

                var response = await httpClient.GetAsync("test.php");
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    var refreshToken = await RefreshToken();
                    if (refreshToken.success)
                    {
                        SetTokens(refreshToken);

                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens.access_token);
                        response = await httpClient.GetAsync("test.php");
                    }
                    else
                    {
                        responseData.message = refreshToken.message;
                        return responseData;
                    }
                }

                var responceString = (response.Content.ReadAsStringAsync().Result).Trim('"');
                responseData = JsonSerializer.Deserialize<TestResponse>(responceString);

            }
            catch (Exception)
            {

            }

            return responseData;
        }

        //----------------------
        public async static Task<LoginResponse> RefreshToken()

        {
            var responseData = new LoginResponse() ;
            

            try
            {
                var response = await httpClient.PostAsJsonAsync("auth/refresh_token.php", new RefreshTokenRequest { refresh_token = Tokens.refresh_token });
                var responceString = (response.Content.ReadAsStringAsync().Result).Trim('"');
                responseData = JsonSerializer.Deserialize<LoginResponse>(responceString);
            }
            catch (Exception)
            {

            }

            return responseData;
        }
        //-----------------------
        public static void SetTokens(LoginResponse loginResponse)
        {
            Tokens.access_token = loginResponse.data.access_token;
            Tokens.refresh_token = loginResponse.data.refresh_token;
        }
        //------------------- Orders ----------------------------------
        public async static Task<SaveDataResponse> SaveOrder(RobotOrder order)
        {
            var responseData = new SaveDataResponse();

            try
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens.access_token);

                var response = await httpClient.PostAsJsonAsync("save_order.php", order);
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    var refreshToken = await RefreshToken();
                    if (refreshToken.success)
                    {
                        SetTokens(refreshToken);

                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens.access_token);
                        response = await httpClient.PostAsJsonAsync("save_order.php", order);
                    }
                    else
                    {
                        responseData.message = refreshToken.message;
                        return responseData;
                    }
                }


                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    responseData.message = response.StatusCode.ToString();
                    return responseData;
                }

                var responceString = (response.Content.ReadAsStringAsync().Result).Trim('"');
                responseData = JsonSerializer.Deserialize<SaveDataResponse>(responceString);
            }
            catch (Exception)
            {

            }

            return responseData;
        }



        public async static Task<GetOrdersResponse> GetOrders(int robotId, DateTime startDate, DateTime endDate)
        {
            var responseData = new GetOrdersResponse();

            try
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens.access_token);

                var response = await httpClient.PostAsJsonAsync("get_orders.php", new PeriodRobotDataRequest { robot_id = robotId, start_date = startDate, end_date = endDate });

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    var refreshToken = await RefreshToken();
                    if (refreshToken.success)
                    {
                        SetTokens(refreshToken);

                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens.access_token);
                        response = await httpClient.PostAsJsonAsync("get_order.php", new PeriodRobotDataRequest { robot_id = robotId, start_date = startDate, end_date = endDate });
                    }
                    else
                    {
                        responseData.message = refreshToken.message;
                        return responseData;
                    }
                }


                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    responseData.message = response.StatusCode.ToString();
                    return responseData;
                }

                var responceString = (response.Content.ReadAsStringAsync().Result).Trim('"');
                responseData = JsonSerializer.Deserialize<GetOrdersResponse>(responceString);
            }
            catch (Exception)
            {

            }

            return responseData;
        }

        //---------- Trades ----------
        public async static Task<SaveDataResponse> SaveTrade(RobotTrade trade)
        {
            var responseData = new SaveDataResponse();

            try
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens.access_token);

                var response = await httpClient.PostAsJsonAsync("save_trade.php", trade);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    var refreshToken = await RefreshToken();
                    if (refreshToken.success)
                    {
                        SetTokens(refreshToken);

                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens.access_token);
                        response = await httpClient.PostAsJsonAsync("save_trade.php", trade);
                    }
                    else
                    {
                        responseData.message = refreshToken.message;
                        return responseData;
                    }
                }


                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    responseData.message = response.StatusCode.ToString();
                    return responseData;
                }

                var responceString = (response.Content.ReadAsStringAsync().Result).Trim('"');
                responseData = JsonSerializer.Deserialize<SaveDataResponse>(responceString);
            }
            catch (Exception)
            {

            }

            return responseData;
        }
        

        public async static Task<SaveDataResponse> UpdateTrade(RobotTrade trade)
        {
            var responseData = new SaveDataResponse();

            try
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens.access_token);

                var response = await httpClient.PostAsJsonAsync("update_trade.php", trade);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    var refreshToken = await RefreshToken();
                    if (refreshToken.success)
                    {
                        SetTokens(refreshToken);

                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens.access_token);
                        response = await httpClient.PostAsJsonAsync("update_trade.php", trade);
                    }
                    else
                    {
                        responseData.message = refreshToken.message;
                        return responseData;
                    }
                }


                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    responseData.message = response.StatusCode.ToString();
                    return responseData;
                }

                var responceString = (response.Content.ReadAsStringAsync().Result).Trim('"');
                responseData = JsonSerializer.Deserialize<SaveDataResponse>(responceString);
            }
            catch (Exception)
            {

            }

            return responseData;
        }

        public async static Task<GetTradesResponse> GetTrades(int robotId, DateTime startDate, DateTime endDate)
        {
            var responseData = new GetTradesResponse();

            try
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens.access_token);

                var response = await httpClient.PostAsJsonAsync("get_trades.php", new PeriodRobotDataRequest { robot_id = robotId, start_date = startDate, end_date = endDate });

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    var refreshToken = await RefreshToken();
                    if (refreshToken.success)
                    {
                        SetTokens(refreshToken);

                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens.access_token);
                        response = await httpClient.PostAsJsonAsync("get_trade.php", new PeriodRobotDataRequest { robot_id = robotId, start_date = startDate, end_date = endDate });
                    }
                    else
                    {
                        responseData.message = refreshToken.message;
                        return responseData;
                    }
                }


                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    responseData.message = response.StatusCode.ToString();
                    return responseData;
                }

                var responceString = (response.Content.ReadAsStringAsync().Result).Trim('"');
                responseData = JsonSerializer.Deserialize<GetTradesResponse>(responceString);
            }
            catch (Exception)
            {

            }

            return responseData;
        }

        //----------- Deals---------
        public async static Task<SaveDataResponse> SaveDeal(RobotDeal deal)
        {
            var responseData = new SaveDataResponse();

            try
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens.access_token);

                var response = await httpClient.PostAsJsonAsync("save_deal.php", deal);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    var refreshToken = await RefreshToken();
                    if (refreshToken.success)
                    {
                        SetTokens(refreshToken);

                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens.access_token);
                        response = await httpClient.PostAsJsonAsync("save_deal.php", deal);
                    }
                    else
                    {
                        responseData.message = refreshToken.message;
                        return responseData;
                    }
                }


                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    responseData.message = response.StatusCode.ToString();
                    return responseData;
                }

                var responceString = (response.Content.ReadAsStringAsync().Result).Trim('"');
                responseData = JsonSerializer.Deserialize<SaveDataResponse>(responceString);
            }
            catch (Exception)
            {

            }

            return responseData;
        }


        public async static Task<GetDealsResponse> GetDeals(int robotId, DateTime startDate, DateTime endDate)
        {
            var responseData = new GetDealsResponse();

            try
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens.access_token);

                var response = await httpClient.PostAsJsonAsync("get_deals.php", new PeriodRobotDataRequest { robot_id = robotId, start_date = startDate, end_date = endDate });

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    var refreshToken = await RefreshToken();
                    if (refreshToken.success)
                    {
                        SetTokens(refreshToken);

                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens.access_token);
                        response = await httpClient.PostAsJsonAsync("get_deal.php", new PeriodRobotDataRequest { robot_id = robotId, start_date = startDate, end_date = endDate });
                    }
                    else
                    {
                        responseData.message = refreshToken.message;
                        return responseData;
                    }
                }


                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    responseData.message = response.StatusCode.ToString();
                    return responseData;
                }

                var responceString = (response.Content.ReadAsStringAsync().Result).Trim('"');
                responseData = JsonSerializer.Deserialize<GetDealsResponse>(responceString);
            }
            catch (Exception)
            {

            }

            return responseData;
        }
        //------- Log -------------
        public async static Task<SaveDataResponse> SaveLog(RobotLog log)
        {
            var responseData = new SaveDataResponse();

            try
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens.access_token);

                var response = await httpClient.PostAsJsonAsync("save_log.php", log);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    var refreshToken = await RefreshToken();
                    if (refreshToken.success)
                    {
                        SetTokens(refreshToken);

                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens.access_token);
                        response = await httpClient.PostAsJsonAsync("save_log.php", log);
                    }
                    else
                    {
                        responseData.message = refreshToken.message;
                        return responseData;
                    }
                }


                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    responseData.message = response.StatusCode.ToString();
                    return responseData;
                }

                var responceString = (response.Content.ReadAsStringAsync().Result).Trim('"');
                responseData = JsonSerializer.Deserialize<SaveDataResponse>(responceString);
            }
            catch (Exception)
            {

            }

            return responseData;
        }

        public async static Task<GetLogsResponse> GetLogs(int robotId, DateTime startDate, DateTime endDate)
        {
            var responseData = new GetLogsResponse();

            try
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens.access_token);

                var response = await httpClient.PostAsJsonAsync("get_logs.php", new PeriodRobotDataRequest { robot_id = robotId, start_date = startDate, end_date = endDate });

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    var refreshToken = await RefreshToken();
                    if (refreshToken.success)
                    {
                        SetTokens(refreshToken);

                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens.access_token);
                        response = await httpClient.PostAsJsonAsync("get_logs.php", new PeriodRobotDataRequest { robot_id = robotId, start_date = startDate, end_date = endDate });
                    }
                    else
                    {
                        responseData.message = refreshToken.message;
                        return responseData;
                    }
                }


                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    responseData.message = response.StatusCode.ToString();
                    return responseData;
                }

                var responceString = (response.Content.ReadAsStringAsync().Result).Trim('"');
                responseData = JsonSerializer.Deserialize<GetLogsResponse>(responceString);
            }
            catch (Exception)
            {

            }

            return responseData;
        }
        //--------------------
        public async static Task<SaveDataResponse> UpdateRobotStateAsync(RobotState state)
        {
            var responseData = new SaveDataResponse();

            try
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens.access_token);

                var response = await httpClient.PostAsJsonAsync("update_robot_state.php", state);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    var refreshToken = await RefreshToken();
                    if (refreshToken.success)
                    {
                        SetTokens(refreshToken);

                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens.access_token);
                        response = await httpClient.PostAsJsonAsync("update_robot_state.php", state);
                    }
                    else
                    {
                        responseData.message = refreshToken.message;
                        return responseData;
                    }
                }


                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    responseData.message = response.StatusCode.ToString();
                    return responseData;
                }

                var responceString = (response.Content.ReadAsStringAsync().Result).Trim('"');
                responseData = JsonSerializer.Deserialize<SaveDataResponse>(responceString);
            }
            catch (Exception)
            {

            }

            return responseData;
        }

        //--------------------
        public async static Task<RobotStateResponse> GetRobotStateAsync(int robotId)
        {
            var responseData = new RobotStateResponse();

            try
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens.access_token);

                var response = await httpClient.PostAsJsonAsync("get_robot_state.php", robotId);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    var refreshToken = await RefreshToken();
                    if (refreshToken.success)
                    {
                        SetTokens(refreshToken);

                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens.access_token);
                        response = await httpClient.PostAsJsonAsync("get_robot_state.php", robotId);
                    }
                    else
                    {
                        responseData.message = refreshToken.message;
                        return responseData;
                    }
                }


                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    responseData.message = response.StatusCode.ToString();
                    return responseData;
                }

                var responceString = (response.Content.ReadAsStringAsync().Result).Trim('"');
                responseData = JsonSerializer.Deserialize<RobotStateResponse>(responceString);
            }
            catch (Exception)
            {

            }

            return responseData;
        }
    }
}
