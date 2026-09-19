using SSSKLv2.Dto.Api.v1;

namespace SSSKLv2.Services.Interfaces;

public interface IOrderCsvExportJobService
{
    (CsvExportJobDto Job, bool Started) StartExport(string startedByUserId);
    CsvExportJobDto? GetStatus(Guid jobId);
    (byte[] Bytes, string FileName)? GetCsv(Guid jobId);
}
