using Drg.Core.Models;

namespace Drg.Core.Io;

public interface IResultWriter
{
    /// <summary>輸出 UTF-8 結果(FR-014);實作於 T027(US1)。</summary>
    void Write(string path, IEnumerable<CodingResult> results);
}
