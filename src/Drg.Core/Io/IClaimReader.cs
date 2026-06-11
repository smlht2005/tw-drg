using Drg.Core.Models;

namespace Drg.Core.Io;

public interface IClaimReader
{
    IEnumerable<ClaimEncounter> Read(string path);
}
