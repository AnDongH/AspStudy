using System;
using System.Net.Http;
using System.Threading.Tasks;
using MessagePack;
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
                var req = new OutputCacheReq();
                var res = await SerializeAndSendAsync(req) as OutputCacheRes;
                Console.WriteLine(res.CacheTime);
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
}