using Microsoft.AspNetCore.Http;
using ShareMaster.Server.Hosting;

namespace ShareMaster.Server.Tests;

public class CanonicalUrlTests
{
    private static string? Build(CanonicalUrlSettings s, bool https, string host, string pathBase = "", string path = "/", string query = "")
        => CanonicalUrlMiddleware.BuildRedirectUrl(
            s, https, HostString.FromUriComponent(host), new PathString(pathBase), new PathString(path), new QueryString(query));

    private static CanonicalUrlSettings Settings(bool requireHttps = true, string domain = "*.hoge.co.jp", int? port = null)
        => new() { RequireHttps = requireHttps, Domain = domain, HttpsPort = port };

    [Fact]
    public void HTTPとホスト名のみは1回でHTTPSのFQDNへ()
        => Assert.Equal("https://server01.hoge.co.jp/ShareMaster/calendars?year=2026",
            Build(Settings(), false, "server01", "/ShareMaster", "/calendars", "?year=2026"));

    [Fact]
    public void HTTPのFQDNはHTTPSのみ切り替え()
        => Assert.Equal("https://server01.hoge.co.jp/ShareMaster/",
            Build(Settings(), false, "server01.hoge.co.jp", "/ShareMaster", "/"));

    [Fact]
    public void HTTPSのホスト名のみはドメインを付与しポートを維持()
        => Assert.Equal("https://server01.hoge.co.jp:8443/",
            Build(Settings(), true, "server01:8443"));

    [Fact]
    public void HTTPのポートは引き継がずHTTPSポートを使う()
    {
        Assert.Equal("https://web.hoge.co.jp/", Build(Settings(), false, "web.hoge.co.jp:80"));
        Assert.Equal("https://web.hoge.co.jp:8443/", Build(Settings(port: 8443), false, "web.hoge.co.jp:8080"));
    }

    [Fact]
    public void 正規URLならリダイレクトしない()
        => Assert.Null(Build(Settings(), true, "server01.hoge.co.jp"));

    [Theory]
    [InlineData("localhost")]
    [InlineData("127.0.0.1")]
    [InlineData("[::1]")]
    public void localhostとIPアドレスにはドメインを付与しない(string host)
        => Assert.Null(Build(Settings(requireHttps: false), false, host));

    [Fact]
    public void 設定が無効ならリダイレクトしない()
        => Assert.Null(Build(Settings(requireHttps: false, domain: ""), false, "server01"));

    [Theory]
    [InlineData("*.hoge.co.jp", "hoge.co.jp")]
    [InlineData(".hoge.co.jp", "hoge.co.jp")]
    [InlineData("hoge.co.jp", "hoge.co.jp")]
    [InlineData(" ", null)]
    public void ドメイン設定の正規化(string input, string? expected)
        => Assert.Equal(expected, CanonicalUrlMiddleware.NormalizeDomain(input));
}
