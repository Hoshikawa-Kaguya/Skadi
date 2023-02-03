using Sora.Entities.Segment;
using System.Threading.Tasks;

namespace Skadi.Interface;

public interface IChromeService
{
    Task<SoraSegment> GetChromeXPathPic(string url, string xpath);

    Task<SoraSegment> GetChromePagePic(string url, bool all);
}