using Sora.Entities.Segment;
using System.Threading.Tasks;

namespace Skadi.Services;

public interface IChromeService
{
    public Task<SoraSegment> GetChromeXPathPic(string url, string xpath);

    public Task<SoraSegment> GetChromePagePic(string url, bool all);
}