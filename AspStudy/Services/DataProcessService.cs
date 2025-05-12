using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MessagePack;
using Microsoft.AspNetCore.Http;
using Protocol;

namespace AspStudy.Services;

public class DataProcessService
{
    public async Task SerializeAndSendAsync(HttpResponse httpRes, ProtocolRes protocolRes)
    {
        var bytes = MessagePackSerializer.Serialize(protocolRes);
        var writer = httpRes.BodyWriter;
        await writer.WriteAsync(bytes);
        await writer.FlushAsync();
    }
    
    public async Task<T> DeSerializeAsync<T>(HttpRequest httpReq, bool pipeRead = false) where T : ProtocolReq
    {
        if (pipeRead)
        {
            var reader = httpReq.BodyReader;
            var completeMessage = new List<byte>();
        
            while (true)
            {
                var readResult = await reader.ReadAsync();
                var buffer = readResult.Buffer;
        
                completeMessage.AddRange(buffer.ToArray());
        
                // BodyReader에게 처리한 데이터를 알림
                reader.AdvanceTo(buffer.End);
        
                // 모든 데이터를 읽었거나 취소된 경우 종료
                if (readResult.IsCompleted || readResult.IsCanceled)
                    break;
            }
        
            return MessagePackSerializer.Deserialize<ProtocolReq>(completeMessage.ToArray())  as T;
        }
        
        httpReq.EnableBuffering();
        
        using var memoryStream = new MemoryStream();
        await httpReq.Body.CopyToAsync(memoryStream);
        memoryStream.Position = 0;
        httpReq.Body.Position = 0;
        
        return MessagePackSerializer.Deserialize<ProtocolReq>(memoryStream.ToArray())  as T;
    }
}