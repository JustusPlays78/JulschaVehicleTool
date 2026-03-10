using CodeWalker.GameFiles;

namespace JulschaVehicleTool.Core.Services;

/// <summary>
/// Loads GTA V binary files (YFT, YTD) via CodeWalker.Core.
/// Only used for binary formats - XML metas are handled by MetaXmlService.
/// </summary>
public class BinaryFileService
{
    public YftFile LoadYft(string path)
    {
        var data = File.ReadAllBytes(path);
        var yft = new YftFile();
        yft.Load(data);
        return yft;
    }

    public YtdFile LoadYtd(string path)
    {
        var data = File.ReadAllBytes(path);
        var ytd = new YtdFile();
        ytd.Load(data);
        return ytd;
    }

    public YftFile LoadYftFromBytes(byte[] data)
    {
        var yft = new YftFile();
        yft.Load(data);
        return yft;
    }

    public YtdFile LoadYtdFromBytes(byte[] data)
    {
        var ytd = new YtdFile();
        ytd.Load(data);
        return ytd;
    }
}
