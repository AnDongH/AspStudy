using System;
using System.Net.Http;
using System.Threading.Tasks;
using MessagePack;
using Newtonsoft.Json;
using Protocol;
using Protocol.Router;

namespace TestClient;

class Program
{
    private static HttpClient Client = new HttpClient();
    
    static async Task Main(string[] args)
    {
        string input;

        while (true)
        {
            input = Console.ReadLine();
            
            if (input == "exit") break;

            if (input == "t")
            {
                // var req = new OutputCacheReq();
                // var res = await SerializeAndSendAsync(req) as OutputCacheRes;
                // Console.WriteLine(res.CacheTime);

                // await GetAsync(ProtocolId.OutputCache);

                var req = new JOutputCacheReq();
                await PostAsync(req);
            } 
        }
    }

    static async Task<ProtocolRes> SerializeAndSendAsync(ProtocolReq req)
    {
        try
        {
            var data = MessagePackSerializer.Serialize(req);
            using var m = await Client.PostAsync($"http://127.0.0.1:5179/{ProtocolRouter.RouterMap[req.ProtocolId]}", new ByteArrayContent(data));
            var resData = await m.Content.ReadAsByteArrayAsync();
            var res = MessagePackSerializer.Deserialize<ProtocolRes>(resData);
            return res;   
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        return null;
    }

    static async Task GetAsync(ProtocolId protocolId)
    {
        try
        {
            using var m = await Client.GetAsync($"http://127.0.0.1:5179/{ProtocolRouter.RouterMap[protocolId]}");
            var res = await m.Content.ReadAsStringAsync();
            Console.WriteLine(res);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
    
    static async Task PostAsync(JsonProtocolReq req)
    {
        try
        {
            var data = JsonConvert.SerializeObject(req);
            using var m = await Client.PostAsync($"http://127.0.0.1:5179/{ProtocolRouter.RouterMap[req.ProtocolId]}", new StringContent(data));
            var res = await m.Content.ReadAsStringAsync();
            Console.WriteLine(res);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
}