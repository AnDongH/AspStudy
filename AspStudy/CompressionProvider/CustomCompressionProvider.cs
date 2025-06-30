using System.IO;
using Microsoft.AspNetCore.ResponseCompression;

namespace AspStudy.CompressionProvider;

// 실제로는 압축되지 않지만, 구현할 위치를 보여줌
public class CustomCompressionProvider : ICompressionProvider
{
    public string EncodingName => "mycustomcompression";
    public bool SupportsFlush => true;

    public Stream CreateStream(Stream outputStream)
    {
        // Replace with a custom compression stream wrapper.
        return outputStream;
    }
}