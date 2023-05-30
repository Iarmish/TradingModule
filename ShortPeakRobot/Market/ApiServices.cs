using CryptoExchange.Net.Objects;
using ShortPeakRobot.Constants;
using ShortPeakRobot.Data;
using ShortPeakRobot.Market.Models.ApiDataModels;
using ShortPeakRobot.Market.Models.ApiModels;
using ShortPeakRobot.Robots;
using ShortPeakRobot.Robots.DTO;
using ShortPeakRobot.ViewModel;
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
                var response = await httpClient.PostAsJsonAsync("auth/login.php", 
                    new LoginRequest { login = login, password = password, app_instance_key = MarketData.Info.AppInstanceKey });
                var responceString = (response.Content.ReadAsStringAsync().Result).Trim('"');
                responseData = JsonSerializer.Deserialize<LoginResponse>(responceString);
            }
            catch (Exception error)
            {
                MarketData.Info.Message += "Api login " + " " + error.Message + "\n";
                MarketData.Info.IsMessageActive = true;
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
                var response = await httpClient.PostAsJsonAsync("auth/refresh_token.php",
                    new RefreshTokenRequest { refresh_token = Tokens.refresh_token, app_instance_key = MarketData.Info.AppInstanceKey });
                var responceString = (response.Content.ReadAsStringAsync().Result).Trim('"');
                responseData = JsonSerializer.Deserialize<LoginResponse>(responceString);
            }
            catch (Exception error)
            {
                MarketData.Info.Message += "Api RefreshToken " + " " + error.Message + "\n";
                MarketData.Info.IsMessageActive = true;
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

                var response = await httpClient.PostAsJsonAsync("robot_orders/add", new ApiOrderModel(order));
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    var refreshToken = await RefreshToken();
                    if (refreshToken.success)
                    {
                        SetTokens(refreshToken);

                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens.access_token);
                        response = await httpClient.PostAsJsonAsync("robot_orders/add", new ApiOrderModel(order));
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
            catch (Exception error)
            {
                MarketData.Info.Message += "Api SetTokens " + " " + error.Message + "\n";
                MarketData.Info.IsMessageActive = true;
            }

            return responseData;
        }



        public async static Task<GetOrdersResponse> GetOrders(int robotId, DateTime startDate, DateTime endDate)
        {
            var responseData = new GetOrdersResponse();

            try
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens.access_token);

                var dateFrom = startDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");//"2023-05-10T00:00:00.000Z"
                var dateTo = endDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
                var response = await httpClient.PostAsJsonAsync("robot_orders/list", 
                    new PeriodRobotDataRequest { robot_id = robotId, from = dateFrom, to = dateTo, limit = 200 });

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    var refreshToken = await RefreshToken();
                    if (refreshToken.success)
                    {
                        SetTokens(refreshToken);

                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens.access_token);
                        response = await httpClient.PostAsJsonAsync("robot_orders/list", 
                            new PeriodRobotDataRequest { robot_id = robotId, from = dateFrom, to = dateTo, limit = 200 });
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
                if (responseData.data == null)
                {
                    responseData.data = new List<ApiOrderModel>();
                }
            }
            catch (Exception error)
            {
                MarketData.Info.Message += "Api GetOrders " + " " + error.Message + "\n";
                MarketData.Info.IsMessageActive = true;
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

                var response = await httpClient.PostAsJsonAsync("robot_trades/add", new ApiTradeModel(trade));

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    var refreshToken = await RefreshToken();
                    if (refreshToken.success)
                    {
                        SetTokens(refreshToken);

                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens.access_token);
                        response = await httpClient.PostAsJsonAsync("robot_trades/add", new ApiTradeModel(trade));
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

                var apiTrade = new ApiTradeModel(trade);
                apiTrade.id = trade.Id;
                var response = await httpClient.PostAsJsonAsync("robot_trades/update", apiTrade);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    var refreshToken = await RefreshToken();
                    if (refreshToken.success)
                    {
                        SetTokens(refreshToken);

                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens.access_token);
                        response = await httpClient.PostAsJsonAsync("robot_trades/update", apiTrade);
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

                var dateFrom = startDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
                var dateTo = endDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
                var response = await httpClient.PostAsJsonAsync("robot_trades/list", 
                    new PeriodRobotDataRequest { robot_id = robotId, from = dateFrom, to = dateTo , limit = 200 });

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    var refreshToken = await RefreshToken();
                    if (refreshToken.success)
                    {
                        SetTokens(refreshToken);

                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens.access_token);
                        response = await httpClient.PostAsJsonAsync("robot_trades/list",
                            new PeriodRobotDataRequest { robot_id = robotId, from = dateFrom, to = dateTo , limit = 200 });
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

                if (responseData.data == null)
                {
                    responseData.data = new List<ApiTradeModel>();
                }
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

                var response = await httpClient.PostAsJsonAsync("robot_deals/add", new ApiDealModel(deal));

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    var refreshToken = await RefreshToken();
                    if (refreshToken.success)
                    {
                        SetTokens(refreshToken);

                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens.access_token);
                        response = await httpClient.PostAsJsonAsync("robot_deals/add", new ApiDealModel(deal));
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
            catch (Exception error)
            {
                MarketData.Info.Message += "Api SaveDeal " + " " + error.Message + "\n";
                MarketData.Info.IsMessageActive = true;
            }

            return responseData;
        }


        public async static Task<GetDealsResponse> GetDeals(int robotId, DateTime startDate, DateTime endDate)
        {
            var responseData = new GetDealsResponse();

            try
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens.access_token);

                var dateFrom = startDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
                var dateTo = endDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
                var response = await httpClient.PostAsJsonAsync("robot_deals/list", 
                    new PeriodRobotDataRequest { robot_id = robotId, from = dateFrom, to = dateTo, limit = 200 });

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    var refreshToken = await RefreshToken();
                    if (refreshToken.success)
                    {
                        SetTokens(refreshToken);

                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens.access_token);
                        response = await httpClient.PostAsJsonAsync("robot_deals/list", 
                            new PeriodRobotDataRequest { robot_id = robotId, from = dateFrom, to = dateTo , limit = 200 });
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

                if (responseData.data == null)
                {
                    responseData.data = new List<ApiDealModel>();
                }
            }
            catch (Exception error)
            {
                MarketData.Info.Message += "Api GetDeals " + " " + error.Message + "\n";
                MarketData.Info.IsMessageActive = true;
            }

            return responseData;
        }


        public async static Task<GetDealsResponse> GetClientDeals( DateTime startDate, DateTime endDate)
        {
            var responseData = new GetDealsResponse();

            try
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens.access_token);

                var dateFrom = startDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
                var dateTo = endDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
                var response = await httpClient.PostAsJsonAsync("robot_deals/client_list",
                    new PeriodRobotDataRequest { from = dateFrom, to = dateTo, limit = 0 });

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    var refreshToken = await RefreshToken();
                    if (refreshToken.success)
                    {
                        SetTokens(refreshToken);

                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens.access_token);
                        response = await httpClient.PostAsJsonAsync("robot_deals/client_list",
                            new PeriodRobotDataRequest {  from = dateFrom, to = dateTo, limit = 0 });
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

                if (responseData.data == null)
                {
                    responseData.data = new List<ApiDealModel>();
                }
            }
            catch (Exception error)
            {
                MarketData.Info.Message += "Api GetClientDeals " + " " + error.Message + "\n";
                MarketData.Info.IsMessageActive = true;
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

                var response = await httpClient.PostAsJsonAsync("robot_logs/add", new ApiLogModel(log));

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    var refreshToken = await RefreshToken();
                    if (refreshToken.success)
                    {
                        SetTokens(refreshToken);

                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens.access_token);
                        response = await httpClient.PostAsJsonAsync("robot_logs/add", new ApiLogModel(log));
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
            catch (Exception error)
            {
                MarketData.Info.Message += "Api SaveLog " + " " + error.Message + "\n";
                MarketData.Info.IsMessageActive = true;
            }

            return responseData;
        }

        public async static Task<GetLogsResponse> GetLogs(int robotId, DateTime startDate, DateTime endDate)
        {
            var responseData = new GetLogsResponse();

            try
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens.access_token);

                var dateFrom = startDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
                var dateTo = endDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
                var response = await httpClient.PostAsJsonAsync("robot_logs/list", 
                    new PeriodRobotDataRequest { robot_id = robotId, from = dateFrom, to = dateTo , limit = 200 });

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    var refreshToken = await RefreshToken();
                    if (refreshToken.success)
                    {
                        SetTokens(refreshToken);

                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens.access_token);
                        response = await httpClient.PostAsJsonAsync("robot_logs/list", 
                            new PeriodRobotDataRequest { robot_id = robotId, from = dateFrom, to = dateTo , limit = 200 });
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

                if (responseData.data == null)
                {
                    responseData.data = new List<ApiLogModel>();
                }
            }
            catch (Exception error)
            {
                MarketData.Info.Message += "Api GetLogs " + " " + error.Message + "\n";
                MarketData.Info.IsMessageActive = true;
            }

            return responseData;
        }
        //--------------------
        public async static Task<SaveDataResponse> UpdateRobotStateAsync(int robot_id, RobotState state)
        {
            var responseData = new SaveDataResponse();

            try
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens.access_token);

                var response = await httpClient.PostAsJsonAsync("robot_states/upsert", new ApiStateModel(state, robot_id));

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    var refreshToken = await RefreshToken();
                    if (refreshToken.success)
                    {
                        SetTokens(refreshToken);

                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens.access_token);
                        response = await httpClient.PostAsJsonAsync("robot_states/upsert", new ApiStateModel(state, robot_id));
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
            catch (Exception error)
            {
                MarketData.Info.Message += "Api UpdateRobotStateAsync " + " " + error.Message + "\n";
                MarketData.Info.IsMessageActive = true;
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

                var response = await httpClient.PostAsJsonAsync("robot_states/get", new StateRequest { robot_id = robotId });

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    var refreshToken = await RefreshToken();
                    if (refreshToken.success)
                    {
                        SetTokens(refreshToken);

                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Tokens.access_token);
                        response = await httpClient.PostAsJsonAsync("robot_states/get", new StateRequest { robot_id = robotId });
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
            catch (Exception error)
            {
                MarketData.Info.Message += "Api GetRobotStateAsync " + " " + error.Message + "\n";
                MarketData.Info.IsMessageActive = true;
            }

            return responseData;
        }
    }
}
