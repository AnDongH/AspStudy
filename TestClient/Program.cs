using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using CaseConverter;
using MessagePack;
using Newtonsoft.Json;
using Protocol;
using Protocol.DataAttribute;
using Protocol.Router;

namespace TestClient;

public enum HttpMethodType
{
    Get,
    Post,
    Put,
    Delete
}

class Program
{
    private static HttpClient Client = new HttpClient();
    
    private static Dictionary<string, string> ETagCache = new Dictionary<string, string>();
    
    static async Task Main(string[] args)
    {

        while (true)
        {
            Console.WriteLine("명령어 입력..");
            
            var keyInfo = Console.ReadKey();
            
            Console.WriteLine();
            
            if (keyInfo.KeyChar == 'e') break;

            if (keyInfo.KeyChar == 't')
            {
                // var req = new OutputCacheReq();
                // var res = await SerializeAndSendAsync(req) as OutputCacheRes;
                // Console.WriteLine(res.CacheTime);

                // await GetAsync(ProtocolId.OutputCache);

                var req = new JRateLimitFixedReq();
                await SendAsync(req);
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

    static async Task SendAsync(JsonProtocolReq req, HttpMethodType httpMethodType = HttpMethodType.Get)
    {
        try
        {
            var message = MakeRequest(req, httpMethodType);
            using var m = await Client.SendAsync(message);

            Console.WriteLine(m.ToString());
            
            if (!m.IsSuccessStatusCode)
            {
                return;
            }

            if (m.Headers.ETag != null)
            {
                if (ETagCache.TryGetValue(message.RequestUri.ToString(), out var eTag))
                {
                    if (eTag != m.Headers.ETag.Tag)
                    {
                        ETagCache[message.RequestUri.ToString()] = m.Headers.ETag.Tag;
                    }
                }
                else
                {
                    ETagCache.Add(message.RequestUri.ToString(), m.Headers.ETag.Tag);
                }
            }
            
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
    
    static HttpRequestMessage MakeRequest(JsonProtocolReq req, HttpMethodType methodType = HttpMethodType.Get)
    {
        var uriBuilder = new UriBuilder($"http://127.0.0.1:5179/{ProtocolRouter.RouterMap[req.ProtocolId]}");
        var query = HttpUtility.ParseQueryString(uriBuilder.Query);

        var type = req.GetType();

        var message = new HttpRequestMessage();
        
        Dictionary<string, object> body = null;
        
        switch (methodType)
        {
            case HttpMethodType.Get:
                message.Method = HttpMethod.Get;
                break;
            case HttpMethodType.Post:
                message.Method = HttpMethod.Post;
                break;
            case HttpMethodType.Put:
                message.Method = HttpMethod.Put;
                break;
            case HttpMethodType.Delete:
                message.Method = HttpMethod.Delete;
                break;
            default:
                message.Method = HttpMethod.Get;
                break;
        }
        
        foreach (var prop in type.GetProperties(    BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public))
        {
            QueryAttribute? qAtt = prop.GetCustomAttribute<QueryAttribute>();

            if (qAtt != null)
            {
                var value = prop.GetValue(req);
                if (value != null)
                {
                    if (qAtt.QueryName != null)
                        query[qAtt.QueryName] = value.ToString();
                    else 
                        query[prop.Name] = value.ToString();
                }
                    
                continue;
            }
                
            HeaderAttribute? hAtt = prop.GetCustomAttribute<HeaderAttribute>();

            if (hAtt != null)
            {
                var value = prop.GetValue(req);
                if (value != null)
                {
                    if (hAtt.HeaderName != null)
                    {
                        message.Headers.Add(hAtt.HeaderName, value.ToString());
                    }
                    else
                    {
                        message.Headers.Add(prop.Name, value.ToString());
                    }
                }
            }
            
            if (methodType == HttpMethodType.Get) continue;
            
            BodyAttribute? bAtt = prop.GetCustomAttribute<BodyAttribute>();

            if (body == null) body = new Dictionary<string, object>();
            
            if (bAtt != null)
            {
                var value = prop.GetValue(req);
                if (value != null)
                {
                    if (bAtt.BodyName != null)
                    {
                        body[bAtt.BodyName] = value;
                    }
                    else
                    {
                        body[prop.Name] = value;   
                    }
                }
            }
        }
            
        uriBuilder.Query = query.ToString();
            
        Console.WriteLine();
        Console.WriteLine(uriBuilder.ToString());
            
        message.RequestUri = uriBuilder.Uri;

        if (ETagCache.TryGetValue(uriBuilder.Uri.ToString(), out var eTag))
        {
            message.Headers.Add("If-None-Match", eTag);
        }
        
        if (body != null)
        {
            var json = JsonConvert.SerializeObject(body);
            message.Content = new StringContent(json);
            message.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        }
        
        return message;
    }
    
    // 특정 이름의 태그에 묶인 캐시들을 전부 만료시키는 메서드
    private static async Task ExpireTagCacheAsync(string tagName)
    {
        try
        {
            using var m = await Client.PostAsync($"http://127.0.0.1:5179/purge/{tagName}", null);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
}