using System.Threading.Tasks;
using Sora.Entities.Segment;

namespace Skadi.Interface;

public interface IChromeService
{
    Task<SoraSegment> GetChromeSelectorPic(string url, string selector);

    Task<SoraSegment> GetChromePagePic(string url, bool all);

    Task InitBilibili();

    Task<(ulong, long)> GetBilibiliDynamic(long uid);
}