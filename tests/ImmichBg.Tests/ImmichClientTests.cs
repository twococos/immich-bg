using ImmichBg.Immich;

namespace ImmichBg.Tests;

public class ImmichClientTests
{
    [Theory]
    [InlineData("192.168.1.10:2283", "http://192.168.1.10:2283/api/")]
    [InlineData("https://photos.example.com/", "https://photos.example.com/api/")]
    [InlineData("https://photos.example.com/api", "https://photos.example.com/api/")]
    [InlineData(" http://nas:2283/api/ ", "http://nas:2283/api/")]
    public void NormalizeBaseUrl(string input, string expected) =>
        Assert.Equal(expected, ImmichClient.NormalizeBaseUrl(input));

    [Fact]
    public void Dimensions_PrefersAssetSize()
    {
        Assert.Equal((1080, 1920), AssetMapper.Dimensions(1080, 1920, new ExifDto(1920, 1080, "6")));
    }

    [Theory]
    [InlineData("6", 1080, 1920)]
    [InlineData("8", 1080, 1920)]
    [InlineData("1", 1920, 1080)]
    [InlineData("Rotate 90 CW", 1080, 1920)]
    [InlineData(null, 1920, 1080)]
    public void Dimensions_FromExifRespectsRotation(string? orientation, int w, int h)
    {
        Assert.Equal((w, h), AssetMapper.Dimensions(null, null, new ExifDto(1920, 1080, orientation)));
    }

    [Fact]
    public void Dimensions_UnknownIsZero() =>
        Assert.Equal((0, 0), AssetMapper.Dimensions(null, null, null));
}
