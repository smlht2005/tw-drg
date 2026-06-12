using Drg.Core.Models;

namespace Drg.Core.Io;

public interface IResultWriter
{
    /// <summary>輸出 UTF-8 結果(FR-014);每列標註 <paramref name="rulesetVersion"/> 以利追溯(FR-013)。</summary>
    void Write(string path, IEnumerable<CodingResult> results, string rulesetVersion);
}
