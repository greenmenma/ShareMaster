using Microsoft.AspNetCore.Http;
using ShareMaster.Server.Hosting;

namespace ShareMaster.Server.Tests;

public class SpaHostingTests
{
    private const string Html = "<!doctype html><html><head><title>x</title></head><body></body></html>";

    [Fact]
    public void サブフォルダ配置では配置パスをbaseに埋め込む()
        => Assert.Contains("<head>\n    <base href=\"/ShareMaster/\" />", SpaHosting.InjectBaseHref(Html, new PathString("/ShareMaster")));

    [Fact]
    public void ルート配置ではスラッシュのみ()
        => Assert.Contains("<base href=\"/\" />", SpaHosting.InjectBaseHref(Html, PathString.Empty));

    [Fact]
    public void 配置パスはエンコードして埋め込む()
        => Assert.Contains("<base href=\"/%E5%85%B1%E6%9C%89/\" />", SpaHosting.InjectBaseHref(Html, new PathString("/共有")));
}
